﻿/*using Infinity.Core.Udp;
using Infinity.Server;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using Infinity.Client.Unity;

namespace Infinity.Core.Tests
{
    public class UnityUdpConnectionTests
    {
        private ILogger logger = new ConsoleLogger(true);

        [Fact]
        public void ServerDisposeDisconnectsTest()
        {
            IPEndPoint ep = new IPEndPoint(IPAddress.Loopback, 4296);

            bool serverConnected = false;
            bool serverDisconnected = false;
            bool clientDisconnected = false;

            using (UdpConnectionListener listener = new UdpConnectionListener(new IPEndPoint(IPAddress.Any, 4296)))
            using (UdpConnection connection = new UnityUdpClientConnection(logger, ep))
            {
                listener.NewConnection += (evt) =>
                {
                    serverConnected = true;
                    evt.Connection.Disconnected += (o, et) => serverDisconnected = true;
                };
                connection.Disconnected += (o, evt) => clientDisconnected = true;

                listener.Start();
                connection.Connect();

                Thread.Sleep(100); // Gotta wait for the server to set up the events.
                listener.Dispose();
                Thread.Sleep(100);

                Assert.True(serverConnected);
                Assert.True(clientDisconnected);
                Assert.False(serverDisconnected);
            }
        }

        [Fact]
        public void ClientServerDisposeDisconnectsTest()
        {
            IPEndPoint ep = new IPEndPoint(IPAddress.Loopback, 4296);

            bool serverConnected = false;
            bool serverDisconnected = false;
            bool clientDisconnected = false;

            using (UdpConnectionListener listener = new UdpConnectionListener(new IPEndPoint(IPAddress.Any, 4296)))
            using (UdpConnection connection = new UnityUdpClientConnection(logger, ep))
            {
                listener.NewConnection += (evt) =>
                {
                    serverConnected = true;
                    evt.Connection.Disconnected += (o, et) => serverDisconnected = true;
                };

                connection.Disconnected += (o, et) => clientDisconnected = true;

                listener.Start();
                connection.Connect();

                Thread.Sleep(100); // Gotta wait for the server to set up the events.
                connection.Dispose();

                Thread.Sleep(100);

                Assert.True(serverConnected);
                Assert.True(serverDisconnected);
                Assert.False(clientDisconnected);
            }
        }

        /// <summary>
        ///     Tests the fields on UdpConnection.
        /// </summary>
        [Fact]
        public void UdpFieldTest()
        {
            IPEndPoint ep = new IPEndPoint(IPAddress.Loopback, 4296);

            using (UdpConnectionListener listener = new UdpConnectionListener(new IPEndPoint(IPAddress.Any, 4296)))
            using (UdpConnection connection = new UnityUdpClientConnection(logger, ep))
            {
                listener.Start();

                connection.Connect();

                //Connection fields
                Assert.Equal(ep, connection.EndPoint);

                //UdpConnection fields
                Assert.Equal(new IPEndPoint(IPAddress.Loopback, 4296), connection.EndPoint);
                Assert.Equal(1, connection.Statistics.DataBytesSent);
                Assert.Equal(0, connection.Statistics.DataBytesReceived);
            }
        }

        [Fact]
        public void UdpHandshakeTest()
        {
            byte[] TestData = new byte[] { 1, 2, 3, 4, 5, 6 };

            using (ManualResetEventSlim mutex = new ManualResetEventSlim(false))
            using (UdpConnectionListener listener = new UdpConnectionListener(new IPEndPoint(IPAddress.Any, 4296)))
            using (UdpConnection connection = new UnityUdpClientConnection(logger, new IPEndPoint(IPAddress.Loopback, 4296)))
            {
                listener.Start();

                MessageReader output = null;
                listener.NewConnection += delegate (NewConnectionEventArgs e)
                {
                    output = e.HandshakeData.Duplicate();
                    mutex.Set();
                };

                connection.Connect(TestData);
                mutex.Wait(5000);

                for (int i = 0; i < TestData.Length; ++i)
                {
                    Assert.Equal(TestData[i], output.ReadByte());
                }
            }
        }

        [Fact]
        public void UdpUnreliableMessageSendTest()
        {
            byte[] TestData = new byte[] { 1, 2, 3, 4, 5, 6 };
            using (UdpConnectionListener listener = new UdpConnectionListener(new IPEndPoint(IPAddress.Any, 4296)))
            using (UdpConnection connection = new UnityUdpClientConnection(logger, new IPEndPoint(IPAddress.Loopback, 4296)))
            {
                MessageReader output = null;
                listener.NewConnection += delegate (NewConnectionEventArgs e)
                {
                    e.Connection.DataReceived += delegate (DataReceivedEventArgs evt)
                    {
                        output = evt.Message;
                    };
                };

                listener.Start();
                connection.Connect();

                for (int i = 0; i < 4; ++i)
                {
                    var msg = MessageWriter.Get(SendOption.None);
                    msg.Write(TestData);
                    connection.Send(msg);
                    msg.Recycle();
                }

                Thread.Sleep(10);
                for (int i = 0; i < TestData.Length; ++i)
                {
                    Assert.Equal(TestData[i], output.ReadByte());
                }
            }
        }

        /// <summary>
        ///     Tests IPv4 connectivity.
        /// </summary>
        [Fact]
        public void UdpIPv4ConnectionTest()
        {
            using (UdpConnectionListener listener = new UdpConnectionListener(new IPEndPoint(IPAddress.Any, 4296)))
            using (UdpConnection connection = new UnityUdpClientConnection(logger, new IPEndPoint(IPAddress.Loopback, 4296)))
            {
                listener.Start();

                connection.Connect();
            }
        }

        /// <summary>
        ///     Tests dual mode connectivity.
        /// </summary>
        [Fact]
        public void MixedConnectionTest()
        {
            using (UdpConnectionListener listener2 = new UdpConnectionListener(new IPEndPoint(IPAddress.IPv6Any, 4296), IPMode.IPv6))
            {
                listener2.Start();

                listener2.NewConnection += (evt) =>
                {
                    Console.WriteLine("v6 connection: " + ((NetworkConnection)evt.Connection).GetIP4Address());
                };

                using (UdpConnection connection = new UnityUdpClientConnection(logger, new IPEndPoint(IPAddress.Parse("127.0.0.1"), 4296)))
                {
                    connection.Connect();
                    Assert.Equal(ConnectionState.Connected, connection.State);
                }

                using (UdpConnection connection = new UnityUdpClientConnection(logger, new IPEndPoint(IPAddress.IPv6Loopback, 4296), IPMode.IPv6))
                {
                    connection.Connect();
                    Assert.Equal(ConnectionState.Connected, connection.State);
                }
            }
        }

        /// <summary>
        ///     Tests IPv4 resilience to non-hello connections.
        /// </summary>
        [Fact]
        public void FalseConnectionTest()
        {
            using (UdpConnectionListener listener = new UdpConnectionListener(new IPEndPoint(IPAddress.Any, 4296)))
            using (Socket socket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp))
            {
                int connects = 0;
                listener.NewConnection += (obj) =>
                {
                    Interlocked.Increment(ref connects);
                };

                listener.Start();

                socket.Bind(new IPEndPoint(IPAddress.Any, 0));
                var bytes = new byte[2];
                bytes[0] = (byte)32;
                for (int i = 0; i < 10; ++i)
                {
                    socket.SendTo(bytes, new IPEndPoint(IPAddress.Loopback, 4296));
                }

                Thread.Sleep(500);

                Assert.Equal(0, connects);
            }
        }

        /// <summary>
        ///     Tests IPv4 resilience to multiple hellos.
        /// </summary>
        [Fact]
        public void ConnectLikeAJerkTest()
        {
            using (UdpConnectionListener listener = new UdpConnectionListener(new IPEndPoint(IPAddress.Any, 4296)))
            using (Socket socket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp))
            {
                int connects = 0;
                listener.NewConnection += (obj) =>
                {
                    Interlocked.Increment(ref connects);
                };

                listener.Start();

                socket.Bind(new IPEndPoint(IPAddress.Any, 0));
                var bytes = new byte[2];
                bytes[0] = (byte)UdpSendOption.Hello;
                for (int i = 0; i < 10; ++i)
                {
                    socket.SendTo(bytes, new IPEndPoint(IPAddress.Loopback, 4296));
                }

                Thread.Sleep(500);

                Assert.Equal(1, connects);
            }
        }

        /// <summary>
        ///     Tests dual mode connectivity.
        /// </summary>
        [Fact]
        public void UdpIPv6ConnectionTest()
        {
            using (UdpConnectionListener listener = new UdpConnectionListener(new IPEndPoint(IPAddress.IPv6Any, 4296), IPMode.IPv6))
            {
                listener.Start();

                using (UdpConnection connection = new UnityUdpClientConnection(logger, new IPEndPoint(IPAddress.Parse("127.0.0.1"), 4296), IPMode.IPv6))
                {
                    connection.Connect();
                }
            }
        }

        /// <summary>
        ///     Tests server to client unreliable communication on the UdpConnection.
        /// </summary>
        [Fact]
        public void UdpUnreliableServerToClientTest()
        {
            using (UdpConnectionListener listener = new UdpConnectionListener(new IPEndPoint(IPAddress.Any, 4296)))
            using (UdpConnection connection = new UnityUdpClientConnection(logger, new IPEndPoint(IPAddress.Loopback, 4296)))
            {
                TestHelper.RunServerToClientTest(listener, connection, 10, SendOption.None);
            }
        }

        /// <summary>
        ///     Tests server to client reliable communication on the UdpConnection.
        /// </summary>
        [Fact]
        public void UdpReliableServerToClientTest()
        {
            using (UdpConnectionListener listener = new UdpConnectionListener(new IPEndPoint(IPAddress.Any, 4296)))
            using (UdpConnection connection = new UnityUdpClientConnection(logger, new IPEndPoint(IPAddress.Loopback, 4296)))
            {
                TestHelper.RunServerToClientTest(listener, connection, 10, SendOption.Reliable);
            }
        }

        /// <summary>
        ///     Tests server to client unreliable communication on the UdpConnection.
        /// </summary>
        [Fact]
        public void UdpUnreliableClientToServerTest()
        {
            using (UdpConnectionListener listener = new UdpConnectionListener(new IPEndPoint(IPAddress.Any, 4296)))
            using (UdpConnection connection = new UnityUdpClientConnection(logger, new IPEndPoint(IPAddress.Loopback, 4296)))
            {
                TestHelper.RunClientToServerTest(listener, connection, 10, SendOption.None);
            }
        }

        /// <summary>
        ///     Tests server to client reliable communication on the UdpConnection.
        /// </summary>
        [Fact]
        public void UdpReliableClientToServerTest()
        {
            using (UdpConnectionListener listener = new UdpConnectionListener(new IPEndPoint(IPAddress.Any, 4296)))
            using (UdpConnection connection = new UnityUdpClientConnection(logger, new IPEndPoint(IPAddress.Loopback, 4296)))
            {
                TestHelper.RunClientToServerTest(listener, connection, 10, SendOption.Reliable);
            }
        }

        /// <summary>
        ///     Tests the keepalive functionality from the client,
        /// </summary>
        [Fact]
        public void KeepAliveClientTest()
        {
            using (UdpConnectionListener listener = new UdpConnectionListener(new IPEndPoint(IPAddress.Any, 4296)))
            using (UdpConnection connection = new UnityUdpClientConnection(logger, new IPEndPoint(IPAddress.Loopback, 4296)))
            {
                listener.Start();

                connection.Connect();
                connection.KeepAliveInterval = 100;

                Thread.Sleep(1050);    //Enough time for ~10 keep alive packets

                Assert.Equal(ConnectionState.Connected, connection.State);
                Assert.True(
                    connection.Statistics.TotalBytesSent >= 30 &&
                    connection.Statistics.TotalBytesSent <= 50,
                    "Sent: " + connection.Statistics.TotalBytesSent
                );
            }
        }

        /// <summary>
        ///     Tests the keepalive functionality from the client,
        /// </summary>
        [Fact]
        public void KeepAliveServerTest()
        {
            ManualResetEvent mutex = new ManualResetEvent(false);

            using (UdpConnectionListener listener = new UdpConnectionListener(new IPEndPoint(IPAddress.Any, 4296)))
            using (UdpConnection connection = new UnityUdpClientConnection(logger, new IPEndPoint(IPAddress.Loopback, 4296)))
            {
                UdpConnection client = null;
                listener.NewConnection += delegate (NewConnectionEventArgs args)
                {
                    client = (UdpConnection)args.Connection;
                    client.KeepAliveInterval = 100;

                    Thread.Sleep(1050);    //Enough time for ~10 keep alive packets

                    mutex.Set();
                };

                listener.Start();

                connection.Connect();

                mutex.WaitOne();

                Assert.Equal(ConnectionState.Connected, client.State);

                Assert.True(
                    client.Statistics.TotalBytesSent >= 27 &&
                    client.Statistics.TotalBytesSent <= 50,
                    "Sent: " + client.Statistics.TotalBytesSent
                );
            }
        }

        /// <summary>
        ///     Tests disconnection from the client.
        /// </summary>
        [Fact]
        public void ClientDisconnectTest()
        {
            using (UdpConnectionListener listener = new UdpConnectionListener(new IPEndPoint(IPAddress.Any, 4296)))
            using (UdpConnection connection = new UnityUdpClientConnection(logger, new IPEndPoint(IPAddress.Loopback, 4296)))
            {
                TestHelper.RunClientDisconnectTest(listener, connection);
            }
        }

        /// <summary>
        ///     Test that a disconnect is sent when the client is disposed.
        /// </summary>
        public void ClientDisconnectOnDisposeTest()
        {
            using (UdpConnectionListener listener = new UdpConnectionListener(new IPEndPoint(IPAddress.Any, 4296)))
            using (UdpConnection connection = new UnityUdpClientConnection(logger, new IPEndPoint(IPAddress.Loopback, 4296)))
            {
                TestHelper.RunClientDisconnectOnDisposeTest(listener, connection);
            }
        }

        /// <summary>
        ///     Tests disconnection from the server.
        /// </summary>
        [Fact]
        public void ServerDisconnectTest()
        {
            using (UdpConnectionListener listener = new UdpConnectionListener(new IPEndPoint(IPAddress.Any, 4296)))
            using (UdpConnection connection = new UnityUdpClientConnection(logger, new IPEndPoint(IPAddress.Loopback, 4296)))
            {
                TestHelper.RunServerDisconnectTest(listener, connection);
            }
        }

        /// <summary>
        ///     Tests disconnection from the server.
        /// </summary>
        [Fact]
        public void ServerExtraDataDisconnectTest()
        {
            using (UdpConnectionListener listener = new UdpConnectionListener(new IPEndPoint(IPAddress.Any, 4296)))
            using (UdpConnection connection = new UnityUdpClientConnection(logger, new IPEndPoint(IPAddress.Loopback, 4296)))
            {
                string received = null;
                ManualResetEvent mutex = new ManualResetEvent(false);

                connection.Disconnected += delegate (object sender, DisconnectedEventArgs args)
                {
                    // We don't own the message, we have to read the string now
                    received = args.Message.ReadString();
                    mutex.Set();
                };

                listener.NewConnection += delegate (NewConnectionEventArgs args)
                {
                    // As it turns out, the UdpConnectionListener can have an issue on loopback where the disconnect can happen before the hello confirm
                    // Tossing it on a different thread makes this test more reliable. Perhaps something to think about elsewhere though.
                    Task.Run(async () =>
                    {
                        await Task.Delay(1);
                        MessageWriter writer = MessageWriter.Get(SendOption.None);
                        writer.Write("Goodbye");
                        args.Connection.Disconnect("Testing", writer);
                    });
                };

                listener.Start();

                connection.Connect();

                mutex.WaitOne();

                Assert.NotNull(received);
                Assert.Equal("Goodbye", received);
            }
        }
    }
}
*/