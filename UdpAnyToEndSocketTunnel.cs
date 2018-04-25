using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Threading.Tasks;

namespace QuickTunnel
{
	public class UdpAnyToEndSocketTunnel : IDisposable
	{
		readonly object lockme = new object();
		readonly UdpClient client;
		readonly Dictionary<SocketAddress, (UdpClient, int)> connected = new Dictionary<SocketAddress, (UdpClient, int)>();

		public UdpAnyToEndSocketTunnel(string remoteHostname, int remotePort, int localPort, bool debug)
		{
			// `client` will be used to receive/send messages from/to downstream (the client connecting to remote through this tunnel)
			client = new UdpClient(new IPEndPoint(IPAddress.Any, localPort))
			{
				//ExclusiveAddressUse = false,
				DontFragment = true
			};

			Task.Run(async () =>
			{
				while (true)
				{
					// here we receive messages from the remote clients connecting to the remote through this tunnel
					var downstreamDatagram = await client.ReceiveAsync();

					if (debug)
						Debug.PrintReceived("UDP", downstreamDatagram.RemoteEndPoint, localPort, downstreamDatagram.Buffer, 0, downstreamDatagram.Buffer.Length);

					// when we receive a message we check if the sender is known; if it is not,
					// then we register a client `thisClient` for the specific sender
					UdpClient thisClient;
					int thisPort;

					lock (lockme)
					{
						var remoteAddress = downstreamDatagram.RemoteEndPoint.Serialize();
						if (!connected.TryGetValue(remoteAddress, out var thisConnection))
						{
							// `thisClient` will be used to receive/send messages from/to upstream (the remote we tunnel to)
							thisClient = new UdpClient(new IPEndPoint(IPAddress.Any, 0));  // 0 = random free ephemeral port
							thisPort = ((IPEndPoint)thisClient.Client.LocalEndPoint).Port;

							connected.Add(remoteAddress, (thisClient, thisPort));

							Task.Run(async () =>
							{
								try
								{
									// TODO: if `connected.Count` gets too high, break the loop for the older inactive clients

									while (true)
									{
										// here we receive messages from the remote we tunnel to
										var upstreamDatagram = await thisClient.ReceiveAsync();

										if (debug) {
											Debug.PrintReceived("UDP", upstreamDatagram.RemoteEndPoint, thisPort, upstreamDatagram.Buffer, 0, upstreamDatagram.Buffer.Length);
											Debug.PrintSending("UDP", downstreamDatagram.RemoteEndPoint, localPort, upstreamDatagram.Buffer, 0, upstreamDatagram.Buffer.Length);
										}

										#pragma warning disable 4014
										// fire and forget, it's no big deal if it fails
										client.SendAsync(upstreamDatagram.Buffer, upstreamDatagram.Buffer.Length, downstreamDatagram.RemoteEndPoint);  // forward message downstream
										#pragma warning restore 4014
									}
								}
								finally
								{
									try
									{
										((IDisposable)thisClient).Dispose();
									}
									finally
									{
										lock (lockme)
										{
											connected.Remove(remoteAddress);
										}
									}
								}
							});
						}
						else
						{
							thisClient = thisConnection.Item1;
							thisPort = thisConnection.Item2;
						}
					}

					if (debug)
						Debug.PrintSending("UDP", remoteHostname, remotePort, thisPort, downstreamDatagram.Buffer, 0, downstreamDatagram.Buffer.Length);

					#pragma warning disable 4014
					// fire and forget, it's no big deal if it fails
					thisClient.SendAsync(downstreamDatagram.Buffer, downstreamDatagram.Buffer.Length, remoteHostname, remotePort);  // forward message upstream
					#pragma warning restore 4014
				}
			});
		}

		public void Dispose()
		{
			// stop the main socket
			try
			{
				((IDisposable)client).Dispose();
			}
			catch (Exception e)
			{
				Console.Error.WriteLine(e);
			}

			List<UdpClient> clients;
			lock (lockme)
			{
				clients = connected.Values.Select(x => x.Item1).ToList();
				connected.Clear();
			}

			foreach (var client in clients)
			{
				try
				{
					((IDisposable)client).Dispose();
				}
				catch (Exception e)
				{
					Console.Error.WriteLine(e);
				}
			}
		}
	}
}
