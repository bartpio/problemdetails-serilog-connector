// Copyright 2023 problemdetails-serilog-connector Contributors
// Copyright 2019-2020 Serilog Contributors
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

using Hellang.Middleware.ProblemDetails;
using Microsoft.AspNetCore.Builder;

namespace Serilog.AspNetCore
{
    /// <summary>
    /// Extends <see cref="IApplicationBuilder"/> with methods for configuring Serilog and Problem Details features.
    /// </summary>
    public static class ProblemDetailsConnectorApplicationBuilderExtensions
    {
        /// <summary>
        /// Adds Serilog middleware for streamlined request logging, and Problem Details middleware to catch exceptions and convert them to responses.
        /// Adds exception-capturing middleware that places exceptions considered "handled" by the Problem Details middlware into Serilog's
        /// Diagnostic Context.
        /// </summary>
        /// <param name="app">The application builder.</param>
        /// <param name="configureOptions">A System.Action`1 to configure the provided Serilog.AspNetCore.RequestLoggingOptions.</param>
        /// <returns>The application builder.</returns>
        /// <seealso cref="SerilogApplicationBuilderExtensions.UseSerilogRequestLogging(IApplicationBuilder, Action{RequestLoggingOptions})"/>
        /// <seealso cref="ProblemDetailsExtensions.UseProblemDetails(IApplicationBuilder)"/>
        /// <seealso cref="CaptureExceptionApplicationBuilderExtensions.UseSerilogRequestLoggingCaptureException(IApplicationBuilder)"/>
        public static IApplicationBuilder UseSerilogRequestLoggingAndProblemDetails(this IApplicationBuilder app, Action<RequestLoggingOptions>? configureOptions = null)
        {
            app.UseSerilogRequestLogging(configureOptions);
            app.UseProblemDetails();
            app.UseSerilogRequestLoggingCaptureException();
            return app;
        }

        /// <summary>
        /// Adds Serilog middleware for streamlined request logging, and Problem Details middleware to catch exceptions and convert them to responses.
        /// Adds exception-capturing middleware that places exceptions considered "handled" by the Problem Details middlware into Serilog's
        /// Diagnostic Context.
        /// </summary>
        /// <param name="app">The application builder.</param>
        /// <param name="messageTemplate">The message template to use when logging request completion events.</returns>
        /// <seealso cref="SerilogApplicationBuilderExtensions.UseSerilogRequestLogging(IApplicationBuilder, string)" />
        /// <seealso cref="ProblemDetailsExtensions.UseProblemDetails(IApplicationBuilder)"/>
        /// <seealso cref="CaptureExceptionApplicationBuilderExtensions.UseSerilogRequestLoggingCaptureException(IApplicationBuilder)"/>
        public static IApplicationBuilder UseSerilogRequestLoggingAndProblemDetails(this IApplicationBuilder app, string messageTemplate)
        {
            app.UseSerilogRequestLogging(messageTemplate);
            app.UseProblemDetails();
            app.UseSerilogRequestLoggingCaptureException();
            return app;
        }
    }
}
