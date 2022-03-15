using System;
using System.Net.Sockets;

namespace WslPortForwarder;

public class TcpForwarderBase
{
    public readonly Socket MainSocket = new(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);

    protected static void OnDataReceive(IAsyncResult result)
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