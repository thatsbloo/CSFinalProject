﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using static System.Windows.Forms.VisualStyles.VisualStyleElement.StartPanel;

namespace YoavProject
{
    enum Messages { ServerExists, DenyServerReq, ConnectingReq}

    enum Registration: byte { Register = 1, Login = 9, RegisterSuccess = 3, LoginSuccess = 4, ErrorTaken = 5, ErrorWrong = 6, ErrorInvalid = 7, ErrorLoggedIn = 8 }
    public enum Data : byte { Position = 1, CompleteStateSync = 2, PositionStateSync = 3, NewPlayer = 4, WorldStateSync = 5, ObjInteract = 6, InteractionStateSync = 7, ObjInteractSuccess = 8, EnterQueue = 9, GameStart = 10, CountdownStart = 11, CountdownStop = 12, WorldStateSyncGame = 13, GameStop = 14, Interval = 15, Score = 16, Winner = 17 } //objinteract
    enum InteractionTypes: byte { pickupPlate = 1, putdownPlate = 2, enterGame = 3, leaveGame = 4 }

    
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

        public static void sendToServer(byte[] message, UdpClient client)
        {
            if (serverAddress == null) {
                MessageBox.Show("Failed to send: " + message + ", serverAddress doesnt exist!");
                return;
            }

            IPEndPoint serverEndpoint = new IPEndPoint(serverAddress, regularCommunicationToServer);

            client.Send(message, message.Length, serverEndpoint);
            client.Client.ReceiveTimeout = 500;
            //sender.Close();

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
        public static byte[] createByteMessage(int id, float f1, float f2, string username)
        {
            List<byte> bytes = new List<byte>();
            bytes.Add((byte)id);

            byte[] float1 = BitConverter.GetBytes(f1);
            byte[] float2 = BitConverter.GetBytes(f2);

            byte[] usernamebytes = Encoding.UTF8.GetBytes(username);
            byte[] usernamebyteslength = BitConverter.GetBytes(usernamebytes.Length);
            if (!BitConverter.IsLittleEndian)
            {
                Array.Reverse(float1);
                Array.Reverse(float2);
                Array.Reverse(usernamebyteslength);
            }

            bytes.AddRange(float1);
            bytes.AddRange(float2);
            bytes.AddRange(usernamebyteslength);
            bytes.AddRange(usernamebytes);

            return bytes.ToArray();
        }

        public static byte[] createByteMessage(int id, float f1, float f2)
        {
            byte[] bytes = new byte[1 + 4 + 4];
            bytes[0] = (byte)id;

            Buffer.BlockCopy(BitConverter.GetBytes(f1), 0, bytes, 1, 4);
            Buffer.BlockCopy(BitConverter.GetBytes(f2), 0, bytes, 5, 4);

            if (!BitConverter.IsLittleEndian)
            {
                Array.Reverse(bytes, 1, 4);
                Array.Reverse(bytes, 5, 4);
            }

            return bytes;
        }

    }

    static class StreamHelp
    {
        public static int tcpPort = 6055;
        //public static async Task<byte[]> ReadExactlyAsync(this Stream stream, int length)
        //{
        //    byte[] buffer = new byte[length];
        //    int offset = 0;
        //    while (offset < length)
        //    {
        //        int bytesRead = await stream.ReadAsync(buffer, offset, length - offset);
        //        if (bytesRead == 0)
        //        {
        //            throw new IOException("Unexpected end of stream.");
        //        }
        //        offset += bytesRead;
        //    }
        //    return buffer;
        //}

        public static async Task<byte[]> ReadExactlyAsync(this Stream stream, int length)
        {
            Console.WriteLine($"[ReadExactlyAsync] Reading {length} bytes...");
            byte[] buffer = new byte[length];
            int offset = 0;

            while (offset < length)
            {
                int bytesRead = await stream.ReadAsync(buffer, offset, length - offset);
                if (bytesRead == 0)
                {
                    Console.WriteLine($"[ReadExactlyAsync] Unexpected end of stream at offset {offset}");
                    throw new IOException("Unexpected end of stream.");
                }

                offset += bytesRead;
                Console.WriteLine($"[ReadExactlyAsync] Read {bytesRead} bytes, total read: {offset}/{length}");
            }

            Console.WriteLine("[ReadExactlyAsync] Done reading.");
            return buffer;
        }

        public static async Task WriteEncrypted(this Stream stream, byte[] message, string AESkey)
        {
            byte[] enc = Encryption.encryptAES(message, AESkey);
            byte[] enclength = BitConverter.GetBytes(enc.Length);
            if (!BitConverter.IsLittleEndian)
            {
                Array.Reverse(enclength);
            }
            await stream.WriteAsync(enclength, 0, enclength.Length);
            await stream.WriteAsync(enc, 0, enc.Length);
        }

        public static async Task WriteEncryptedToAll(TcpClient[] clients, byte[] message, Dictionary<TcpClient, string> AESKeys)
        {
            Console.WriteLine(clients.Length);
            foreach (TcpClient client in clients)
            {
                await WriteEncrypted(client.GetStream(), message, AESKeys[client]);
            }
        }

        //public static async Task<byte[]> ReadEncrypted(this Stream stream, string AESkey)
        //{
        //    byte[] lengthbyte = await StreamHelp.ReadExactlyAsync(stream, 4);
        //    if (!BitConverter.IsLittleEndian)
        //        Array.Reverse(lengthbyte);

        //    int length = BitConverter.ToInt32(lengthbyte, 0);

        //    byte[] bytesenc = await StreamHelp.ReadExactlyAsync(stream, length);

        //    byte[] bytes = Encryption.decryptAES(bytesenc, AESkey);
        //    return bytes;
        //}

        public static async Task<byte[]> ReadEncrypted(this Stream stream, string AESkey)
        {
            Console.WriteLine("[ReadEncrypted] Reading length...");
            byte[] lengthbyte = await StreamHelp.ReadExactlyAsync(stream, 4);
            Console.WriteLine("[ReadEncrypted] Raw length bytes: " + BitConverter.ToString(lengthbyte));

            if (!BitConverter.IsLittleEndian)
                Array.Reverse(lengthbyte);

            int length = BitConverter.ToInt32(lengthbyte, 0);
            Console.WriteLine($"[ReadEncrypted] Encrypted payload length: {length}");

            byte[] bytesenc = await StreamHelp.ReadExactlyAsync(stream, length);
            Console.WriteLine($"[ReadEncrypted] Encrypted bytes: {BitConverter.ToString(bytesenc)}");

            byte[] bytes = Encryption.decryptAES(bytesenc, AESkey);
            Console.WriteLine($"[ReadEncrypted] Decrypted bytes: {BitConverter.ToString(bytes)}");

            return bytes;
        }
    }
}
