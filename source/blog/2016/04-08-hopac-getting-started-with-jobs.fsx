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
idiosyncrasies.

The biggest difference with `Async<'a>` is that Hopac runs its jobs on threads dedicated to Hopac. Hopac pre-allocates
one Hopac thread per processor, and these threads are managed directly by Hopac rather than being a part of a general
.NET thread pool. The Hopac scheduler takes care of managing jobs, keeping track of which jobs are ready for execution
and handling when a thread switches between jobs. Hopac is heavily optimized to minimize the overhead related to its
management of jobs, providing better throughput and CPU utilization than `Async<'a>` and the TPL under workloads with
many concurrent tasks.

## Dealing with the garbage

When you first start working with Hopac, you are likely to see the following warning when you execute the first job.

> WARNING: You are using single-threaded workstation garbage collection, which means that parallel programs cannot
> scale. Please configure your program to use server garbage collection.

This warning relates to the fact that workstation garbage collection executes on the thread that triggered the
collection. If this thread happens to be one of the Hopac threads, the suspended job and any other jobs that may be
waiting for that job will be blocked until the collection completes. With server garbage collection, each CPU
receives its own heap and has a dedicated thread on which collections are executed. There are some instances where you
may choose to ignore this warning, but blocking a Hopac worker thread can have unintended consequences across other
Hopac threads due to its synchronous messaging design.

An application can request server garbage collection by adding the `gcServer` element to its `app.config`:

    [xml]
    <configuration>
      <runtime>
        <gcServer enabled="true"/>
      </runtime>
    </configuration>

For more information on GC settings, see the [MSDN documentation][MSDNGC].

  [MSDNGC]:https://msdn.microsoft.com/en-us/library/ee787088(v=vs.110).aspx#workstation_and_server_garbage_collection

Hopac is written to handle a very large number of jobs (e.g., millions) concurrently. Each job is very lightweight,
taking only a few bytes of memory for itself. A `Job<'a>` is a simple .NET object requiring no disposal or
finalization (as opposed to the `MailboxProcessor<'Msg>`, which is disposable). This means that when a job no longer
has any references keeping it alive, it can be readily garbage collected and no special kill protocol is required for
recursive jobs (servers/actors).

## The `job{}` computation expression

For users experienced with F#'s `async{}` computation expression, `job{}` will feel very familiar. The form is exactly
the same as `async{}` with the added benefit that the bind operation is overloaded to accept `Job<'a>`, `Async<'a>`,
`Task<T>`, and `IObservable<T>`. Here's an example of the computation expression in use:

*)
let doAThing (getThingAsync : Async<_>) delay = job {
  let! result = getThingAsync
  do! delay |> asJob
  return result
}
(**
The first thing to note is that `getThingAsync` works just fine as the direct target of the bind operation (`let!`). In
the second bind operation (`do!`), `asJob` was used when binding `delay`. The `asJob` function is a noop which tells
the compiler that `delay` needs to be a `Job<'a>` without providing an explicit type or upcast. Without it, the compiler
can't infer which of the several available bind overloads should be used. For those that prefer explicit function
signatures, the following is equivalent:
*)
let doAThing2 (getThingAsync : Async<_>) (delay : Job<unit>) = job {
  let! result = getThingAsync
  do! delay
  return result
}
(**
Jobs can be started by using `run`, `start`, or `queue` and are similar to the `Async` counterparts:
`RunSynchronously`, `StartImmediate`, and `Start`. As is the advice for `Async`, _`run` should only be used in a root
context_. If you ever find yourself using `run` from inside of a `job{}` or `async{}`, you are probably doing something
wrong. If Hopac detects that you are executing `run` on a Hopac thread, it will spit out a warning because this can
result in deadlocks.
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
computation—and its side-effects—to be executed once. Think of a promise as the concurrent alternative to `Lazy<T>`.
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
the live, push-based data streams and turn them into more manageable pull-based streams. In the meantime, you can learn
more about Hopac from the programming docs in the project's [GitHub docs][HopacGitHubDocs] folder and the from
[Hopac API documentation][HopacAPI] page.

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