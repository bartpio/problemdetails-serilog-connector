// Copyright 2023 problemdetails-serilog-connector Contributors
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
using Microsoft.Extensions.DependencyInjection;

namespace Serilog.AspNetCore
{
    /// <summary>
    /// Extends <see cref="IServiceCollection"/> with methods for registering Problem Details services.
    /// </summary>
    public static class ProblemDetailsExtensions
    {
        /// <summary>
        /// Adds the required services for <see cref="ProblemDetailsExtensions.UseProblemDetails(Microsoft.AspNetCore.Builder.IApplicationBuilder)"/> to work correctly,
        /// using the specified <paramref name="configure"/> callback for configuration.
        /// By default, configures options such that exceptions are never considered "unhandled" by Problem Details. We'll let Serilog's request logging take care of logging
        /// all exceptions, and deciding which level (ex. Information vs. Error) to log at.
        /// </summary>
        /// <param name="services">The service collection to add the services to.</param>
        /// <param name="configure"></param>
        /// <seealso cref="ProblemDetailsConnectorApplicationBuilderExtensions.UseSerilogRequestLoggingAndProblemDetails(Microsoft.AspNetCore.Builder.IApplicationBuilder, Action{RequestLoggingOptions}?)"/>
        /// <seealso cref="ProblemDetailsConnectorApplicationBuilderExtensions.UseSerilogRequestLoggingAndProblemDetails(Microsoft.AspNetCore.Builder.IApplicationBuilder, string)"/>
        public static IServiceCollection AddProblemDetailsAlongsideSerilog(this IServiceCollection services, Action<ProblemDetailsOptions>? configure = null)
        {
            Action<ProblemDetailsOptions> originalConfigure = configure ?? ((options) => { });
            Action<ProblemDetailsOptions> newConfigure = options =>
            {
                options.ShouldLogUnhandledException = (httpContext, exception, problemDetails) => false;
                originalConfigure(options);
            };

            services.AddProblemDetails(newConfigure);
            return services;
        }
    }
}
