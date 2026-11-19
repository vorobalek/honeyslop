using Honeyslop.Common.Serializers;
using Honeyslop.Llm.Contracts;
using Honeyslop.Llm.Internals;
using Honeyslop.Llm.Serializers.Internals;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace Honeyslop.Llm;

public static class ServiceCollectionExtensions
{
    extension(IServiceCollection services)
    {
        public IServiceCollection AddLlm(Action<LlmOptions> configure)
        {
            services
                .AddOptions<LlmOptions>()
                .Configure(configure);
            services
                .AddHttpClient(LlmClient.HttpClientName, (serviceProvider, httpClient) => httpClient.Timeout = serviceProvider
                    .GetRequiredService<IOptions<LlmOptions>>()
                    .Value
                    .Timeout);
            return services
                .AddSingleton<ILlmEndpointProvider, LlmEndpointProvider>()
                .AddSingleton<ILlmClient, LlmClient>()
                .AddSingleton<IJsonSerializer<GenerationOutputDto>, GenerationOutputSerializer>();
        }
    }
}