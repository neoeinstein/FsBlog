(*@
    Layout = "draft";
    Title = "Taming the Live Wire";
    Date = "2015-12-13T14:15:16";
    Tags = "F# Hopac FsAdvent";
    Description = "The Reactive Manifesto sets out some good ideas, but message-passing shouldn't need to be asynchronous.";
*)
(*** hide ***)
#I @"..\..\..\packages\posts\Hopac\lib\net45"
#r "Hopac.Core.dll"
#r "Hopac.dll"
open Hopac
open Hopac.Infixes
(**
Reactive programming is a way to write software that can handle asynchronous events in a reasonable manner. By turning
these events into asynchronous data streams of observable events, frameworks like Microsoft's Rx Extensions and
Bacon.js make it easier to write software that handles them. Using a publish/subscribe model, events are pushed down
the stream to be handled asynchronously.
*)
(*** more ***)
(**
F# has its own primitive for handling these events. `Event` implements `IObservable` and can be used to trigger
downstream tasks. Here's a quick example demonstrating how to create and trigger an event:
*)
(*** define-output:start ***)
let c = Event<int>()
Event.add (printfn "Received %i") c.Publish
c.Trigger 42
(**
This results in the following output:
*)
(*** include-output:start ***)
(**
These events can be chained to handlers that map or filter the data and send it off as another event.
*)
type Player = PlayerOne | PlayerTwo
let turnStarted = Event<Player>

type ColoredBall = Yellow | Blue | Red | Purple | Orange | Green | Maroon

type Ball =
  | Solid of ColoredBall
  | Stripe of ColoredBall
  | EightBall
let ballPocketed = Event<Ball>

(**
*This post is a part of the F# Advent in English. Many thanks to Sergey Thion for promoting this event. For more posts
on F# and functional programming throughout December, check out the list of posts on his site.*
*)
