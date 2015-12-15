@{
    Layout = "post";
    Title = "Hello, FsBlog!";
    Date = "2015-11-29T23:27:31";
    Tags = "";
    Description = "Getting started — all over again";
}

It has been a long time since I have done any sort of blogging. Now that I've committed to an F# Advent post, it's time that I pulled a fork off [FsBlog][FsBlog] and set up shop on `gh‑pages`.

  [FsBlog]:https://github.com/fsprojects/FsBlog

<!--more-->

I don't often find the time to sit down and write a proper post, and the hosting solutions that I've used in the past have been just as much a hassle as anything. In the past, I've journeyed the depths of Blogger, Livejournal, Wordpress, and a few self-hosted Wordpress instances. I've never been quite happy with the way that I can do things on sites like Blogger, and Livejournal is a relic of the early 2000's. Wordpress has been the general standard for blogging for the past few years, but it's written on PHP, and even the Wordpress folks [are getting tired of PHP][WPNew] as a language for web development. I don't blame them. Anytime I've done anything with PHP, I've been worried about security and never was sure if the ball of yarn I had just stood up for a blog was actually going to still be there when I looked back at it.

The new rage in the blogging world seems to be to use static site generators, and it's easy to see why. Static sites provide a wealth of benefits with very little downside. Web servers know how to serve up static files and attach all the caching headers they can. There's no need to invoke any sort of web engine on some under-performant piece of leased virtual hardware. Since there's no real logic to invoke, the attack surface is greatly minimized to any bugs inherent in the web server itself. And, since they don't have to run any of our unsanitary code, Github offers static site hosting for free through its [Github Pages][GHPages] offering.

This is pretty great all around. It means that I no longer have to be limited to 140 characters on Twitter (or multiple tweets), and I can avoid the headache of setting up some virtual machine that needs a port left open for the public to try to attack. Instead, generators like [Jekyll][Jekyll] and [FsBlog][FsBlog] have become the better way to start a new blog these days. Since the entire site is literally generated out of a Git repository, there is no more fiddling with databases or backups. You get clean static files that are generally host agnostic, which means that if Github decides to take it's ball and go home, it's no big deal to migrate those static files to another host.

For me, I also jumped at the chance to be writing blog posts in Markdown and F# script files. Thanks to the wonderful work of [Matt Ball][TwitMBal] and [Tomas Petricek][TwitTPet], FsBlog allows me to incorporate snippets of F# code inline. For example:

    [lang=fsharp]
    /// Represents the Natural numbers starting from zero
    let ℕ0 = Seq.initInfinite <| fun i -> i
    ℕ0
    |> Seq.take 200
    |> Seq.iteri <| fun idx ->
      printfn "Natural number %i is %i" (idx + 1)

[FSharp.Formatting][FsForm] provides rich tooltips to better understand what's going on in the code, like giving the signature for the `Seq.iteri` function. There are even more crazy awesome things that can be done, including [live evaluation and embedding of results][FsFormEval]. I plan on using these to their fullest as I continue to write about what I've been doing with F#.

  [FsForm]:https://tpetricek.github.io/FSharp.Formatting
  [FsFormEval]:https://tpetricek.github.io/FSharp.Formatting/evaluation.html
  [GHPages]:https://pages.github.com/
  [Jekyll]:https://jekyllrb.com/
  [TwitMBal]:https://twitter.com/MattDrivenDev
  [TwitTPet]:https://twitter.com/tomaspetricek
  [WPNew]:https://developer.wordpress.com/2015/11/23/the-story-behind-the-new-wordpress-com/
