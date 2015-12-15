(*@
    Layout = "post";
    Title = "Chiron: Taming Types in the Wild";
    Date = "2015-12-15T05:00:00Z";
    Tags = "F#, Chiron, FsAdvent, JSON, NodaTime";
    Description = "Using Chiron to serialize types that you can't control.";
*)
(**
In my [last post][Chiron1], I gave an overview of [Chiron][], described how to parse JSON, and demonstrated how to
write `ToJson` and `FromJson` static methods to support serialization of custom types. At the end of the article, I
left a question hanging: What if you don't control the data type that you want to serialize? What if you can't add the
static `ToJson`/`FromJson` methods required by Chiron? That's where serialization extensions come in.

  [Chiron1]:/blog/2015/12-13-chiron-json-ducks-monads/index.html
  [Chiron]:https://xyncro.tech/chiron/

*)
(*** more ***)
(*** hide ***)
#I @"..\..\..\packages\posts\FParsec\lib\portable-net45+netcore45+wpa81+wp8"
#I @"..\..\..\packages\posts\Aether\lib\net35"
#I @"..\..\..\packages\posts\Chiron\lib\net45"
#I @"..\..\..\packages\posts\NodaTime\lib\portable-net4+sl5+netcore45+wpa81+wp8+MonoAndroid1+MonoTouch1+XamariniOS1"
#r "FParsec.dll"
#r "Aether.dll"
#r "Chiron.dll"
#r "NodaTime.dll"
open Chiron
open Chiron.Operators
open NodaTime
open NodaTime.Text
(**
As an example, let's consider the [NodaTime][] library. NodaTime provides a preferable set of types for interacting
with date/time values when compared to the <abbr title="Base Class Libraries">BCL</abbr>, and I regularly reference
NodaTime anywhere that I need to work with or manipulate time. While it is possible to convert an `Instant` to a
`DateTimeOffset` and then serialize that value, it would be much nicer to be able to serialize an `Instant` directly.
We will use the representation defined in [ISO-8601][] for serializing `Instant` values to JSON strings. We could
choose any other form, like WCF's `\/Date(1234567890)\/`, but representing a date/time in any format other than the
ISO standard generally [leads to confusion][xkcd1179].

  [NodaTime]:http://nodatime.org/
  [ISO-8601]:http://www.w3.org/TR/NOTE-datetime
  [xkcd1179]:https://xkcd.com/1179/

Using NodaTime's facilities for formatting and parsing date/time strings we can define a serialization
extension for an `Instant`:
*)
let instantToJson i =
  String <| InstantPattern.GeneralPattern.Format i
(**
`GeneralPattern` provides serialization of `Instant`s in the ISO-8601 format. If you prefer a compatible
representation with sub-second resolution, you can use the `ExtendedIsoPattern` instead.

The `instantToJson` function has the type signature `Instant -> Json`. This is different from the monadic `Json<'a>`
signature that was used in the serializers in the previous post. Instead of calling `Json.serialize`, `instantToJson`
can be used as a drop-in replacement.
*)
let instantJson =
  SystemClock.Instance.Now
  |> instantToJson
  |> Json.format
(*** include-value: instantJson ***)
(**
Next we define the deserialization function. In doing so, I will also define an active pattern to encapsulate
NodaTime's pattern parsing logic. This will help to keep the intent of the deserialization function clear.
*)
let (|ParsedInstant|InvalidInstant|) str =
  let parseResult = InstantPattern.GeneralPattern.Parse str
  if parseResult.Success then
    ParsedInstant parseResult.Value
  else
    InvalidInstant

let instantFromJson = function
  | String (ParsedInstant i) -> Value i
  | json ->
    Json.formatWith JsonFormattingOptions.SingleLine json
    |> sprintf "Expected a string containing a valid ISO-8601 date/time: %s"
    |> Error
(**
`instantFromJson` has the type signature `Json -> JsonResult<Instant>`. Together with the signature of `instantToJson`,
these functions provide the complimentary facets of the `Json<'a>` state monad. `JsonResult<'a>` holds the working
value while `Json` stores the state of the JSON data that we are serializing or deserializing.

Now we can try to convert `instantJson` back to an `Instant` to validate that our serialization can round-trip.
*)
let instantRoundTrip =
  instantJson
  |> Json.parse
  |> instantFromJson
(*** include-value: instantRoundTrip ***)
(**
We can also check that an invalid value produces a relevant error message during deserialization:
*)
let instantError =
  """ "Tomorrow at 13:30" """
  |> Json.parse
  |> instantFromJson
(*** include-value: instantError ***)
(**
This looks good so far. Of course, it is rare to want to serialize something like an `Instant` all on its own. More
commonly, the data is incorporated into a larger JSON object or array. In this case, there are a few additional
read and write functions that provide points to inject custom (de)serialization functions: `readWith` and `writeWith`.
To demonstrate their use, we will consider a trivial type containing an `Instant` and add `FromJson` and `ToJson`
static methods.
*)
type MyType =
  { Time : Instant }
  static member ToJson (x:MyType) =
    Json.writeWith instantToJson "time" x.Time
  static member FromJson (_:MyType) =
        fun t -> { Time = t }
    <!> Json.readWith instantFromJson "time"
(**
Note how `instantToJson` and `instantFromJson` are injected as the first argument to `Json.writeWith` and
`Json.readWith`, respectively. Now we can demonstrate round-trip serialization:
*)
let myTypeJson =
  { Time = SystemClock.Instance.Now }
  |> Json.serialize
  |> Json.format
(*** include-value:myTypeJson ***)
let myTypeRoundTrip : MyType =
  myTypeJson
  |> Json.parse
  |> Json.deserialize
(*** include-value:myTypeRoundTrip ***)
(**
We now have serializers that can round-trip an external type either alone or as part of a type we control. What if the
external type is contained in yet another external type? To demonstrate this case, we will consider serializing an
`Instant list`:

    let listOfInstantJson =
      [ Instant(); SystemClock.Instance.Now ]
      |> Json.serialize
      |> Json.format

Before we even get to running this code, the compiler has already started disagreeing with us:

    [lang=text]
    Error: No overloads match for method 'ToJson'. …

Where did we go wrong? The issue is that an `a' list` is serializable through Chiron's default functions if and only
if `'a` contains the necessary `ToJson`/`FromJson` functions. In this case, `Instant` doesn't have the needed hooks for
Chiron's default serialization to hook into, so we wrote our own serialization functions. As a precursor to serializing
an `Instant list`, we first define a function that can serialize a generic `'a list`.
*)
let listToJsonWith serialize lst =
  Array <| List.map serialize lst
(**
The `serialize` parameter provides an injection point where we can provide our custom serializer.

The deserializer is a little more complicated because we can't just map it over the list.
We need a function that maps `Json -> JsonResult<Instant list>`. Chiron already has a function that fits this need:
[`fromJsonFold`][fromJsonFold]. Chiron uses this function to support its default serialization of arrays and lists.
`fromJsonFold` iterates over a `Json list` wrapped by a `Json.Array` and produces a `JsonResult` over the list. This
function is marked `internal`, though, so we don't have direct access to it. Instead, we can extract the function's
logic and refactor it to fit our needs. Replacing `fromJson` with a new `deserialize` parameter gives us a generic
function for applying a custom deserializer over a `Json list`.

  [fromJsonFold]:https://github.com/xyncro/chiron/blob/5b02320/src/Chiron/Chiron.fs#L821
*)
let fromJsonFoldWith deserialize fold zero xs =
    List.fold (fun r x ->
      match r with
      | Error e -> Error e
      | Value xs ->
        match deserialize x with
        | Value x -> Value (fold x xs)
        | Error e -> Error e) (Value zero) (List.rev xs)

let listFromJsonWith deserialize = function
  | Array l -> fromJsonFoldWith deserialize (fun x xs -> x::xs) [] l
  | _ -> Error "Expected an array"
(**
`fromJsonFoldWith` is likely to be added in to Chiron in a future version, but for now, our custom serialization
functions suffice as demonstrated by another round-trip:
*)
let listOfInstantJson =
  [ Instant(); SystemClock.Instance.Now ]
  |> listToJsonWith instantToJson
  |> Json.format
(*** include-value: listOfInstantJson ***)
let listOfInstantRoundTrip =
  listOfInstantJson
  |> Json.parse
  |> listFromJsonWith instantFromJson
(*** include-value: listOfInstantRoundTrip ***)
(**
Thus far, I've only been creating custom serializers for records and tuples, a.k.a. [product types][SWla12],
and none of these serializers would deal well if they were given an object with missing data or `null` values:

  [SWla12]:http://fsharpforfunandprofit.com/posts/overview-of-types-in-fsharp/
*)
let result : Choice<MyType,_> =
  Json.parse "{}"
  |> Json.tryDeserialize
(*** include-value: result ***)
(**
In order to handle missing data or `null` values and other discriminated unions, a.k.a. [sum types][SWla12],
we will need to learn about a few more tricks that Chiron has up its sleeve. In my [next post][Chiron3], I will focus
on the Chiron features that allow you to provide defaults for missing values and serialize the disjoint cases of
a discriminated union.

  [Chiron3]:# "Coming Soon!"

*This post is a continuation of my post for the [F# Advent Calendar in English][FsAdvent]. Many thanks to
[Sergey Tihon][SergeyTihonTwitter] for promoting this event. For more posts on F# and functional programming
throughout December, check out the list of posts on his site.*

  [FsAdvent]:https://sergeytihon.wordpress.com/2015/10/25/f-advent-calendar-in-english-2015/
  [SergeyTihonTwitter]:https://www.twitter.com/sergey_tihon
*)
