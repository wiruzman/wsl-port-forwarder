using System.Net;
using System.Net.Sockets;

namespace WslPortForwarder
{
    public class TcpForwarderDestination : TcpForwarderBase
    {
        public void Connect(EndPoint remoteEndpoint, Socket destination)
        {
            var state = new State(MainSocket, destination);
            MainSocket.Connect(remoteEndpoint);
            MainSocket.BeginReceive(state.Buffer, 0, state.Buffer.Length, SocketFlags.None, OnDataReceive, state);
        }
    }
}