using SocketHelpers.Messaging;

namespace Injector.DTO.Messaging
{
    public interface IMessageWithIProxy
    {
        IProxy FromProxy { get; set; }
    }

    public interface IMessageWithAssemblyBytes : IMessage, IMessageWithIProxy
    {
        string AssemblyName { get; set; }
        byte[] AssemblyBytes { get; set; }
    }
}