using System;
using System.Threading.Tasks;
using SocketHelpers.Discovery;

namespace Injector.DTO.Discovery
{
    public class InjectorServiceDefinition : JsonSerializedServiceDefinition<string, InjectorHostServiceResponse>
    {
        private readonly Func<Task<InjectorHostServiceResponse>> _getServiceResponse;
        private const string DiscoveryString = "Hi is there any injector host out there?";

        public InjectorServiceDefinition(Func<Task<InjectorHostServiceResponse>> getServiceResponseFunc = null)
        {
            DiscoveryPort = InjectorDefaults.DiscoveryPort;
            ResponsePort = InjectorDefaults.ResponsePort;

            _getServiceResponse = getServiceResponseFunc;
        }

        public override string DiscoveryRequest()
        {
            return DiscoveryString;
        }

        public override InjectorHostServiceResponse ResponseFor(string seekData)
        {
            return seekData == DiscoveryString ? Task.Run(() => _getServiceResponse()).Result : null;
        }
    }
}