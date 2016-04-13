(*@
    Layout = "draft";
    Title = "Hopac: Playing at Actors";
    Date = "2016-04-10T10:00:00Z";
    Tags = "F#, Hopac, Async, actors, draft";
    Description = "Synchronous messaging and lightweight threading";
*)
(*** hide ***)
#I @"..\..\..\packages\posts\Hopac\lib\net45"
#r "Hopac.Core.dll"
#r "Hopac.dll"
#r "Hopac.Platform.dll"
open Hopac
(*** hide,define-output:warning ***)
run <| Job.unit ()
(**
In my [last post][Hopac1], I discussed a bit of the basics of Hopac and the similarities between `Job<'a>` and other
existing concurrency constructs. In this post, we're going to continue that exploration a little bit by creating a
stateful actor using Hopac and compare it to a similar implementation using F#'s `MailboxProcessor<'Msg>`. We'll
also construct an abstract data type to insulate consumers from some of the communication details. Finally, we will
discuss the value of synchronous rendezvous as compared to the asynchronous messaging pushed by the actor model.

  [Hopac1]:/blog/2016/04-08-hopac-getting-started-with-jobs/index.html
*)
(*** more ***)
(**
## Constructing an Actor

The actor that we are going to build is relatively simple. It will receive updates about an operation's progress and
health and provide a way to retreive the current status of the operation. To do this, we first set up a pair of types
that represent the health and status of the operation.
*)
type OperationHealth =
  | Healthy
  | Unhealthy

type OperationStatus =
  { Progress : uint32
    Health : OperationHealth }
(**
### The basic F# actor

Having defined what our actor will do, we can now define the discriminated union that encapsulates the three messages
that the actor will accept. One of the messages expects a reply from the actor, so a reply channel must be passed in
with that message. In FSharp.Core, that reply channel is `AsyncReplyChannel<'Reply>`. Since this blog is running live
code, we will also put this type into a module so that we can more easily separate it from the Hopac message type we
will define later.
*)
module FSharpActor =
  type StatusMessage =
    | UpdateProgress of uint32
    | UpdateHealth of OperationHealth
    | GetStatus of AsyncReplyChannel<OperationStatus>
(*** hide ***)
open FSharpActor
(**
As is customary, we will also alias `MailboxProcessor<'Msg>`:
*)
type Actor<'Msg> = MailboxProcessor<'Msg>
(**
Now we can define the actor:
*)
let createFSharpStatusActor () : Actor<StatusMessage> =
  let rec actorLoop state (mbx : Actor<StatusMessage>) = async {
    let! msg = mbx.Receive ()
    match msg with
    | UpdateProgress newProgress ->
      return! actorLoop { state with Progress = newProgress} mbx
    | UpdateHealth newHealth ->
      return! actorLoop { state with Health = newHealth} mbx
    | GetStatus replyCh ->
      replyCh.Reply state
      return! actorLoop state mbx
  }
  Actor<_>.Start <| actorLoop { Progress = 0u; Health = Healthy }

let fsActor = createFSharpStatusActor ()
(**
Communication with the actor is then done by invoking methods on the actor in an object-oriented fashion. Messages are
sent using `Post` and messages with replies are sent using `PostAndReply` or `PostAndAsyncReply`.
*)
(*** define-output:fsActor ***)
fsActor.Post <| UpdateProgress 1u
let status =
  fsActor.PostAndAsyncReply GetStatus
  |> Async.RunSynchronously
printfn "Current status: %A" status

fsActor.Post <| UpdateHealth Unhealthy
let status2 =
  fsActor.PostAndAsyncReply GetStatus
  |> Async.RunSynchronously
printfn "Current status: %A" status2
(*** include-output:fsActor ***)
(**
The client does not block when posting; messages are sent asynchronously. Once sent, the client has
no guarantee that the message will been received by the actor. The delivery guarantee is at-most-once. When a reply is
requested, the actor provides a blocking version and a version which returns an `Async<'a>`. Since concurrency is of
primary import here, we will pretend that the blocking version does not exist.

The actor uses an unbounded, FIFO mailbox for handling messages. The unbounded nature of this mailbox and the lack of
a built-in method to provide back-pressure means that an actor will queue up messages until an out-of-memory condition
occurs. The general failure mode when a message producer outpaces a message consumer is an `OutOfMemoryException`, the
source of which can be hard to diagnose.

There is also no mechanism for supporting a second "priority" channel for important system messages. The actor does
provide a `Scan` functionality, but this has a time complexity of `O(n)` to perform a linear search through messages
in the mailbox to find a match.

### Actors the Hopac way

*)
module HopacChActor =
  type StatusMessage =
    | UpdateProgress of uint32
    | UpdateHealth of OperationHealth
    | GetStatus of Ch<OperationStatus>
(*** hide ***)
open HopacChActor
(**

*)
module HopacIVarActor =
  type StatusMessage =
    | UpdateProgress of uint32
    | UpdateHealth of OperationHealth
    | GetStatus of IVar<OperationStatus>
(*** hide ***)
open HopacIVarActor
(**

*)
module HopacTwoChActor =
  type StatusMessage =
    | UpdateProgress of uint32
    | UpdateHealth of OperationHealth
(*** hide ***)
open HopacTwoChActor
(**

## Communicating with actors

## Shrink-wrapping the server

## Lessons from leam manufacturing

* Paper Airplane Lean Manufacturing Simulation

*)