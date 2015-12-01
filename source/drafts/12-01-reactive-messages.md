@{
    Layout = "draft";
    Title = "The Reactive Manifesto and Asynchronous Messaging";
    Date = "2015-11-29T23:27:31";
    Tags = "";
    Description = "The Reactive Manifesto sets out some good ideas, but message-passing shouldn't need to be asynchronous.";
}

Concurrency: A concept that all software engineers are coming to realize is increasingly important. In the past decade or so, the clock frequency of CPUs has plateaued. *Moore's Law* regarding transistor density is still holding, though we are approaching limits imposed by quantum physics. Instead of getting faster as CPU manufacturing processes shrink, processor companies are adding more cores onto a single chip. It's no secret that the free lunch is over; Jeff Atwood [noted the coming changes back in 2004][JAtw04]. In order to continue accelerating the performance of our applications, we must build our applications with concurrency and parallelism in mind.

<!--more-->

It's important to first note that there [is a difference between concurrency and parallelism][RPik12v]. But once we have done that, we realize that the tools we have are generally ill‐equiped to handle concurrent programming. Most of the common languages that are [used nowadays][ALa15] and [being searched for][PYLP] are imperative and object‐oriented languages. Functional languages—which tend to offer greater support for immutability and [referential transparency][HWikiRefTrans] and are better suited for a present where multiple cores share both data and execution—are only recently gaining mainstream notice.

Enter the [Reactive Manifesto (RM)][RM], a document which establishes four core qualities that system architectures should exhibit in order to meet demands that they be highly‐available, fault‐tolerant, and scalable. Among the four properties is that a system be "message-driven", defined as:

>Reactive Systems rely on asynchronous message-passing to establish a boundary between components that ensures loose coupling, isolation, location transparency, and provides the means to delegate errors as messages. Employing explicit message-passing enables load management, elasticity, and flow control by shaping and monitoring the message queues in the system and applying back-pressure when necessary. Location transparent messaging as a means of communication makes it possible for the management of failure to work with the same constructs and semantics across a cluster or within a single host. Non-blocking communication allows recipients to only consume resources while active, leading to less system overhead. —\[[Reactive Manifesto][RM]\]

To understand what this means, we look to the glossary for definitions. First we look at "asynchronous":

> In the context of this manifesto we mean that the processing of a request occurs at an arbitrary point in time, sometime after it has been transmitted from client to service. The client cannot directly observe, or synchronize with, the execution that occurs within the service. This is the antonym of synchronous processing which implies that the client only resumes its own execution once the service has processed the request. —\[[Reactive Manifesto Glossary][RM.Async]\]

In this definition, the RM explicitly notes that synchronous processing is antithetical to a reactive system. A client is not allowed to wait to ensure that the server has actually received the message. In "asynchronous message-passing", a client fires off a message to a server's mailbox. The server then processes messages from its mailbox in whatever order it deems appropriate (generally <abbr title="First-In First-Out">FIFO</abbr> order). In "synchronous message-passing", a client pauses execution until the server is ready to rendezvous with the client. When that happens, the message is passed directly to the server via a channel.

<!--That must be done indirectly, through some other mechanism, effectively baking at-most-once delivery into the system. In order to get any delivery guarantees, another process must handle delivery guarantees. Such guarantees are progressively more limiting and expensive in an asynchronous system. At-least-once delivery requires the system to track and manage the message internally and requires some level of idempotence from the message to ensure multiple deliveries don't have extra side effects. Exactly-once delivery is even more expensive in terms of overhead required from the messaging system to ensure delivery. In some ways, this is alright. If lost or over-delivered messages are a part of the system, then the system needs to be able to tolerate these scenarios.-->

There is an assumption implied in this definition that synchronous processing is bad, in that it must prevent concurrency. However, many communications between a client and a server are in the form of a query. In order to query a server, a client must form the request in such a way that an asynchronous reply message can be posted to its own mailbox when the result is available. The client then is freed to handle other work until the reply is received. This freedom forces the client to have some way to keep track of or pass state between its query and the server's reply so that the client can resume where it left off. Effectively a client needs to be written as a finite-state machine or broken out into a set of actors akin to implementing a continuation pattern. All this is glued together with an unreliable, at-most-once message delivery layer. Asynchronous message-passing hasn't changed the fact that, in order to make progress on a particular message, a process still needs to wait for an answer to its query. It's just added several requirements on the client to keep track of more things in the event of a failure.

Instead of "asynchronous", I think that the "non-blocking" achieves the reactive goals better without imposing the additional burdens above. Indeed, "non-blocking" is used later in the RM's definition for the "message-driven" property. Let's see how the manifesto defines "non-blocking":

> In concurrent programming an algorithm is considered non-blocking if threads competing for a resource do not have their execution indefinitely postponed by mutual exclusion protecting that resource. In practice this usually manifests as an API that allows access to the resource if it is available otherwise it immediately returns informing the caller that the resource is not currently available or that the operation has been initiated and not yet completed. A non-blocking API to a resource allows the caller the option to do other work rather than be blocked waiting on the resource to become available. This may be complemented by allowing the client of the resource to register for getting notified when the resource is available or the operation has completed. —\[[Reactive Manifesto Glossary][RM.NonB]\]

"Non-blocking" and "synchronous message-passing" are not mutually exclusive.

  [Akka]:http://akka.io
  [Akka.Net]:http://getakka.net
  [RM]:http://www.reactivemanifesto.org/
  [RM.Async]:http://www.reactivemanifesto.org/glossary#Asynchronous
  [RM.NonB]:http://www.reactivemanifesto.org/glossary#Non-Blocking
  [ALa15]:https://github.com/blog/2047-language-trends-on-github
  [CSør15]:http://blog.geist.no/an-actor-model-example-with-akka-net/
  [JAtw04]:https://blog.codinghorror.com/threading-concurrency-and-the-most-powerful-psychokinetic-explosive-in-the-univ/
  [PYLP]:https://pypl.github.io/PYPL.html
  [RPik12v]:https://vimeo.com/49718712
  [SpotifyCheatSheet]:http://nomad8.com/squadschaptersguilds/
  [HWikiRefTrans]:https://wiki.haskell.org/Referential_transparency
  [WPdtKA]:https://en.wikipedia.org/wiki/Drinking_the_Kool-Aid