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

using Microsoft.AspNetCore.Http;

namespace Serilog.AspNetCore
{
    class RequestLoggingCaptureExceptionMiddleware
    {
        readonly RequestDelegate _next;
        readonly IDiagnosticContext _diagnosticContext;

        public RequestLoggingCaptureExceptionMiddleware(RequestDelegate next, IDiagnosticContext diagnosticContext)
        {
            _next = next ?? throw new ArgumentNullException(nameof(next));
            _diagnosticContext = diagnosticContext ?? throw new ArgumentNullException(nameof(diagnosticContext));
        }

        public async Task Invoke(HttpContext httpContext)
        {
            if (httpContext == null) throw new ArgumentNullException(nameof(httpContext));

            try
            {
                await _next(httpContext);
            }
            catch (Exception ex)
                // Never caught, because `CaptureException(...)` returns false.
                when (CaptureException(httpContext, ex))
            {
            }
        }

        bool CaptureException(HttpContext _, Exception ex)
        {
            _diagnosticContext.SetException(ex);
            return false;
        }
    }
}
