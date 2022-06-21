using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace ReModCE_ARES.ApplicationBot
{
    internal class SocketConnection : VRCModule
    {
        public static void SendCommandToClients(string Command)
        {
            Console.WriteLine($"[{DateTime.Now}] [Server] Sending Message ({Command})");
            List<Socket> sockets = ServerHandlers.Where(s => s != null).ToList();
            foreach (Socket socket in sockets)
            {
                try
                {
                    socket.Send(Encoding.ASCII.GetBytes(Command));
                }
                catch { }
            }
        }

        public static void OnClientReceiveCommand(string Command)
        {
            Console.WriteLine($"[{DateTime.Now}] [Client] Received Message ({Command})");
            Bot.ReceiveCommand(Command);
        }

        private static List<Socket> ServerHandlers = new List<Socket>();

        public static void StartServer()
        {
            ServerHandlers.Clear();
            Task.Run(HandleServer);
        }

        private static void HandleServer()
        {
            IPHostEntry host = Dns.GetHostEntry("localhost");
            IPAddress ipAddress = host.AddressList[0];
            IPEndPoint localEndPoint = new IPEndPoint(ipAddress, 11000);

            try
            {
                Socket listener = new Socket(ipAddress.AddressFamily, SocketType.Stream, ProtocolType.Tcp);
                listener.Bind(localEndPoint);
                listener.Listen(10);
                Console.WriteLine($"[Server] Waiting for connections...");
                do
                {
                    ServerHandlers.Add(listener.Accept());
                } while (true);
            }
            catch (Exception e)
            {
                Console.WriteLine("[Server] " + e.ToString());
            }
        }

        public static void Client()
        {
            Task.Run(HandleClient);
        }

        private static void HandleClient()
        {
            byte[] bytes = new byte[1024];

            try
            {
                IPHostEntry host = Dns.GetHostEntry("localhost");
                IPAddress ipAddress = host.AddressList[0];
                IPEndPoint remoteEP = new IPEndPoint(ipAddress, 11000);

                Socket sender = new Socket(ipAddress.AddressFamily,
                    SocketType.Stream, ProtocolType.Tcp);

                try
                {
                    sender.Connect(remoteEP);

                    Console.WriteLine("[Client] Socket connected to {0}",
                        sender.RemoteEndPoint.ToString());

                    for (; ; )
                    {
                        int bytesRec = sender.Receive(bytes);
                        OnClientReceiveCommand(Encoding.ASCII.GetString(bytes, 0, bytesRec));
                    }
                }
                catch (ArgumentNullException ane)
                {
                    Console.WriteLine("[Client] ArgumentNullException : {0}", ane.ToString());
                }
                catch (SocketException se)
                {
                    Console.WriteLine("[Client] SocketException : {0}", se.ToString());
                }
                catch (Exception e)
                {
                    Console.WriteLine("[Client] Unexpected exception : {0}", e.ToString());
                }
            }
            catch (Exception e)
            {
                Console.WriteLine("[Client] " + e.ToString());
            }
        }
    }
}