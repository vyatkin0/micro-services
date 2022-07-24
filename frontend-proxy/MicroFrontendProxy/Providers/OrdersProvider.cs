using Microsoft.Extensions.Configuration;

namespace MicroFrontendProxy.Providers
{
    public class OrdersProvider : GrpcProvider
    {
        public OrdersProvider(IConfiguration configuration) : base(configuration["ProviderEndpoints:Orders"])
        {
        }
    }
}
