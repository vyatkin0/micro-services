using Microsoft.Extensions.Configuration;

namespace MicroFrontendProxy.Providers
{
    public class IdentityProvider : GrpcProvider
    {
        public IdentityProvider(IConfiguration configuration) : base(configuration["ProviderEndpoints:Identity"])
        {
        }
    }
}
