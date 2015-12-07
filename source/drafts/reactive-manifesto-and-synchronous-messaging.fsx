(*@
    Layout = "draft";
    Title = "The Reactive Manifesto and Synchronous Messaging";
    Date = "2015-11-29T23:27:31";
    Tags = "";
    Description = "The Reactive Manifesto sets out some good ideas, but message-passing shouldn't need to be asynchronous.";
*)
(*** hide ***)
#I @"..\..\packages\posts\Hopac\lib\net45"
#r "Hopac.Core.dll"
#r "Hopac.dll"
open Hopac
open Hopac.Infixes
// Consume the warning message
run <| Alt.unit ()
(**

Concurrency. It's a concept that is becoming increasingly important to mainstream software engineers. Processor speeds,
which increased greatly in the 1990, have generally plateaued over the last decade. Over that time, manufacturers have
increased the number of cores in a processor to increase processing power. Most mainstream computers today have at least
two cores; the laptop used to compose this article has four. [The free lunch is over][JAtw04]; there is no faster
processor that will speed up a sequential application. Today's software must be written to take advantage of concurrency
and parallelism, and understanding these concepts is the key to writing software which is highly-performant and
responsive.

  [JAtw04]:https://blog.codinghorror.com/threading-concurrency-and-the-most-powerful-psychokinetic-explosive-in-the-univ/
*)
(*** more ***)
(**

Enter the *[Reactive Manifesto][RM]*. This document establishes four key attributes—responsive, resilient, elastic,
and message-driven—that systems need to exhibit in order to meet demands that they be highly‐available, fault‐tolerant,
and scalable. This article focuses on the fourth attribute: message-driven. As the manifesto explains:

>Reactive Systems rely on asynchronous message-passing to establish a boundary between components that ensures loose
coupling, isolation, location transparency, and provides the means to delegate errors as messages. …
—\[[Reactive Manifesto][RM]\]

The *Reactive Manifesto*'s Glossary goes on to define "asynchronous":

> In the context of this manifesto we mean that the processing of a request occurs at an arbitrary point in time,
sometime after it has been transmitted from client to service. The client cannot directly observe, or synchronize with,
the execution that occurs within the service. This is the antonym of synchronous processing which implies that the
client only resumes its own execution once the service has processed the request.
—\[[Reactive Manifesto Glossary][RM.Async]\]

By this definition, the manifesto asserts that synchronous message-passing, also known as synchronous rendezvous is
antithetical to a reactive system. A client is not allowed to wait to ensure that the server has received its message.
To understand the basis for this assertion, let's take a look at the differences between asynchronous message-passing
and synchronous rendezvous.

Asynchronous message-passing is similar to dropping a letter in the mail and relying on the postal system to deliver the
message. Keywords here are `send` or `post` for the client and `receive` for the server with a `mailbox` being the means
of addressing messages.

In synchronous rendezvous, the client and the server meet at a common rendezvous point. When both are at the
rendezvous, the client hands the message to the server directly. If only one party is at the rendezvous, they generally
wait for their counterpart to arrive in order to perform the handoff. Keywords here are `give` for the client and `take` for
the server with a `channel` as the rendezvous point.

Asynchronous message-passing fits well into the postal system analogy. The client posts a message to the server's
mailbox. Once the message is sent, responsibility for delivering the message is transferred to the postal system. The
postal service provides at-most-once delivery by default. They also offer at-least-once (bulk mail) and exactly-once
(registered mail) services, but at an additional cost. Sending a message is inexpensive for the client, but in order to
receive a reply, the client must include a self-addressed reference to its own mailbox.

Synchronous rendezvous is similar to two agents meeting at a pre-arranged location in order to exchange a message.
The client deliveres the message directly to server. There is no need to rely on a third party to provide a delivery
guarantee. If the client doesn't need to ensure delivery, a courier can be hired to wait at the rendezvous point for
a minimal cost. To receive a reply a message may also contain a future rendezvous point where the client and server can
meet again in the future.

The manifesto's preference for asynchronous message-passing is based on the client being immediately freed to do other
meaningful work. The client doesn't have to wait for the server to become available. Other concerns, such as routing,
back-pressure, load-balancing, and guarantees are outsourced to the messaging system. The manifesto asserts that
synchronous processing necessarily prevents concurrency, but this assertion is flawed. As noted above, a synchronous
rendezvous can be turned into an asynchronous send by spawning a new process to pass the message. Guaranteeing
exactly-once delivery on top of an asynchronous send carries significant additional costs and explicit tracking by the
messaging system.

Often, messages between processes are queries and are not sent solely to induce side-effects. Additionally, within a
thread of execution, there is generally no other beneficial work to be done until the server replies to the query. In
the asynchronous context, a client must either be written as a finite-state machine or be structured as a series of
continuations with state tracked and passed along. Because these messages only receive a best-effort
guarantee, the system must be able to handle dropped messages at any of the interaction points.

I propose that the use of asynchronous in the *Reactive Manifesto* is better served by the term "non-blocking". Indeed,
the term is used later in the manifesto's statement of the "message-driven" property.

> … Non-blocking communication allows recipients to only consume resources while active, leading to less system
overhead. —\[[Reactive Manifesto][RM]\]

The manifesto goes on to define "non-blocking":

> In concurrent programming an algorithm is considered non-blocking if threads competing for a resource do not have
their execution indefinitely postponed by mutual exclusion protecting that resource. In practice this usually manifests
as an API that allows access to the resource if it is available otherwise it immediately returns informing the caller
that the resource is not currently available or that the operation has been initiated and not yet completed. A
non-blocking API to a resource allows the caller the option to do other work rather than be blocked waiting on the
resource to become available. This may be complemented by allowing the client of the resource to register for getting
notified when the resource is available or the operation has completed. —\[[Reactive Manifesto Glossary][RM.NonB]\]

"Non-blocking" and "synchronous rendezvous," as defined above, are not mutually exclusive, though the manifesto asserts
that they are. Nonetheless, the server's portion of asynchronous message-passing is still bound to wait for messages if
its mailbox is empty. The server's wait for new messages is inherently synchronized, yet in doing so, it frees up its
resources so that other processes may execute. This is the fundamental principle of concurrency. Software must to be
written such that if a needed resource is not available, a process can suspend and get out of the way to allow other
ready processes to execute. The server's syncronous receive proves that a wait by one process need not imply all others
are blocked.

In fact, as demonstrated in the analogies above, the mechanisms of asynchronous message-passing can be succinctly built
from the primitives of synchronous rendezvous. A synchronous `give` can be converted into an asynchronous `send` by
spawning a short-lived process to wait for the server to `take`. Multiple offers to `give` can be prioritized by the
server. By freeing ourselves from the constraints of explicitly asynchronous communication, applications can gain access
to a more powerful set of constructs.

Languages and libraries that provide synchronous message-passing primatives also provide mechanisms to allow for
non-deterministic handling of rendezvous, selecting from among several possible alternatives. In [Hopac][], an F#
library modeled after [Concurrent ML][CML], a process can offer to `give` a message and await a timeout at the same
time. The process will commit to the set of actions associated with which event occurs first. For example, the following
code demonstrates a server which takes a message if one is available and says "Hello". If no message becomes available
within one second, the server will instead commit to saying "Timed out...".
*)
(*** define-output: hopac1 ***)
let ch = Ch<string>()
let tryHello =
  Alt.choose [
    Ch.take ch ^-> sprintf "Hello, %s!"
    timeOutMillis 1000 ^->. "Timed out..."
  ]
queue <| Ch.give ch "World"
run tryHello
(**
produces
*)
(*** include-it: hopac1 ***)
(**
By introducing a two-second delay before the give:
*)
(*** define-output: hopac2 ***)
queue <| timeOutMillis 2000 ^=>. Ch.give ch "Panda"
run tryHello
(**
`tryHello` times out and instead produces
*)
(*** include-it: hopac2 ***)
(**
This exposes the real power of synchronous rendezvous. The type of `tryHello` is `Alt<string>`. That means it can
be composed with other `Alt`-types to build larger components. `tryHello` could be one of several alternatives that a
higher-level abstraction could use in in its own processing.

> By starting with base-event values to represent the communication operations, and providing combinators to
associate actions with events and to build nondeterministic choices of events, we provide a flexible mechanism for
building new synchronization and communication abstractions. —\[John Reppy, [Concurrent Programming in ML][JRep99], p. 124\]

John Reppy also contends that synchronous rendezvous has the advantage that its typical failure mode is deadlock,
which can be detected quickly, whereas asynchronous message-passing typically delays the detection of errors until a
mailbox is full (which in the case of an unbounded mailbox, may mean the entire process is out of memory).

*Concurrent Programming in ML*, quoted above, presents Concurrent ML as a set of extensions built upon Standard ML.
These extensions provide first-class synchronous primitives and the constructs which allow for their composition.
When CML was created in the early 1990s, the dominant architecture was the single-core uniprocessor so CML was
designed to enable concurrency on these machines. Hopac provides similar primitives to F# as a library; [Go][]
provides "[goroutines][GoConc]" and other synchronous programming constructs as part of the language.
[Concurrency does not equal parallelism][RPik12v], but software which is written to make good use of concurrency is
better enabled to take advantage of the parallelism provided by modern hardware.

The importance of designing systems that leverage concurrency will only increase in the future. The
*Reactive Manifesto* lays out some good principles, but its claims should be taken with a grain of salt. The goals of
the manifesto can be achieved without solely relying on asynchronous message-passing. Indeed, synchronous
rendezvous can provide a more powerful set of abstractions while still being easier to reason about.

  [CML]:http://cml.cs.uchicago.edu/
  [Go]:https://golang.org/
  [GoConc]:https://golang.org/doc/effective_go.html#concurrency
  [RM]:http://www.reactivemanifesto.org/
  [RM.Async]:http://www.reactivemanifesto.org/glossary#Asynchronous
  [RM.NonB]:http://www.reactivemanifesto.org/glossary#Non-Blocking
  [CSør15]:http://blog.geist.no/an-actor-model-example-with-akka-net/
  [JRep99]:http://www.cambridge.org/tv/academic/subjects/computer-science/distributed-networked-and-mobile-computing/concurrent-programming-ml
  [RPik12v]:https://vimeo.com/49718712
  [Hopac]:https://hopac.github.io/Hopac/Hopac.html
*)