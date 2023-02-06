# ProblemDetails.Serilog.AspNetCore.Middleware.Connector

[serilog/serilog-aspnetcore](https://github.com/serilog/serilog-aspnetcore) alongside  [khellang/Middleware](https://github.com/khellang/Middleware)


# Serilog Diagnostic Context

Serilog provides a convnient Diagnostic Context capable of capturing an exception that occured at some point during a request. The `RequestLoggingMiddleware` installed by `UseSerilogRequestLogging` [logs the exception captured in the context](https://github.com/serilog/serilog-aspnetcore/issues/270) when there is no unhandled exception flying.