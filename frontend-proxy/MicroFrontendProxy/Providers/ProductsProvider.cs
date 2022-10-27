using Microsoft.Extensions.Configuration;

namespace MicroFrontendProxy.Providers
{
    public class ProductsProvider : GrpcProvider
    {
        public ProductsProvider(IConfiguration configuration) : base(configuration["ProviderEndpoints:Products"])
        {
        }
    }
}
