# problemdetails-serilog-connector

## What does it do?

This package facilitates use of [serilog/serilog-aspnetcore](https://github.com/serilog/serilog-aspnetcore) alongside [khellang/Middleware](https://github.com/khellang/Middleware), to log exceptions at request completion, with a severity level indicating their nature.

It works well with Serilog's [default log level computation](https://github.com/serilog/serilog-aspnetcore/blob/dev/src/Serilog.AspNetCore/AspNetCore/RequestLoggingOptions.cs#L30), which treats http status codes ≥500 as errors🔥, and lower status codes (when an exception is not flying, i.e. it has been caught by Problem Details) as informational `🛈` messages.

## nuget Package

[ProblemDetails.Serilog.AspNetCore.Middleware.Connector](https://www.nuget.org/packages/ProblemDetails.Serilog.AspNetCore.Middleware.Connector/)


# Usage

## Simple

1) When configuring the application's request pipeline, call `UseSerilogRequestLoggingAndProblemDetails` instead of `UseSerilogRequestLogging` and `UseProblemDetails`.
1) When configuring the application's services, call `AddProblemDetailsAlongsideSerilog` instead of `AddProblemDetails`.

## Explicit Middleware Registration

Calling `UseSerilogRequestLoggingAndProblemDetails` registers three middlewares. These middlewares can be registered explicitly instead:

```csharp
public void Configure(IApplicationBuilder app)
{
    // ...
    app.UseSerilogRequestLogging(configureOptions); // provided by Serilog
    app.UseProblemDetails(); // provided by Hellang
    app.UseSerilogRequestLoggingCaptureException(); // provided by this package
    // ...
}
```

Optionally, other middlewares can be mixed in:

```csharp
public void Configure(IApplicationBuilder app)
{
    // ...
    app.UseSerilogRequestLogging(configureOptions);
    app.UseCoolMiddleware(); // for example
    app.UseProblemDetails();
    app.UseSentryTracing(); // for example
    app.UseSerilogRequestLoggingCaptureException();
    // ...
}
```


# Referenced Components
## Serilog and its Diagnostic Context

[Serilog](https://github.com/serilog/serilog-aspnetcore) provides a convenient diagnostic context capable of capturing an exception that occured at some point during a request. The `RequestLoggingMiddleware` installed by `UseSerilogRequestLogging` [logs the exception captured in the context](https://github.com/serilog/serilog-aspnetcore/issues/270) when there is no unhandled exception flying.

## Problem Details

[ProblemDetails](https://github.com/khellang/Middleware) catches exceptions and translates them to http responses generally compliant with [RFC7807](https://www.rfc-editor.org/rfc/rfc7807).


# Connector Internals
## Exception Capturing Middleware

This package provides exception capturing middleware that can be registered using a call to the `UseSerilogRequestLoggingCaptureException` extension method. This very simple middleware writes any exceptions encountered to Serilog's diagnostic context, without catching them.

## Serilog alongside Problem Details

This package provides a `UseSerilogRequestLoggingAndProblemDetails` extension method that can be used in place of Serilog's `UseSerilogRequestLogging` and Hellang's `UseProblemDetails`. It also registers the exception capturing middleware provided by this package, via a call to `UseSerilogRequestLoggingCaptureException`. The middleware registration order is:

1) `UseSerilogRequestLogging`
1) `UseProblemDetails`
1) `UseSerilogRequestLoggingCaptureException`

The resulting behavior is that exceptions thrown from requests (ex. from MVC controller action methods) are writen to the diagnostic context, then caught by Problem Details, then logged by Serilog.

## Problem Details configuration

This package provides a `AddProblemDetailsAlongsideSerilog` extension method that can be used in place of Hellang's `AddProblemDetails`. It calls Hellang's `AddProblemDetails`, but with options defaulted to request that Problem Details doesn't consider as "unhandled", and therefore doesn't log, any exceptions. The options used look like this:

```csharp
options.ShouldLogUnhandledException = (httpContext, exception, problemDetails) => false;
```


# Alternatives

Some alternatives to using this package are described below.
 - Problem Details can be configured to log additional exceptions using its `ShouldLogUnhandledException` option, by returning true from the configured function. This approach logs only at the error level, with a message describing logged exceptions as _unhandled_.
 - Problem Details can be configured to rethrow exceptions, so that other middlewares can see and log them. Serilog's default log level computation will consider all rethrown exceptions still flying to be errors, regardless of the response status code. `GetLevel` can be [configured](https://github.com/serilog/serilog-aspnetcore/blob/dev/src/Serilog.AspNetCore/AspNetCore/RequestLoggingOptions.cs#L66) to consider response status codes even when an exception is flying.
 - Problem Details can be configured to log exceptions using its `ShouldLogUnhandledException` option, by introducing side effects (e.g. explicit logging to an `ILogger`, propagating the exception to an `IDiagnosticContext`) within the configured function.