using System;
using System.Net;
using System.Net.Sockets;
using System.Threading.Tasks;

namespace QuickTunnel
{
	public class TcpSocketTunnel : IDisposable
	{
		readonly TcpListener listener;

		public TcpSocketTunnel(string remoteHostname, int remotePort, int localPort, bool debug)
		{
			listener = new TcpListener(new IPEndPoint(IPAddress.Any, localPort));
			listener.Start();

			Task.Run(async () =>
			{
				while (true)
				{
					// here we receive messages from the client connecting to the remote through this tunnel
					var downstreamClient = await listener.AcceptTcpClientAsync();
					downstreamClient.NoDelay = true;
					downstreamClient.ReceiveBufferSize = 256 * 1024;  // 256KB
					downstreamClient.SendBufferSize = 256 * 1024;  // 256KB

					var downstreamEndpoint = downstreamClient.Client.RemoteEndPoint;

					if (debug)
						Debug.PrintIncomingConnected("TCP", downstreamEndpoint, localPort);

					// here we receive messages from the remote trying to talk to the client through this tunnel
					var upstreamClient = new TcpClient();
					upstreamClient.NoDelay = true;
					upstreamClient.ReceiveBufferSize = 256 * 1024;  // 256KB
					upstreamClient.SendBufferSize = 256 * 1024;  // 256KB

					await upstreamClient.ConnectAsync(remoteHostname, remotePort);

					var upstreamPort = ((IPEndPoint)upstreamClient.Client.LocalEndPoint).Port;

					if (debug)
						Debug.PrintOutgoingConnected("TCP", remoteHostname, remotePort, upstreamPort);

					#pragma warning disable 4014
					// start a new task to listen for messages from this client, it's no big deal if it fails
					Task.Run(async () =>
					{
						try
						{
							var buffer = new byte[256 * 1024];

							while(upstreamClient.Client.Connected && downstreamClient.Client.Connected)
							{
								// TODO: make a sliding window buffer

								var count = await Task.Factory.FromAsync(
									downstreamClient.Client.BeginReceive(buffer, 0, buffer.Length, SocketFlags.None, null, null),
									downstreamClient.Client.EndReceive
								);
								if (count == 0)
									break;

								if (debug) {
									Debug.PrintReceived("TCP", downstreamEndpoint, localPort, buffer, 0, count);
									Debug.PrintSending("TCP", remoteHostname, remotePort, upstreamPort, buffer, 0, count);
								}

								await upstreamClient.Client.SendAsync(new ArraySegment<byte>(buffer, 0, count), SocketFlags.None);  // forward message upstream
							}
						}
						finally
						{
							try
							{
								try
								{
									downstreamClient.Close();
								}
								finally
								{
									upstreamClient.Close();
								}
							}
							finally
							{
								if (debug)
									Debug.PrintIncomingDisconnected("TCP", downstreamEndpoint, localPort);
							}
						}
					});

					// start a new task to listen for messages from the remote, it's no big deal if it fails
					Task.Run(async () =>
					{
						try
						{
							var buffer = new byte[256 * 1024];

							while(upstreamClient.Client.Connected && downstreamClient.Client.Connected)
							{
								// TODO: make a sliding window buffer

								var count = await Task.Factory.FromAsync(
									upstreamClient.Client.BeginReceive(buffer, 0, buffer.Length, SocketFlags.None, null, null),
									upstreamClient.Client.EndReceive
								);
								if (count == 0)
									break;

								if (debug) {
									Debug.PrintReceived("TCP", remoteHostname, remotePort, upstreamPort, buffer, 0, count);
									Debug.PrintSending("TCP", downstreamEndpoint, localPort, buffer, 0, count);
								}

								await downstreamClient.Client.SendAsync(new ArraySegment<byte>(buffer, 0, count), SocketFlags.None);  // forward message downstream
							}
						}
						finally
						{
							try
							{
								try
								{
									upstreamClient.Close();
								}
								finally
								{
									downstreamClient.Close();
								}
							}
							finally
							{
								if (debug)
									Debug.PrintOutgoingDisconnected("TCP", remoteHostname, remotePort, upstreamPort);
							}
						}
					});
					#pragma warning restore 4014
				}
			});
		}

		public void Dispose()
		{
			((IDisposable)listener).Dispose();
		}
	}
}
