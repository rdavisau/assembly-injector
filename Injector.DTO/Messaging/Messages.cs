using System;
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

    public class ErrorMessage : Message 
    {
        public string ExceptionType { get; set; }
        public string ExceptionMessage { get; set; }

        public ErrorMessage() { }

        public ErrorMessage(Exception e)
        {
            ExceptionType = e.GetType().FullName;
            ExceptionMessage = e.Message;
        }
    }
}