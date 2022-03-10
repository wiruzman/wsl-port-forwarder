using System;
using System.Net;
using System.Net.Sockets;

namespace WslPortForwarder
{
    public class TcpForwarder
    {
        private readonly Socket _mainSocket = new(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);

        public void Start(IPEndPoint local, IPEndPoint remote)
        {
            _mainSocket.Bind(local);
            _mainSocket.Listen(10);

            while (true)
            {
                var source = _mainSocket.Accept();
                var destination = new TcpForwarder();
                var state = new State(source, destination._mainSocket);
                destination.Connect(remote, source);
                source.BeginReceive(state.Buffer, 0, state.Buffer.Length, 0, OnDataReceive, state);
            }
        }

        private void Connect(EndPoint remoteEndpoint, Socket destination)
        {
            var state = new State(_mainSocket, destination);
            _mainSocket.Connect(remoteEndpoint);
            _mainSocket.BeginReceive(state.Buffer, 0, state.Buffer.Length, SocketFlags.None, OnDataReceive, state);
        }

        private static void OnDataReceive(IAsyncResult result)
        {
            var state = result.AsyncState as State;
            try
            {
                if (state == null) return;
                var bytesRead = state.SourceSocket.EndReceive(result);
                if (bytesRead <= 0) return;
                state.DestinationSocket.Send(state.Buffer, bytesRead, SocketFlags.None);
                state.SourceSocket.BeginReceive(state.Buffer, 0, state.Buffer.Length, 0, OnDataReceive, state);
            }
            catch
            {
                state?.DestinationSocket.Close();
                state?.SourceSocket.Close();
            }
        }
    }
}