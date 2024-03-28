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

using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Hosting;
using Serilog;
using Serilog.Core;

namespace ProblemDetails.Serilog.AspNetCore.Middleware.Connector.Tests.Support;

public class SerilogWebApplicationFactory : WebApplicationFactory<TestStartup>
{
    private readonly Lazy<Logger> _logger;

    public SerilogSink Sink { get; } = new SerilogSink();

    public Logger Logger => _logger.Value;

    public SerilogWebApplicationFactory()
    {
        _logger = new(() => new LoggerConfiguration()
            .Enrich.FromLogContext()
            .WriteTo.Sink(Sink)
            .CreateLogger(), LazyThreadSafetyMode.ExecutionAndPublication);
    }

    protected override IHostBuilder CreateHostBuilder() => Host.CreateDefaultBuilder().UseSerilog(Logger, true);

    protected override void ConfigureWebHost(IWebHostBuilder builder) => builder.UseContentRoot(".");
}

public class TestStartup { }
