using System.Net.Sockets;

namespace WslPortForwarder
{
    public class State
    {
        public Socket SourceSocket { get; }
        public Socket DestinationSocket { get; }
        public byte[] Buffer { get; }

        public State(Socket source, Socket destination)
        {
            SourceSocket = source;
            DestinationSocket = destination;
            Buffer = new byte[8192];
        }
    }
}