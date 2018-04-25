using System;
using System.Net;

namespace QuickTunnel
{
	public static class Debug
	{
		static readonly object lockme = new object();

		public static void PrintIncomingConnected(string protocol, EndPoint remoteEndpoint, int localPort) => printIncomingConnection(true, protocol, remoteEndpoint.ToString(), localPort);
		public static void PrintIncomingConnected(string protocol, string remoteHostname, int remotePort, int localPort) => printIncomingConnection(true, protocol, remoteHostname + ':' + remotePort, localPort);
		public static void PrintIncomingConnected(string protocol, string remote, int localPort) => printIncomingConnection(true, protocol, remote, localPort);

		public static void PrintIncomingDisconnected(string protocol, EndPoint remoteEndpoint, int localPort) => printIncomingConnection(false, protocol, remoteEndpoint.ToString(), localPort);
		public static void PrintIncomingDisconnected(string protocol, string remoteHostname, int remotePort, int localPort) => printIncomingConnection(false, protocol, remoteHostname + ':' + remotePort, localPort);
		public static void PrintIncomingDisconnected(string protocol, string remote, int localPort) => printIncomingConnection(false, protocol, remote, localPort);

		public static void PrintOutgoingConnected(string protocol, EndPoint remoteEndpoint, int localPort) => printOutgoingConnection(true, protocol, remoteEndpoint.ToString(), localPort);
		public static void PrintOutgoingConnected(string protocol, string remoteHostname, int remotePort, int localPort) => printOutgoingConnection(true, protocol, remoteHostname + ':' + remotePort, localPort);
		public static void PrintOutgoingConnected(string protocol, string remote, int localPort) => printOutgoingConnection(true, protocol, remote, localPort);

		public static void PrintOutgoingDisconnected(string protocol, EndPoint remoteEndpoint, int localPort) => printOutgoingConnection(false, protocol, remoteEndpoint.ToString(), localPort);
		public static void PrintOutgoingDisconnected(string protocol, string remoteHostname, int remotePort, int localPort) => printOutgoingConnection(false, protocol, remoteHostname + ':' + remotePort, localPort);
		public static void PrintOutgoingDisconnected(string protocol, string remote, int localPort) => printOutgoingConnection(false, protocol, remote, localPort);

		public static void PrintSending(string protocol, EndPoint remoteEndpoint, int localPort, byte[] data, int offset, int count) => printPacket(true, protocol, remoteEndpoint.ToString(), localPort, data, offset, count);
		public static void PrintSending(string protocol, string remoteHostname, int remotePort, int localPort, byte[] data, int offset, int count) => printPacket(true, protocol, remoteHostname + ':' + remotePort, localPort, data, offset, count);
		public static void PrintSending(string protocol, string remote, int localPort, byte[] data, int offset, int count) => printPacket(true, protocol, remote, localPort, data, offset, count);
		
		public static void PrintReceived(string protocol, EndPoint remoteEndpoint, int localPort, byte[] data, int offset, int count) => printPacket(false, protocol, remoteEndpoint.ToString(), localPort, data, offset, count);
		public static void PrintReceived(string protocol, string remoteHostname, int remotePort, int localPort, byte[] data, int offset, int count) => printPacket(false, protocol, remoteHostname + ':' + remotePort, localPort, data, offset, count);
		public static void PrintReceived(string protocol, string remote, int localPort, byte[] data, int offset, int count) => printPacket(false, protocol, remote, localPort, data, offset, count);
		
		static void printIncomingConnection(bool isConnected, string protocol, string remote, int localPort)
		{
			try
			{
				lock (lockme)
				{
					// TODO: do not lock, preserve print order between threads

					// + TCP 2018-07-18 07:11:02.3106 192.168.1.1:37821 connected to local port 80
					//

					// - TCP 2018-07-18 07:11:08.9823 192.168.1.1:37821 disconnected from local port 80
					//

					Console.Error.WriteLine(
						"{0} {1} {2} {3} {4} ({5} local port {6})",
						isConnected ? '+' : '-',
						protocol,
						DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.ffff"),
						remote,
						isConnected ? "connected" : "disconnected",
						isConnected ? "to" : "from",
						localPort
					);

					Console.Error.WriteLine();
				}
			}
			catch (Exception)
			{
				// not a big deal
			}
		}

		static void printOutgoingConnection(bool isConnected, string protocol, string remote, int localPort)
		{
			try
			{
				lock (lockme)
				{
					// TODO: do not lock, preserve print order between threads

					// + TCP 2018-07-18 07:11:02.3106 connected to 192.168.1.1:80 from local port 37821
					//

					// - TCP 2018-07-18 07:11:08.9823 disconnected from 192.168.1.1:80 from local port 37821
					//

					Console.Error.WriteLine(
						"{0} {1} {2} {3} {4} from local port {5}",
						isConnected ? '+' : '-',
						protocol,
						DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.ffff"),
						isConnected ? "connected to" : "disconnected from",
						remote,
						localPort
					);

					Console.Error.WriteLine();
				}
			}
			catch (Exception)
			{
				// not a big deal
			}
		}

		static void printPacket(bool isSending, string protocol, string remote, int localPort, byte[] data, int offset, int count)
		{
			try
			{
				lock (lockme)
				{
					// TODO: do not lock, preserve print order between threads

					// > UDP 2018-01-13 13:35:25.3324 sending to 192.168.1.1:35237 from local port 3000
					// 61 62 00 48 65 6c 6c 6f 20 57 6f 72 6c 64 13 10 | ab.Hello World..
					// 00 01 02 03 74 65 73 74                         | ....test
					//

					// < TCP 2018-03-25 13:37:01.8394 received from 192.168.2.2:2000 on local port 34456
					// 74 65 73 74 32                                  | test2
					//

					Console.Error.WriteLine(
						"{0} {1} {2} {3} {4} {5} local port {6}",
						isSending ? '>' : '<',
						protocol,
						DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.ffff"),
						isSending ? "sending to" : "received from",
						remote,
						isSending ? "from" : "on",
						localPort
					);

					for (var i = offset; i < offset + count; i += 16)
					{
						// complete line: "61 62 00 48 65 6c 6c 6f 20 57 6f 72 6c 64 13 10 "
						//  partial line: "00 01 02 03 74 65 73 74                         "
						for (var c = 0; c < 16; c++)
						{
							if (i + c < offset + count)
								Console.Error.Write("{0:x2} ", data[i + c]);
							else
								Console.Error.Write("   ");
						}

						Console.Error.Write("| ");

						// complete line: "ab.Hello World.."
						//  partial line: "....test"
						for (var c = 0; c < 16 && i + c < offset + count; c++)
						{
							var byteVal = data[i + c];
							if (byteVal >= ' ' && byteVal <= '~')
								Console.Error.Write((char)byteVal);
							else
								Console.Error.Write('.');
						}

						Console.Error.WriteLine();
					}

					Console.Error.WriteLine();
				}
			}
			catch (Exception)
			{
				// not a big deal
			}
		}
	}
}
