using System.Collections.Generic;

namespace Injector.DTO.Discovery
{
    public class InjectorHostServiceResponse
    {
        public string ServiceGuid { get; set; }

        public List<string> RunningAt { get; set; }
        public int Port { get; set; }

        public string ServiceName { get; set; }
        public int NumConnectedClients { get; set; }

        public InjectorHostServiceResponse()
        {
            RunningAt = new List<string>();
        }
    }
}