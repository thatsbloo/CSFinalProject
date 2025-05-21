using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Windows.Forms;

namespace YoavProject
{
    enum Messages { ServerExists, DenyServerReq, ConnectingReq }
    enum Data : byte { Position = 1, StateSync = 2 }
    
    
    static class UDP
    {
        public static int udpBroadcastPort = 1947;
        public static int regularCommunicationToServer = 1948;
        public static int regularCommunicationToClients = 1949;

        public static IPAddress serverAddress;

        static string byteToString(byte[] b) => Encoding.ASCII.GetString(b);
        static byte[] stringToByte(string str) => Encoding.ASCII.GetBytes(str);
        static byte[] messagesToByte(Messages m) => Encoding.ASCII.GetBytes(m.ToString());

        static bool isEqual(Messages m, byte[] bit)
        {
            return byteToString(bit) == m.ToString();
        }
        static bool isEqual(Messages m, string str)
        {
            return str == m.ToString();
        }
        public static bool serverDoesntExist()
        {
            UdpClient broadcaster = new UdpClient();
            broadcaster.EnableBroadcast = true;

            IPEndPoint allEndPoints = new IPEndPoint(IPAddress.Broadcast, udpBroadcastPort);
            byte[] byteServerExists = messagesToByte(Messages.ServerExists);

            broadcaster.Send(byteServerExists, byteServerExists.Length, allEndPoints);
            broadcaster.Client.ReceiveTimeout = 500;

            IPEndPoint receivingPoint;
            byte[] byteReply;

            bool res = true;
            try
            {
                receivingPoint = new IPEndPoint(IPAddress.Any, 0);
                byteReply = broadcaster.Receive(ref receivingPoint);
                broadcaster.Client.ReceiveTimeout = 500;

                if (isEqual(Messages.DenyServerReq, byteReply))
                {
                    serverAddress = receivingPoint.Address;
                    res = false;
                }
            } catch (Exception e) {
                Console.WriteLine(e.StackTrace);
            } finally { 
                broadcaster.Close();
            }
            return res;
            
        }

        public static void denyOthers()
        {
            UdpClient denier = new UdpClient();
            denier.Client.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, true);

            IPEndPoint address = new IPEndPoint(IPAddress.Any, udpBroadcastPort);
            denier.Client.Bind(address);

            byte[] byteDeny = messagesToByte(Messages.DenyServerReq);

            Thread blockOthers = new Thread(() =>
            {
                try
                {
                    while (true)
                    {
                        byte[] receivedMessage = denier.Receive(ref address);
                        bool isMessageServerExists = isEqual(Messages.ServerExists, receivedMessage);
                        if (isMessageServerExists)
                        {
                            denier.Send(byteDeny, byteDeny.Length, address);
                        }
                    }
                }
                catch (SocketException e)
                {
                    Console.WriteLine(e.StackTrace);
                }
            });
            blockOthers.IsBackground = true;
            blockOthers.Start();
        }

        public static void sendToServer(byte[] message)
        {
            if (serverAddress == null) {
                MessageBox.Show("Failed to send: " + message + ", serverAddress doesnt exist!");
                return;
            }

            UdpClient sender = new UdpClient();

            IPEndPoint serverEndpoint = new IPEndPoint(serverAddress, regularCommunicationToServer);

            sender.Send(message, message.Length, serverEndpoint);
            sender.Client.ReceiveTimeout = 500;

        }

        public static byte[] createByteMessage(Data type, float f1, float f2)
        {
           return createByteMessage(type, Game.clientId, f1, f2);
        }

        public static byte[] createByteMessage(Data type, int id, float f1, float f2)
        {
            byte[] bytes = new byte[1 + 1 + 4 + 4];
            bytes[0] = (byte)type;
            bytes[1] = (byte)id;

            Buffer.BlockCopy(BitConverter.GetBytes(f1), 0, bytes, 2, 4);
            Buffer.BlockCopy(BitConverter.GetBytes(f2), 0, bytes, 6, 4);

            if (!BitConverter.IsLittleEndian)
            {
                Array.Reverse(bytes, 2, 4);
                Array.Reverse(bytes, 6, 4);
            }

            return bytes;
        }
    }
}
