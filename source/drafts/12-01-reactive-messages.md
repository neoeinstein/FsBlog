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

Enter the [Reactive Manifesto][RM]. The manifesto establishes four core qualities that systems should exhibit in order to meet demands that they be highly‐available, fault‐tolerant, and scalable. There are a number of good ideas there, and I was first introduced to the document while learning about [Akka][] and [Akka.Net][]. The [actor model][CSør15] offered by these actor systems fits most of the tenets of a Reactive<sup><span class="fa fa-trademark"></span></sup> system. Supervision provides fault‐tolerance; dynamic routers can provide elasticity; and, by their nature, actors rely on asynchronous message‐passing. In the glossary, Asynchronous is defined in the following way:

> In the context of this manifesto we mean that the processing of a request occurs at an arbitrary point in time, sometime after it has been transmitted from client to service. The client cannot directly observe, or synchronize with, the execution that occurs within the service. This is the antonym of synchronous processing which implies that the client only resumes its own execution once the service has processed the request. —*[Reactive Manifesto Glossary][RM.Async]*

For a while I [drank the Kool-Aid][WPdtKA]. I wrote several drafts of services using actor systems. I presented videos and introduced the concepts to other engineers in my organization. I even formed a [guild][SpotifyCheatSheet] around functional programming. As I worked with Akka.Net, I uncovered a few things that caused the actor model to lose some of its luster in my eyes. While I am all-in on functional programming languages, I've pulled up short on the actor model.

  [Akka]:http://akka.io
  [Akka.Net]:http://getakka.net
  [RM]:http://www.reactivemanifesto.org/
  [RM.Async]:http://www.reactivemanifesto.org/glossary#Asynchronous
  [ALa15]:https://github.com/blog/2047-language-trends-on-github
  [CSør15]:http://blog.geist.no/an-actor-model-example-with-akka-net/
  [JAtw04]:https://blog.codinghorror.com/threading-concurrency-and-the-most-powerful-psychokinetic-explosive-in-the-univ/
  [PYLP]:https://pypl.github.io/PYPL.html
  [RPik12v]:https://vimeo.com/49718712
  [SpotifyCheatSheet]:http://nomad8.com/squadschaptersguilds/
  [HWikiRefTrans]:https://wiki.haskell.org/Referential_transparency
  [WPdtKA]:https://en.wikipedia.org/wiki/Drinking_the_Kool-Aid