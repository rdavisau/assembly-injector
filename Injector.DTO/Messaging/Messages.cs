using SocketHelpers.Messaging;

namespace Injector.DTO.Messaging
{
    public class Message : IMessage, IMessageWithIProxy
    {
        public string FromGuid { get; set; }
        public IProxy FromProxy { get; set; }
    }

    public class NewAssemblyMessage : Message, IMessageWithAssemblyBytes
    {
        public string AssemblyName { get; set; }
        public byte[] AssemblyBytes { get; set; }
    }
}