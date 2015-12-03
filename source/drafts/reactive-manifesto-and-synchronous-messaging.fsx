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

Concurrency. It's a concept that is becoming increasingly important to mainstream software engineers. For over a
decade, the speed of processors has been steady. Limits imposed by quantum physics and manufacturing processes have
induced manufacturers to increase the number cores on a chip instead of increasing speed. [The free lunch is over][JAtw04].
Applications no longer get faster just by dropping in a faster processor. Now applications must be written to take
advantage of concurrency and parallelism, and understanding these concepts is key to writing the highly-performant
software of tomorrow.

  [JAtw04]:https://blog.codinghorror.com/threading-concurrency-and-the-most-powerful-psychokinetic-explosive-in-the-univ/
*)
(*** more ***)
(**

Enter the *[Reactive Manifesto][RM]*. This document establishes four properties—responsive, resilient, elastic,
and message-driven—that systems need to exhibit in order to meet demands that they be highly‐available, fault‐tolerant,
and scalable. I want to focus on the fourth property: message-driven. As the manifesto explains:

>Reactive Systems rely on asynchronous message-passing to establish a boundary between components that ensures loose
coupling, isolation, location transparency, and provides the means to delegate errors as messages. …
—\[[Reactive Manifesto][RM]\]

Breaking this explanation down, the *Reactive Manifesto*'s Glossary defines "asynchronous":

> In the context of this manifesto we mean that the processing of a request occurs at an arbitrary point in time,
sometime after it has been transmitted from client to service. The client cannot directly observe, or synchronize with,
the execution that occurs within the service. This is the antonym of synchronous processing which implies that the
client only resumes its own execution once the service has processed the request.
—\[[Reactive Manifesto Glossary][RM.Async]\]

By this definition, the manifesto asserts that synchronous message-passing is antithetical to a reactive system. A
client must not wait to ensure that the server has received the message. Before we handle that argument, let's take a
look at the differences between asynchronous and synchronous message-passing.

Asynchronous message-passing is similar to dropping a letter in the mail and relying on the postal system to deliver the
message. Keywords here are `send` or `post` for the client and `receive` for the server with a `mailbox` being the means
of addressing messages.

In synchronous message-passing, the client and the server have a common rendezvous point. When both are at the
rendezvous, the client hands the message to the server directly. If only one party is at the rendezvous, they may wait
for their counterpart to arrive in order to perform the handoff. Keywords here are `give` for the client and `take` for
the server with a `channel` as the rendezvous point.

Asynchronous message-passing fits well into the postal system analogy. Each user has their own mailbox. The postal
service provides at-most-once delivery by default. They also offer at-least-once (bulk mail) and exactly-once (signature
confirmation) services, but at an additional cost. Sending a message to a mailbox is as simple as attaching the mailbox
address to the message. In order to get a response, you must include a self-addressed reference to your own mailbox.

Synchronous message-passing is similar to two agents meeting at a pre-arranged location in order to exchange
information. The sender doesn't need to rely on anyone else to guarantee that his message has been delivered as he gives
the message directly to the recipient. If the sender really doesn't care about that guarantee, he can
instead hire a courier to wait with the message at the rendezvous point for minimal cost. A message may also contain a
future location where the sender will be waiting for a reply from the recipient.

The manifesto bases its claims on the assertion that synchronous processing necessarily prevents concurrency, but this
assertion is faulty. As noted above, a synchronous rendezvous can be turned into an asynchronous send by spawning a new
process to manage passing the message. In order to guarantee exactly-once delivery on top of an asynchronous send
requires significant additional costs and tracking by the messaging system.
In addition, many messages between components are queries for information and are not sent just for their side-effects.
Often, within a thread of execution, there is no other beneficial work to be done until the query has been answered. In
the asynchronous context, a client must either be written as a finite-state machine or be structured as a series of
continuations with state tracked and passed along. In addition, because these messages only receive a best-effort
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

"Non-blocking" and "synchronous message-passing" as defined above are not mutually exclusive. The manifesto asserts that
they are, but the server portion of asynchronous message-passing is still bound to wait by the mailbox if it is empty.
The server's wait for new messages is inherently synchronized, yet in doing so, it frees up its resources so that other
processes may execute. This is the fundamental principle of concurrency. Processes need to be written such that if a
needed resource is not available, a process can suspend and allow other ready processes to execute without blocking
their flow. The server's side of asynchronous message-passing proves that waiting by one process does not imply blocking
all others.

In fact, as demonstrated in the analogies above, the mechanisms of asynchronous message-passing can be succinctly built
from the primitives of synchronous rendezvous. A synchronous `give` can be converted into an asynchronous `send` by
spawning a short-lived process to wait for the server to `take`. Multiple offers to `give` can be prioritized by the
server. By freeing ourselves from the constraints of explicitly asynchronous communication, applications can gain access
to a more powerful set of constructs.

Languages and libraries that provide synchronous message-passing primatives also provide mechanisms to allow for
non-deterministic handling of rendezvous, selecting from among several possible alternatives. In [Hopac][], an F#
library modeled after [Concurrent ML][CML], a process can offer to `give` a message and await a timeout at the same
time. The process will commit to the set of actions associated with which event occurs first. For example, here we have
a server which takes a message if one is available and says "Hello". If no message is available within one second, the
server will instead commit to saying "Timed out...".
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
This exposes the real power of synchronous message-passing. The type of `tryHello` is `Alt<string>`. That means it can
be composed with other `Alt`-types to build larger components. `tryHello` could be one of several alternatives that a
higher-level abstraction could use in in its own processing.

> By starting with base-event values to represent the communication operations, and providing combinators to
associate actions with events and to build nondeterministic choices of events, we provide a flexible mechanism for
building new synchronization and communication abstractions. —\[John Reppy, [Concurrent Programming in ML][JRep99], p. 124\]

John Reppy also contends that synchronous message-passing has the advantage that its typical failure mode is deadlock,
which can be detected quickly, whereas asynchronous message-passing typically delays the detection of errors until a
mailbox is full (which in the case of an unbounded mailbox, may mean the entire process is out of memory).

*Concurrent Programming in ML*, quoted above, presents Concurrent ML as a set of extensions built upon Standard ML.
These extensions provide first-class synchronous primitives and the constructs which allow for their composition.
When CML was created in the early 1990's, the dominant architecture was the single-core uniprocessor so CML was
designed to enable concurrency on these machines. Hopac provides similar primitives F# as a library, while [Go][]
provides "[goroutines][GoConc]" and other synchronous programming constructs as part of the language.
[Concurrency is not parallelism][RPik12v], but software that is written to leverage concurrency is better enabled to
take advantage of the parallelism provided by modern hardware.

The importance of designing systems that can take leverage concurrency will only increase as time goes on. The
*Reactive Manifesto* lays out some good principles, but its claims should be taken with a grain of salt. The goals of
the manifesto can be achieved without solely relying on asynchronous message-passing. Indeed, synchronous
message-passing can provide a more powerful set of abstractions while still being easier to reason about.

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