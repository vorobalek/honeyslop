using Honeyslop.Admin.Access.Internals;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.DataProtection.KeyManagement;

namespace Honeyslop.Admin.Access;

public static class ServiceCollectionExtensions
{
    private const string ApplicationName = "honeyslop-admin";

    extension(IServiceCollection services)
    {
        public IServiceCollection AddSharedDataProtection()
        {
            services
                .AddDataProtection()
                .SetApplicationName(ApplicationName);
            services
                .AddOptions<KeyManagementOptions>()
                .Configure<IServiceScopeFactory, ILogger<DataProtectionKeyStore>>((options, serviceScopeFactory, logger) =>
                    options.XmlRepository = new DataProtectionKeyStore(serviceScopeFactory, logger));
            return services;
        }
    }
}
