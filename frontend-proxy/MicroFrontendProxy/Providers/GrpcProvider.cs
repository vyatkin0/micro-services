using Grpc.Net.Client;
using System;

namespace MicroFrontendProxy.Providers
{
    public abstract class GrpcProvider : IDisposable
    {
        private GrpcChannel _channel;
        public GrpcProvider(string address)
        {
            _channel = GrpcChannel.ForAddress(address);
        }

        public GrpcChannel Channel { get => _channel; }
        public void Dispose()
        {
            _channel.Dispose();
        }
    };
}
