(*@
    Layout = "post";
    Title = "Chiron: Easier with Computation Expressions";
    Date = "2016-04-02T18:30:00Z";
    Tags = "F#, Chiron, JSON";
    Description = "Powerful JSON processing with computation expressions";
*)
(**
Back in my first [Chiron article][Chiron1], I made a bit of a mistake. I started with all the things
that scare off new functional programmers. Things like `>>=` and monads. We're going to take a step
back from that precipice and talk about the `json{}` computation expression.

  [Chiron1]:/blog/2015/12-13-chiron-json-ducks-monads/index.html
*)
(*** more ***)
(*** hide ***)
#I @"..\..\packages\posts\FParsec\lib\portable-net45+netcore45+wpa81+wp8"
#I @"..\..\packages\posts\Aether\lib\net35"
#I @"..\..\packages\posts\Chiron\lib\net45"
#r "FParsec.dll"
#r "Aether.dll"
#r "Chiron.dll"
open Chiron
(**
In general, computation expressions are a bit of syntactic sugar that F# provides. Internally, the
monadic functions of `Bind` and `Return` are used, but that is generally transparent to the
user of a computation expression.

Let's dive straight into an example from the first article:
*)
(*** hide ***)
type User =
  { Name: string
    IsAdmin: bool }
  static member ToJson (x:User) = json {
    do! Json.write "name" x.Name
    do! Json.write "isAdmin" x.IsAdmin
  }
  static member FromJson (_:User) = json {
    let! n = Json.read "name"
    let! a = Json.read "isAdmin"
    return { Name = n; IsAdmin = a }
  }
(*** do-not-eval ***)
type User =
  { Name: string
    IsAdmin: bool }
(**
Now, using the `json{}` compuation expression, let's add the `ToJson` and `FromJson` functions:
*)
(*** do-not-eval ***)
  static member ToJson (x:User) = json {
    do! Json.write "name" x.Name
    do! Json.write "isAdmin" x.IsAdmin
  }
  static member FromJson (_:User) = json {
    let! n = Json.read "name"
    let! a = Json.read "isAdmin"
    return { Name = n; IsAdmin = a }
  }
(**
Compared to [the previous example][Chiron1User], I think this is way more understandable on its face.
There are no custom operators to figure out, and you don't need to open `Chiron.Operators` to use it.

  [Chiron1User]:/blog/2015/12-13-chiron-json-ducks-monads/index.html#user-type

So, what do the various parts mean? What's that `let!` and `do!`? For a fuller explanation of computation
expressions in general, I'll point you to the excellent introduction by Scott Wlaschin on [fsharpforfunandprofit.com][FSfFaPCompExp].
Nonetheless, let's briefly break this down.

  [FSfFaPCompExp]:https://fsharpforfunandprofit.com/posts/computation-expressions-intro/
*)
(*** do-not-eval ***)
    let! n = Json.read "name"
(**
The first line that we will look at is from the `FromJson` function. In this case, we are reading the `name` member
from the hidden `Json` object. This deserialization may or may not be successful. Presuming that it is successful, we
want the value to be bound to `n`, but if it fails, we want to short-circuit and report the error.

The key to making this work is the `!` in `let!`. The `!` provides a signal to the F# compiler that what we really want
is the `string` from the `Json<string>` returned by `Json.read`. Chiron's `json{}` implementation of provides such a
`bind` function which takes care of unwrapping the deserialized value, or in case deserialization fails, triggers the
short-circuit to return the deserialization error.
*)
(*** do-not-eval ***)
    do! Json.write "name" x.Name
(**
Things are subtly different in the `ToJson` function. In this case, we are updating the hidden `Json` object being
held by the computation expression. The write doesn't return anything meaningful but it does have a return type of
`Json<unit>`. The above is syntactically equivalent to:
*)
(*** do-not-eval ***)
    let! _ = Json.write "name" x.Name
(**
The `do!` just plain looks better than binding to an ignored value.

In the long-promised [next article][Chiron3], we'll finally delve into dealing with missing data and union types.

  [Chiron3]:# "Coming Soon!"
*)