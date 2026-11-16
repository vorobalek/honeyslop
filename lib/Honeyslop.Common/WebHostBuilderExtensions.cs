using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Honeyslop.Common;

public static class WebHostBuilderExtensions
{
    extension(IWebHostBuilder builder)
    {
        public IWebHostBuilder AddGlobalCancellationToken()
        {
            return builder.ConfigureServices(services => services
                .AddSingleton<IGlobalCancellationTokenSource, GlobalCancellationTokenSource>());
        }

        public IWebHostBuilder AddJsonConsoleLogging()
        {
            return builder.ConfigureLogging(logging => logging
                .ClearProviders()
                .AddJsonConsole(options =>
                {
                    options.UseUtcTimestamp = true;
                    options.TimestampFormat = "yyyy-MM-ddTHH:mm:ss.fffZ";
                }));
        }
    }
}
