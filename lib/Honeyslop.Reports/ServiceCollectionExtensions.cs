using Honeyslop.Reports.Internals;
using Microsoft.Extensions.DependencyInjection;

namespace Honeyslop.Reports;

public static class ServiceCollectionExtensions
{
    extension(IServiceCollection services)
    {
        public IServiceCollection AddRequestArchives()
        {
            return services.AddScoped<IRequestArchiveBuilder, RequestArchiveBuilder>();
        }
    }
}