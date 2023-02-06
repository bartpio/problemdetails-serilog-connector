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
using Hellang.Middleware.ProblemDetails;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using ProblemDetails.Serilog.AspNetCore.Middleware.Connector.Tests.Support;
using Serilog;
using Serilog.AspNetCore;
using Serilog.Filters;
using Xunit;

// Newer frameworks provide IHostBuilder
#pragma warning disable CS0618

namespace ProblemDetails.Serilog.AspNetCore.Middleware.Connector.Tests
{
    public class ProblemDetailsConnectorApplicationBuilderExtensionsTests : IClassFixture<SerilogWebApplicationFactory>
    {
        readonly SerilogWebApplicationFactory _web;

        public ProblemDetailsConnectorApplicationBuilderExtensionsTests(SerilogWebApplicationFactory web)
        {
            _web = web;
        }


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
            var exception = new UnhandledException("something went wrong"); // actually handled thanks to ProblemDetails being engaged
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
            Assert.Contains("responded 501", renderedMessage); // as per ProblemDetails configuration in ConfigureProblemDetails below

            // this would fail (actual would be 2) if we called default AddProblemDetails instead of AddProblemDetailsAlongsideSerilog
            Assert.Single(sink.Writes.Where(x => exception.Equals(x.Exception)));
        }

        WebApplicationFactory<TestStartup> Setup(
            ILogger logger,
            bool dispose,
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
                            options.Logger = logger;
                            options.EnrichDiagnosticContext += (diagnosticContext, _) =>
                            {
                                diagnosticContext.Set("SomeString", "string");
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
                    })
                    .UseSerilog(logger, dispose));

            return web;
        }

        (SerilogSink, WebApplicationFactory<TestStartup>) Setup(
            Action<RequestLoggingOptions>? configureOptions = null,
            Action<HttpContext>? actionCallback = null)
        {
            var sink = new SerilogSink();
            var logger = new LoggerConfiguration()
                .Enrich.FromLogContext()
                .WriteTo.Sink(sink)
                .CreateLogger();

            var web = Setup(logger, true, configureOptions, actionCallback);

            return (sink, web);
        }

        internal static void ConfigureProblemDetails(Hellang.Middleware.ProblemDetails.ProblemDetailsOptions options)
        {
            options.MapToStatusCode<HandledException>(StatusCodes.Status418ImATeapot);
            options.MapToStatusCode<UnhandledException>(StatusCodes.Status501NotImplemented);
        }
    }
}