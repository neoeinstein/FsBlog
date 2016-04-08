(*@
    Layout = "post";
    Title = "Hopac: Getting Started with Jobs";
    Date = "2016-04-08T00:05:00Z";
    Tags = "F#, Hopac, Async, Tasks Parallel Library";
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
Concurrency is a pretty hot topic these days. Processor speeds have generally plateaued over the last decade, and
multiple cores are now the default. Writing software to take advantage of these cores requires a different approach
than the single-threaded default. [Hopac][HopacGitHub] is a unique library that offers lightweight threading along
with a host of other valuable concurrency constructs, all of which make it easier to write highly-concurrent software.

  [HopacGitHub]:https://github.com/Hopac/Hopac/tree/master
*)
(*** more ***)
(**

## Comparing `Job<'a>`, `Async<'a>`, and `Task<T>`

Similar to `Async<'a>` in F# and `Task<T>` from the .NET Base Class Libraries, Hopac uses a `Job<'a>`
to define a concurrent thread of execution. Each of these three constructs uses a subtly different method of running
jobs<span class="ref">[1](#ref-1)</span>.

The <abbr title="Base Class Libraries">BCL</abbr>'s Task Parallel Library schedules its jobs using threads from the
.NET thread pool by default. Longer running tasks can be scheduled on their own threads by using
`TaskCreationOptions.LongRunning`. The default generally makes `Task<T>`s better suited for CPU-bound operations as
blocking IO-operations performed within a job can block that thread from being reused by the thread pool for other
ready work.

A significant advance has been made in the C# space with the introduction of `async`/`await`. Under the covers, the
C# compiler creates a state machine which handles decomposing an `async` method into a state machine which is better
able to release threads back to the thread pool when blocking operations occur. This results in a better utilization
of the thread pool threads and does a better job of enabling concurrency than the raw <abbr title="Task Parallel Library">
TPL</abbr>.

F#'s `Async<'a>` served as the inspiration for C#'s `async`/`await` functionality, but uses an `async{}` computation
expression and takes advantage of continuations to power the construction of its concurrency construct. The F#
compiler doesn't need any special knowledge of the `Async<'a>` construct. Instead the computation expression is
decomposed into a series of continuations. This is a bit less efficient than the state machines that C# generates
and as such that means that `Async<'a>` is less suited for CPU-bound operations.

For a more comprehensive look at the differences between the TPL, `async`/`await`, and `Async<'a>`, I recommend
Tomas Petricek's _[Asynchronous C# and F#][TPet10]_ series.

  [TPet10]:http://tomasp.net/blog/csharp-fsharp-async-intro.aspx/

Hopac's `Job<'a>` has more in common with F#'s `Async<'a>` than the `Task<T>` based model of C#. A `Task<T>` represents
a computation in progress, while a `Job<'a>` or `Async<'a>` represents a potential computation that can be invoked.
Hopac also provides a `job{}` computation builder similar to `async{}`, but with several nice additions and a few
idiosyncrasies. The biggest difference is that Hopac runs its jobs on threads dedicated to Hopac. Hopac pre-allocates
one thread per processor.

## Dealing with the garbage

When you first start working with Hopac, you are likely to see the following warning when you execute the first job.

> WARNING: You are using single-threaded workstation garbage collection, which means that parallel programs cannot
> scale. Please configure your program to use server garbage collection.

This warning relates to the fact that workstation garbage collection performs garbage collection on the thread that
triggered the collection. This means that any work scheduled on a thread that triggers a garbage collection is blocked
from execution until the collection completes. With server garbage collection, each CPU receives its own heap and has
a dedicated thread for garbage collection. There are some instances where you may want to ignore this warning, but
blocking a Hopac worker thread can have unintended consequences across other Hopac threads due to the synchronous
communication structure.

An application can request server garbage collection by adding the `gcServer` element to its `app.config`:

    [xml]
    <configuration>
      <runtime>
        <gcServer enabled="true"/>
      </runtime>
    </configuration>

For more information on GC settings, see the [MSDN documentation][MSDNGC].

  [MSDNGC]:https://msdn.microsoft.com/en-us/library/ee787088(v=vs.110).aspx#workstation_and_server_garbage_collection

## The `job{}` computation expression

For users experienced with F#'s `async{}` computation expression, `job{}` will feel very familiar. The form is nearly
the same as `async{}` with the added benefit that the bind operation is overloaded to accept `Job<'a>`, `Async<'a>`,
and `Task<T>`. There are also added overloads that deal with more situations than the default `async{}`.

*)
let doAThing (getThingAsync : Async<_>) delay = job {
  let! result = getThingAsync
  do! delay |> asJob
  return result
}
(**
In this little piece of code, we can see a couple of things. First we see that, `getThingAsync` works just fine being
the direct target of the bind operation. Second, we note that we used `asJob` when binding on `delay`. This is in part
to help the compiler to infer the type of `delay` as without it, it doesn't know which of the several available bind
operations should be used to infer its type. The `asJob` function is a noop which tells the compiler that `delay`
needs to be a `Job<'a>` and avoids unnecessary upcasts. We could have done this instead with equivallent results:
*)
let doAThing2 (getThingAsync : Async<_>) (delay : Job<unit>) = job {
  let! result = getThingAsync
  do! delay
  return result
}
(**
Jobs can be started by using `run`, `start`, or `queue` and are similar to the `Async` counterparts:
`RunSynchronously`, `StartImmediate`, and `Start`. As is the advice for `Async`, you should only use `run` in a root
context. Hopac will warn you if you use `run` inside a Hopac job as this can cause Hopac to deadlock.
*)
(*** define-output:doAThingResult ***)
let myResult = async { return 4; }
let delay = timeOutMillis 10
doAThing myResult delay |> run |> printfn "The result was: %i"
(*** include-output:doAThingResult ***)
(**

Looking at this snippet, you may have noticed<span class="ref">[2](#ref-2)</span> that `timeOutMillis` returns an
`Alt<'a>`. We will go into what that means in more depth in later posts, but in essence, an `Alt<'a>` is a `Job<'a>`
whose completion can be waited on in combination with other `Alt<'a>`s, with the result being whichever `Alt<'a>`
becomes ready first.

## Memoization

Like `Async<'a>`, the same `Job<'a>` can be passed into a function and executed multiple times to get the intended
effects. Hopac also offers a `Promise<'a>` type that can be used to memoize a job when you only want the
computation—and its side-effects—to be executed once. Think of it as the concurrent alternative to `Lazy<T>`.
`Promise<'a>` derives from `Alt<'a>`, so like `Alt<'a>`, it can be used in the same contexts as any other `Job<'a>`.

As an example, compare the output of these two snippets which both use the job defined by `sideEffect`.
*)
(*** define-output:memoization1 ***)
let sideEffect = job {
  printfn "> Side-effect!"
  return 4;
}
printfn "Without memoization"
run sideEffect |> printfn "The result was: %i"
run sideEffect |> printfn "The result was: %i"
(*** include-output:memoization1 ***)

(*** define-output:memoization2 ***)
let memoized = memo sideEffect
printfn "With memoization"
run memoized |> printfn "The result was: %i"
run memoized |> printfn "The result was: %i"
(*** include-output:memoization2 ***)
(**

As you can see, once memoized, the side-effect occurred only once. After that, the promise is considered fulfilled and
all future requests to read from the promise will immediately return the result already computed.

In future posts, I'll introduce the Hopac's messaging channels and the star of the show: alternatives. We'll also delve
into synchronous messaging to reproduce an actor using Hopac's concurrency primitives and look at how Hopac can tame
the live, push-based data streams and turn them into more manageable pull-based streams. In the meantime, I recommend
the programming docs in the project's [GitHub docs][HopacGitHubDocs] folder and the [API documentation][HopacAPI] itself.

  [HopacGitHubDocs]:https://github.com/Hopac/Hopac/tree/master/Docs
  [HopacAPI]:https://hopac.github.io/Hopac/Hopac.html

*)
delay |> start
(**

<div class="footnotes">

1. <a name="ref-1"></a> For the purposes of this post, I will refer to the concurrent threads of execution as _jobs_.
2. <a name="ref-2"></a> You can hover over most of the identifiers in the F# snippets to get tooltips with type
    information and XML documentation.

</div>
*)