// Copyright 2023 problemdetails-serilog-connector Contributors
// Copyright 2019-2020 Serilog Contributors
// Copyright (c) .NET Foundation. All rights reserved.
//
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
//     http://www.apache.org/licenses/LICENSE-2.0
//
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.

using System.Globalization;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using ProblemDetails.Serilog.AspNetCore.Middleware.Connector.Tests.Support;
using Serilog.AspNetCore;
using Serilog.Events;
using Serilog.Filters;
using Xunit;

// Newer frameworks provide IHostBuilder
#pragma warning disable CS0618

namespace ProblemDetails.Serilog.AspNetCore.Middleware.Connector.Tests
{
    public class ProblemDetailsConnectorApplicationBuilderExtensionsTests
    {
        private readonly SerilogWebApplicationFactory _web = new();

        [Fact]
        public async Task RequestLoggingMiddlewareShouldEnrichWithCollectedExceptionIfNoUnhandledException()
        {
            var diagnosticContextException = new Exception("Exception set in diagnostic context");
            var (sink, web) = Setup(options =>
            {
                options.EnrichDiagnosticContext += (diagnosticContext, _) =>
                {
                    diagnosticContext.SetException(diagnosticContextException);
                };
            });

            await web.CreateClient().GetAsync("/resource");

            Assert.NotEmpty(sink.Writes);

            var completionEvent = sink.Writes.First(logEvent => Matching.FromSource("Serilog.AspNetCore.RequestLoggingMiddleware")(logEvent));
            Assert.Same(diagnosticContextException, completionEvent.Exception);
        }

        [Fact]
        public async Task RequestLoggingMiddlewareShouldEnrichWithCollectedExceptionWhenHandledByProblemDetails()
        {
            var exception = new HandledException("some user error"); // handled thanks to ProblemDetails being engaged
            var (sink, web) = Setup(options =>
            {
                // don't artificially SetException here for this scenario
            }, hc =>
            {
                throw exception; // not handled here, but will be by ProblemDetails
            });

            await web.CreateClient().GetAsync("/resource");

            var completionEvent = sink.Writes.First(logEvent => Matching.FromSource("Serilog.AspNetCore.RequestLoggingMiddleware")(logEvent));
            Assert.Same(exception, completionEvent.Exception);

            var renderedMessage = completionEvent.RenderMessage(CultureInfo.InvariantCulture);
            Assert.Contains("responded 418", renderedMessage); // as per ProblemDetails configuration in ConfigureProblemDetails below

            Assert.Single(sink.Writes.Where(x => exception.Equals(x.Exception)));
        }

        [Fact]
        public async Task ProblemDetailsShouldNotLogWhatItWouldHaveConsideredUnhandledByDefault()
        {
            var (exception, sink) = await RunSomethingWentWrongScenario();

            var completionEvent = sink.Writes.First(logEvent => Matching.FromSource("Serilog.AspNetCore.RequestLoggingMiddleware")(logEvent));
            Assert.Same(exception, completionEvent.Exception);

            var renderedMessage = completionEvent.RenderMessage(CultureInfo.InvariantCulture);
            Assert.Contains("responded 501", renderedMessage); // as per ProblemDetails configuration in ConfigureProblemDetails below

            // this would fail (actual would be 2) if we called default AddProblemDetails instead of AddProblemDetailsAlongsideSerilog
            Assert.Single(sink.Writes.Where(x => exception.Equals(x.Exception)));
        }

        private async Task<(UnhandledException exception, SerilogSink sink)> RunSomethingWentWrongScenario(Action<HttpContext>? preExceptionCallback = null)
        {
            var exception = new UnhandledException("something went wrong");
            var (sink, web) = Setup(options =>
            {
                // don't artificially SetException here for this scenario
            }, hc =>
            {
                preExceptionCallback?.Invoke(hc);
                throw exception; // not handled here, but will be by ProblemDetails
            });

            await web.CreateClient().GetAsync("/resource");
            return (exception, sink);
        }

        [Fact]
        public async Task ProblemDetailsWorksAlongsideInfo()
        {
            var (exception, sink) = await RunSomethingWentWrongScenario(hc =>
            {
                var logger = hc.RequestServices.GetRequiredService<ILogger<ProblemDetailsConnectorApplicationBuilderExtensionsTests>>();
                logger.LogInformation("some info");
            });

            var completionEvent = sink.Writes.First(logEvent => Matching.FromSource("Serilog.AspNetCore.RequestLoggingMiddleware")(logEvent));
            Assert.Same(exception, completionEvent.Exception);
            Assert.Equal("indeed some string", GetString(completionEvent, "SomeString"));

            // ProblemDetails.Serilog.AspNetCore.Middleware.Connector.Tests.ProblemDetailsConnectorApplicationBuilderExtensionsTests
            var source = GetType().FullName ?? throw new InvalidOperationException("test fixture type must have a name");
            var infoEvent = sink.Writes.First(logEvent => Matching.FromSource(source)(logEvent));
            Assert.Null(infoEvent.Exception);
            Assert.Equal("some info", infoEvent.RenderMessage(CultureInfo.InvariantCulture));

            // EnrichDiagnosticContext applies only to completion event
            Assert.Null(GetString(infoEvent, "SomeString"));
        }

        private string? GetString(LogEvent logEvent, string key)
        {
            try
            {
                return logEvent.Properties[key] switch
                {
                    ScalarValue scalar => scalar.Value as string,
                    _ => throw new InvalidOperationException("log event property isn't scalar")
                };
            }
            catch (KeyNotFoundException)
            {
                return null;
            }
        }

        (SerilogSink, WebApplicationFactory<TestStartup>) Setup(
            Action<RequestLoggingOptions>? configureOptions = null,
            Action<HttpContext>? actionCallback = null)
        {
            var web = _web.WithWebHostBuilder(
                builder => builder
                    .ConfigureServices(sc =>
                    {
                        sc.AddProblemDetailsAlongsideSerilog(ConfigureProblemDetails);
                        sc.AddControllers().AddApplicationPart(typeof(TestStartup).Assembly);

                        sc.Configure<RequestLoggingOptions>(options =>
                        {
                            options.Logger = _web.Logger;
                            options.EnrichDiagnosticContext += (diagnosticContext, _) =>
                            {
                                diagnosticContext.Set("SomeString", "indeed some string");
                            };
                        });
                    })
                    .Configure(app =>
                    {
                        app.UseSerilogRequestLoggingAndProblemDetails(configureOptions);
                        app.Run(ctx =>
                        {
                            actionCallback?.Invoke(ctx);
                            return Task.CompletedTask;
                        }); // 200 OK
                    }));

            return (_web.Sink, web);
        }

        internal static void ConfigureProblemDetails(Hellang.Middleware.ProblemDetails.ProblemDetailsOptions options)
        {
            options.MapToStatusCode<HandledException>(StatusCodes.Status418ImATeapot);
            options.MapToStatusCode<UnhandledException>(StatusCodes.Status501NotImplemented);
        }
    }
}