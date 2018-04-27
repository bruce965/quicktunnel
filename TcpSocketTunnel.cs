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
				while (listener.Server.IsBound)
				{
					TcpClient downstreamClient = null;
					TcpClient upstreamClient = null;
					EndPoint downstreamEndpoint = null;
					int upstreamPort = 0;

					Action disconnect = () => {
						try
						{
							if (downstreamClient != null)
							{
								try
								{
									downstreamClient.Close();
									downstreamClient = null;
								}
								finally
								{
									if (debug)
										Debug.PrintIncomingDisconnected("TCP", downstreamEndpoint, localPort);
								}
							}
						}
						catch (Exception) { }

						try
						{
							try
							{
								if (upstreamClient != null)
								{
									upstreamClient.Close();
									upstreamClient = null;
								}
							}
							finally
							{
								if (debug)
									Debug.PrintOutgoingDisconnected("TCP", remoteHostname, remotePort, upstreamPort);
							}
						}
						catch (Exception) { }
					};

					try
					{
						// here we receive messages from the client connecting to the remote through this tunnel
						downstreamClient = await listener.AcceptTcpClientAsync();
						downstreamClient.NoDelay = true;
						downstreamClient.ReceiveBufferSize = 256 * 1024;  // 256KB
						downstreamClient.SendBufferSize = 256 * 1024;  // 256KB

						downstreamEndpoint = downstreamClient.Client.RemoteEndPoint;

						if (debug)
							Debug.PrintIncomingConnected("TCP", downstreamEndpoint, localPort);

						// here we receive messages from the remote trying to talk to the client through this tunnel
						upstreamClient = new TcpClient();
						upstreamClient.NoDelay = true;
						upstreamClient.ReceiveBufferSize = 256 * 1024;  // 256KB
						upstreamClient.SendBufferSize = 256 * 1024;  // 256KB

						try
						{
							await upstreamClient.ConnectAsync(remoteHostname, remotePort);

							upstreamPort = ((IPEndPoint)upstreamClient.Client.LocalEndPoint).Port;

							if (debug)
								Debug.PrintOutgoingConnected("TCP", remoteHostname, remotePort, upstreamPort);
						}
						catch (SocketException e)
						{
							// if the remote does not accept our connection, we disconnect the client
							if (e.SocketErrorCode == SocketError.ConnectionRefused)
							{
								disconnect();
								continue;
							}
						}

						#pragma warning disable 4014
						// start a new task to listen for messages from this client, it's no big deal if it fails
						Task.Run(async () =>
						{
							try
							{
								var buffer = new byte[256 * 1024];

								while (upstreamClient.Client.Connected && downstreamClient.Client.Connected)
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
							catch (Exception e)
							{
								Debug.PrintError(e, "from downstream task");
							}
							finally
							{
								disconnect();
							}
						});
						#pragma warning restore 4014

						#pragma warning disable 4014
						// start a new task to listen for messages from the remote, it's no big deal if it fails
						Task.Run(async () =>
						{
							try
							{
								var buffer = new byte[256 * 1024];

								while (upstreamClient.Client.Connected && downstreamClient.Client.Connected)
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
							catch (Exception e)
							{
								Debug.PrintError(e, "from upstream task");
							}
							finally
							{
								disconnect();
							}
						});
						#pragma warning restore 4014
					}
					catch (Exception e)
					{
						Debug.PrintError(e, "from server task");
						disconnect();
					}
				}
			});
		}

		public void Dispose()
		{
			try
			{
				listener.Stop();
			}
			catch (Exception) { }
		}
	}
}
