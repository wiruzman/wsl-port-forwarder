using System;
using System.Net;
using System.Net.Sockets;

namespace WslPortForwarder
{
    public class TcpForwarder
    {
        public void Start(IPEndPoint local, IPEndPoint remote)
        {
            Socket mainSocket;
            try
            {
                mainSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
                mainSocket.Bind(local);
                mainSocket.Listen(10);
            }
            catch (Exception exp)
            {
                Console.WriteLine("Error on listening to " + local.Port + ": " + exp.Message);
                return;
            }

            while (true)
            {
                // Accept a new client
                var socketSrc = mainSocket.Accept();
                var socketDest = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);

                try
                {
                    // Connect to the endpoint
                    socketDest.Connect(remote);
                }
                catch
                {
                    socketSrc.Shutdown(SocketShutdown.Both);
                    socketSrc.Close();
                    Console.WriteLine("Exception in connecting to remote host");
                    continue;
                }

                // Wait for data sent from client and forward it to the endpoint
                StartReceive(0, socketSrc, socketDest);

                // Also, wait for data sent from endpoint and forward it to the client
                StartReceive(1, socketDest, socketSrc);
            }
        }

        private static void StartReceive(int id, Socket src, Socket dest)
        {
            var state = new State(id, src, dest);

            Console.WriteLine("{0} StartReceive: BeginReceive", id);
            try
            {
                src.BeginReceive(state.Buffer, 0, state.Buffer.Length, 0, OnDataReceive, state);
            }
            catch
            {
                Console.WriteLine("{0} Exception in StartReceive: BeginReceive", id);
            }
        }

        private static void OnDataReceive(IAsyncResult? result)
        {
            State? state = null;
            try
            {
                if (result == null) throw new NullReferenceException($"{nameof(result)} is null.");
                state = (State?)result.AsyncState;

                if (state == null) throw new NullReferenceException($"{nameof(state)} is null.");
                Console.WriteLine("{0} OnDataReceive: EndReceive", state.Id);
                var bytesRead = state.SourceSocket.EndReceive(result);
                if (bytesRead > 0)
                {
                    state.DestinationSocket.Send(state.Buffer, bytesRead, SocketFlags.None);

                    Console.WriteLine("{0} OnDataReceive: BeginReceive", state.Id);
                    state.SourceSocket.BeginReceive(state.Buffer, 0, state.Buffer.Length, 0, OnDataReceive, state);
                }
                else
                {
                    Console.WriteLine("{0} OnDataReceive: Close (0 read)", state.Id);
                    state.SourceSocket.Shutdown(SocketShutdown.Both);
                    state.DestinationSocket.Shutdown(SocketShutdown.Both);
                    state.DestinationSocket.Close();
                    state.SourceSocket.Close();
                }
            }
            catch (NullReferenceException e)
            {
                Console.WriteLine(e.Message);
            }
            catch (Exception e) when(e is not NullReferenceException)
            {
                if (state != null)
                {
                    Console.WriteLine("{0} OnDataReceive: Close (exception)", state.Id);
                    state.SourceSocket.Shutdown(SocketShutdown.Both);
                    state.DestinationSocket.Shutdown(SocketShutdown.Both);
                    state.DestinationSocket.Close();
                    state.SourceSocket.Close();
                }
            }
        }
    }
}