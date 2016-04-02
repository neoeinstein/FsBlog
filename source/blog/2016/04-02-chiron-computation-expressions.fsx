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
monadic functions of `Bind` and `Return` are utilized, but that is generally transparent to the
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

In the long-promised [next article][Chiron3], we'll finally delve into dealing with missing data and union types.

  [Chiron3]:# "Coming Soon!"
*)