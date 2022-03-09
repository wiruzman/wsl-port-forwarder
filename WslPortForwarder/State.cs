using System.Net.Sockets;

namespace WslPortForwarder
{
    public class State
    {
        public int Id { get; } // for debugging purposes
        public Socket SourceSocket { get; }
        public Socket DestinationSocket { get; }
        public byte[] Buffer { get; }
        public State(int id, Socket source, Socket destination)
        {
            Id = id;
            SourceSocket = source;
            DestinationSocket = destination;
            Buffer = new byte[8192];
        }
    }
}