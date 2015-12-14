(*@
    Layout = "post";
    Title = "Chiron: JSON + Ducks + Monads";
    Date = "2015-12-13T20:00:00";
    Tags = "F# Chiron FsAdvent JSON";
    Description = "Getting started with Chiron.";
*)
(**
There are a multitude of ways to handle JSON data on the .NET framework. You can pull in [Json.NET][], use the
`JsonSerializer` from the now proprietary [ServiceStack.Text][SvcStkTxt], or use the `JsonDataContractSerializer`
provided by the Base Class Libraries. Developers in F# have access to the strongly-typed erasing type provider through
FSharp.Data's [`JsonProvider`][FsDataJson]. In terms of simplicity, though, *[Chiron][]* delivers a fully-functional
JSON serializer in a compact, [single-file implementation][ChironFs].

  [Json.NET]:http://www.newtonsoft.com/json
  [SvcStkTxt]:https://servicestack.net/text
  [FsDataJson]:https://fsharp.github.io/FSharp.Data/library/JsonProvider.html
  [Chiron]:https://xyncro.tech/chiron/
  [ChironFs]:https://github.com/xyncro/chiron/blob/master/src/Chiron/Chiron.fs

*)
(*** more ***)
(**
At the core of Chiron is a simple discriminated union:
*)
(*** do-not-eval ***)
type Json =
  | Null of unit
  | Bool of bool
  | String of string
  | Number of decimal
  | Array of Json list
  | Object of Map<string,Json>
(**
Serialization of a `Json` instance to a JSON string is handled by the `format` and `formatWith` functions:
*)
(*** hide ***)
#I @"..\..\..\packages\posts\FParsec\lib\portable-net45+netcore45+wpa81+wp8"
#I @"..\..\..\packages\posts\Aether\lib\net35"
#I @"..\..\..\packages\posts\Chiron\lib\net45"
#r "FParsec.dll"
#r "Aether.dll"
#r "Chiron.dll"
open Chiron
open Chiron.Operators
let marcusJson =
  Object <| Map.ofList
    [ "name", String "Marcus Griep"
      "isAdmin", Bool true
      "social", Array
        [ Object <| Map.ofList
            [ "service", String "twitter"
              "username", String "neoeinstein" ]
          Object <| Map.ofList
            [ "service", String "github"
              "username", String "neoeinstein" ] ] ]
(*** define:jsonMonad, do-not-eval ***)
type Json<'a> = Json -> JsonResult<'a> * Json
(*** show ***)
let formatExample =
  Object <| Map.ofList
    [ "name", String "Marcus Griep"
      "isAdmin", Bool true
      "numbers", Array [ Number 1m; Number 2m; String "Fizz" ] ]

let formatCompact = Json.format formatExample
let formatPretty =
  Json.formatWith JsonFormattingOptions.Pretty formatExample
(**
By default, Chrion formats JSON in a compact form:
*)
(*** include-value:formatCompact ***)
(**
By specifying custom formatting options, you can get a more readable print out.
*)
(*** include-value:formatPretty ***)
(**
Text is turned into `Json` with the `parse` and `tryParse` functions. Chiron implements a custom [FParsec][] parser to
convert strings into corresponding `Json` instances. For example:

  [FParsec]:http://www.quanttec.com/fparsec/about/fparsec-vs-alternatives.html
*)
(*** define-output:parseExample ***)
Json.parse """
  {
    "foo": [ { "bar": 1 }, { "bar": 2 }, { "bar": "fizz" } ],
    "test": { "one":3.5, "two":null, "three":false }
  }
"""
(**
Parses into the following `Json`:
*)
(*** include-it:parseExample ***)
(** There are several reasons that parsing a JSON structure might fail. Using the `tryParse` function will return a
`Choice2Of2` in the event parsing fails.
*)
(*** define-output:tryParseExample ***)
Json.tryParse
  """{ "foo": [ { "bar": 1 }, { "bar": 2 } { "bar": "fizz" } ] }"""
(**
This results in an error message clearly indicating where the parsing error occurred.
*)
(*** include-it:tryParseExample ***)
(**
Converting data from `Json` to `string` and back again is all well and good, but every JSON library needs to provide a
means to convert JSON strings into a Plain Old *[insert language]* Object. Most .NET converters rely on reflection to
inspect data objects and perform conversion by convention. Chiron doesn't rely on convention or decorate members with
attributes. Instead, any type that has the static methods `FromJson` and `ToJson` can be serialized or deserialized.
Chiron's `serialize` and `deserialize` functions use [statically-resolved type parameters][SRTP], similar to duck-typing,
to hook in to the appropriate methods at compile time.

  [SRTP]:https://msdn.microsoft.com/en-us/library/dd548046.aspx

As an example, let's create a data type for a user:
*)
(*** hide ***)
type User =
  { Name: string
    IsAdmin: bool }
  static member ToJson (x:User) =
       Json.write "name" x.Name
    *> Json.write "isAdmin" x.IsAdmin
  static member FromJson (_:User) =
        fun n a ->
          { Name = n
            IsAdmin = a }
    <!> Json.read "name"
    <*> Json.read "isAdmin"
(*** do-not-eval ***)
type User =
  { Name: string
    IsAdmin: bool }
(**
Chiron uses a monadic type, `Json<'a>`, to build up the serialized `Json` type:
*)
(*** do-not-eval ***)
  static member ToJson (x:User) =
       Json.write "name" x.Name
    *> Json.write "isAdmin" x.IsAdmin
(**
The `ToJson` function consciously separates the name of the field in code from its representation in a JSON object.
This allows them to vary independently. This way we can later change how we refer to the field in code, without
accidentally breaking our JSON contract. `ToJson` takes two parameters, a `User` and a `Json`. That second parameter
is hidden in the `Json<'a>` return type. `Json<'a>` is a state monad which we use to build up a `Json` instance
in one direction, and extract values out of a `Json` instance in the other. `Json<'a>` is represented by the following
signature.
*)
(*** include:jsonMonad ***)
(**
The `*>` operator that we used in `ToJson` discards the `JsonResult<'a>` (which is only used when writing),
but continues to build upon the `Json` object from the previous operation. By chaining these operations together, we
build up the members of a `Json.Object`.

Serialization is done using `FromJson`:
*)
(*** do-not-eval ***)
  static member FromJson (_:User) =
        fun n a ->
          { Name = n
            IsAdmin = a }
    <!> Json.read "name"
    <*> Json.read "isAdmin"
(**
The `FromJson` function reads its value out of the implicit `Json` instance provided by `Json<'a>`. The dummy `User`
parameter is used by the F# compiler to resolve the statically-resolved type parameter on the `Json.deserialize`
function. The `FromJson` function makes use of `lift` and `apply` functions from our `Json<'a>` monad, which are identified
by the custom operators `<!>` and `<*>`, respectively.

With these two functions defined, we can serialize an instance of our custom type:
*)
(*** define-output:serialize1 ***)
{ Name = "Marcus Griep"; IsAdmin = true }
|> Json.serialize
|> Json.formatWith JsonFormattingOptions.Pretty
(*** include-it:serialize1 ***)
(**
And deserialize it:
*)
let deserializedUser : User =
  """{"name":"Marcus Griep","isAdmin":true}"""
  |> Json.parse
  |> Json.deserialize
(*** include-value:deserializedUser ***)
(**
Chiron provides built-in serializers for common primitives, such as `int`, `string`, `DateTimeOffset`, as well as
arrays, lists, sets, and simple tuples.

This should give you a start toward serializing and deserializing your own custom types, but what about types that
you don't control? We'll take a look at how to provide custom serializers for those types, in my
[next post][ChironPart2].

  [ChironPart2]:# "Coming soon!"
*)
(**
*)
(**
*This post is a part of the [F# Advent Calendar in English][FsAdvent]. Many thanks to Sergey Thion for promoting this event.
For more posts on F# and functional programming throughout December, check out the list of posts on his site.*

  [FsAdvent]:https://sergeytihon.wordpress.com/2015/10/25/f-advent-calendar-in-english-2015/
*)

