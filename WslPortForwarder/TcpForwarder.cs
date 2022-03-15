using System;
using System.Net;
using System.Net.Sockets;

namespace WslPortForwarder;

public class TcpForwarder : TcpForwarderBase
{
    public ushort Port { get; }
    private readonly Action<ushort> _removePortAction;
    private readonly IPEndPoint _local;
    private readonly IPEndPoint _remote;

    public TcpForwarder(Action<ushort> removePortAction, string remoteIp, ushort port)
    {
        Port = port;
        _removePortAction = removePortAction;
        _local = new IPEndPoint(IPAddress.Any, port);
        _remote = new IPEndPoint(IPAddress.Parse(remoteIp), port);
    }

    public void Start()
    {
        MainSocket.Bind(_local);
        MainSocket.Listen(10);

        try
        {
            while (true)
            {
                var source = MainSocket.Accept();
                var destination = new TcpForwarderDestination();
                var state = new State(source, destination.MainSocket);
                destination.Connect(_remote, source);
                source.BeginReceive(state.Buffer, 0, state.Buffer.Length, 0, OnDataReceive, state);
            }
        }
        catch (SocketException)
        {
            _removePortAction.Invoke((ushort)_local.Port);
        }
    }

    public void Stop()
    {
        MainSocket.Close();
    }
}