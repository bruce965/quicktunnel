using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Reflection;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;

namespace QuickTunnel
{
	class Program
	{
		class Route
		{
			public string Protocol;
			public string Address;
			public int Port;
			public int EndPort;
			public string Remote;
			public int RemotePort;
			public string[] Parameters;
		}

		static void Main(string[] args)
		{
			var routes = new List<Route>();
			var stuff = new List<IDisposable>();

			try
			{
				#region Help

				if (args.Length == 0 || args[0].IndexOf(':') == -1)
				{
					var executableName = Path.GetFileNameWithoutExtension(Assembly.GetEntryAssembly().CodeBase);

					Console.Error.WriteLine("QuickTunnel v0.1 - Redirect TCP streams and UDP datagrams to/from the running machine.");
					Console.Error.WriteLine("Copyright (c) Fabio Iotti 2018. Licensed under the terms of the MIT License.");
					Console.Error.WriteLine();
					Console.Error.WriteLine("Usage:");
					Console.Error.WriteLine("  {0} [[protocol:]address:]port[-endport]:remote[:remoteport][(param1[,param2[,...]])] [...]", executableName);
					Console.Error.WriteLine();
					Console.Error.WriteLine("Protocols:");
					Console.Error.WriteLine("  tcp   TCP/IP protocol (default if omitted)");
					Console.Error.WriteLine("  udp   UDP/IP protocol");
					Console.Error.WriteLine();
					Console.Error.WriteLine("Parameters:");
					// Console.Error.WriteLine("  immediate              immediately estabilish a connection, without sending any data until a connection arrives, TCP only");
					// Console.Error.WriteLine("  persistent             do not close connection after incoming connection is closed, TCP only");
					// Console.Error.WriteLine("  close                  shutdown after all connections are closed, TCP only");
					// Console.Error.WriteLine("  fail                   shutdown if unable to connect to remote, the alternative is rejecting incoming connections, TCP only");
					// Console.Error.WriteLine("  limit=number           limit maximum number of concurrent connections, TCP only");
					// Console.Error.WriteLine("  queue                  accept and enqueue connections over the limit, TCP only");
					// Console.Error.WriteLine("  delay=min[-max]        delay transmission of packets between min and optionally max milliseconds");
					// Console.Error.WriteLine("  drop=percent[-out]     drop a percentage of packets randomly, optionally an alternate percentage for outgoing packets, value between 0 and 100, UDP only");
					// Console.Error.WriteLine("  speed=bytesec[-out]    limit maximum throughput to a specific number of bytes/second, optionally an alternate speed for outgoing packets");
					// Console.Error.WriteLine("  speedx=bytesec[-out]   limit individual connections throughput to a specific number of bytes/second, optionally an alternate speed for outgoing packets");
					// Console.Error.WriteLine("  buffer=bytes[-out]     group and buffer TCP packets incoming in rapid sequence, optionally an alternate size for upstream, TCP only");
					// Console.Error.WriteLine("  bufftime=ms[-out]      automatically flush the buffer after the specified number of milliseconds, optionally an alternate time for upstream, TCP only");
					Console.Error.WriteLine("  debug                  print transiting packets and connections to STDERR");
					Console.Error.WriteLine();
					Console.Error.WriteLine("Examples:");
					Console.Error.WriteLine("  -- Redirecting --");
					Console.Error.WriteLine("  {0} 80:example.com                  redirect incoming requests on TCP port 80 to example.com on same port", executableName);
					Console.Error.WriteLine("  {0} *:80:example.com                same as above", executableName);
					Console.Error.WriteLine("  {0} tcp:*:80:example.com            same as above", executableName);
					Console.Error.WriteLine("  {0} 8080:example.com:80             redirect requests on TCP port 8080 to example.com on TCP port 80", executableName);
					// Console.Error.WriteLine("  {0} 127.0.0.1:80:example.com        redirect requests from 127.0.0.1 on TCP port 80 to example.com on same port", executableName);
					Console.Error.WriteLine("  {0} tcp:*:10-20:example.com         redirect requests on TCP ports from 10 to 20 to example.com on same ports", executableName);
					Console.Error.WriteLine("  {0} tcp:*:10-20:example.com:30      redirect requests on TCP ports from 10 to 20 to example.com on port from 30 to 40", executableName);
					Console.Error.WriteLine("  {0} 10-20:example.com:30            same as above", executableName);
					Console.Error.WriteLine("  {0} 99:192.168.1.1 55:192.168.5.5   redirect requests on TCP port 99 to 192.168.1.1 and incoming requests on TCP port 55 to 192.168.5.5", executableName);
					Console.Error.WriteLine("  {0} udp:*:1234:server               redirect requests on UDP port 1234 to server on same port from a random UDP port", executableName);
					Console.Error.WriteLine("  {0} udp:*:1111:server:2222          redirect requests on UDP port 1111 to server on UDP port 2222 from a random UDP port", executableName);
					//Console.Error.WriteLine("  {0} udp:server1:1234:server2        redirect requests on UDP port 1234 between server1 and server2 from the same port", executableName);
					//Console.Error.WriteLine("  {0} udp:server1:1111:server2:2222   redirect requests on UDP port 1111 of server1 to/from UDP port 2222 of server2", executableName);
					Console.Error.WriteLine();
					// Console.Error.WriteLine("  -- Balancing --");
					// Console.Error.WriteLine("  {0} 80:server1 80:server2           balance requests on TCP port 80 between server1 and server2 on same port", executableName);
					// Console.Error.WriteLine("  {0} 80:server:8080 80:server:8081   balance requests on TCP port 80 between ports 8080 and 8081 of server", executableName);
					// Console.Error.WriteLine();
					// Console.Error.WriteLine("  -- Parameters --");
					// Console.Error.WriteLine("  {0} 1234:example.com(immediate)     connect to example.com on TCP port 1234 and redirect incoming requests on same port, open more connections if necessary", executableName);
					// Console.Error.WriteLine("  {0} 1000:server(immediate,fail)     connect to server on TCP port 1234 and redirect incoming requests on same port, shutdown if server is down", executableName);
					// Console.Error.WriteLine("  {0} 1234:192.168.1.1(persistent)    redirect requests on TCP port 1234 to 192.168.1.1 and keep connections alive, open more connections if necessary", executableName);
					// Console.Error.WriteLine("  {0} 1234:192.168.1.1(close)         redirect requests on TCP port 1234 to 192.168.1.1 and shutdown after all connections are closed", executableName);
					// Console.Error.WriteLine("  {0} 80:example.com(limit=10)        redirect up to 10 parallel requests on TCP port 80 to example.com on same port, connections over the limit are dropped", executableName);
					// Console.Error.WriteLine("  {0} 80:example.com(queue,limit=3)   redirect up to 3 parallel requests on TCP port 80 to example.com on same port, connections over the limit are enqueued", executableName);
					// Console.Error.WriteLine();
					Console.Error.WriteLine("  -- Debugging --");
					// Console.Error.WriteLine("  {0} 80:example.com(delay=100-200)   simulate a latency between 100 and 200 milliseconds", executableName);
					// Console.Error.WriteLine("  {0} udp:*:3000:server(drop=30)      randomly drop 30% of the packets", executableName);
					// Console.Error.WriteLine("  {0} udp:*:3000:server(drop=10-20)   randomly drop 10% of the incoming and 20% of the outgoing packets", executableName);
					// Console.Error.WriteLine("  {0} 80:example.com(speed=65536)     limit upload+download speed to 64KiB/s", executableName);
					// Console.Error.WriteLine("  {0} 80:example.com(speed=256-512)   limit upload speed to 256B/s and download speed to 512B/s", executableName);
					Console.Error.WriteLine("  {0} 80:example.com(debug)           redirect requests on TCP port 80 to example.com on same port and print all transiting packets and connections to STDERR", executableName);
					Console.Error.WriteLine();

					Environment.ExitCode = 5;
					return;
				}

				#endregion
			
				#region Arguments Parsing

				foreach (var arg in args)
				{
					if (!tryParseArg(arg, out var route))
					{
						Console.Error.WriteLine("Invalid argument: {0}", arg);
						Environment.ExitCode = 5;
						return;
					}

					routes.Add(route);
				}

				#endregion

				// build and start routes
				foreach (var route in routes)
				{
					var invalidParameters = route.Parameters.Except(new[] { "debug" }, StringComparer.OrdinalIgnoreCase);
					if (invalidParameters.Any()) {
						Console.Error.WriteLine("Invalid parameter: {0}", invalidParameters.First());
						Environment.ExitCode = 5;
						return;
					}

					var isDebug = route.Parameters.Contains("debug", StringComparer.OrdinalIgnoreCase);

					switch (route.Protocol)
					{
						case "tcp":
							if (route.Address == "*")
							{
								foreach (var port in Enumerable.Range(route.Port, route.EndPort - route.Port + 1))
								{
									stuff.Add(new TcpSocketTunnel(route.Remote, route.RemotePort - route.Port + port, port, isDebug));
								}
							}
							else
							{
								throw new NotImplementedException("TODO: end-to-end TCP tunnel support not implemented yet");
							}
							break;

						case "udp":
							if (route.Address == "*")
							{
								foreach (var port in Enumerable.Range(route.Port, route.EndPort - route.Port + 1))
								{
									stuff.Add(new UdpAnyToEndSocketTunnel(route.Remote, route.RemotePort - route.Port + port, port, isDebug));
								}
							}
							else
							{
								throw new NotImplementedException("TODO: end-to-end UDP tunnel support not implemented yet");
							}
							break;

						default:
							Console.Error.WriteLine("Unsupported protocol: {0}", route.Protocol);
							Environment.ExitCode = 5;
							return;
					}
				}

				// wait for CTRL + C
				var semaphore = new SemaphoreSlim(0);
				Console.CancelKeyPress += (sender, e) => semaphore.Release();
				semaphore.Wait();

				Environment.ExitCode = 0;
				return;
			}
			finally
			{
				foreach (var el in stuff)
				{
					try
					{
						el.Dispose();
					}
					catch (Exception)
					{
						// closing soon, who cares at this point?
					}
				}
			}
		}

		#region tryParseArg

		// [[protocol:]address:]port[-endport]:remote[:remoteport][(params)]
		static readonly Regex argRegex = new Regex(@"^(?:(?:(?<protocol>[^:()]+):)?(?<address>[^:()]+):)?(?<port>[0-9]+)(?:-(?<endport>[0-9]+))?:(?<remote>[^:()]+)(?::(?<remoteport>[0-9]+))?(?:\((?<params>[^:()]+)\))?$");

		static bool tryParseArg(string arg, out Route route)
		{
			var match = argRegex.Match(arg);
			if (!match.Success) {
				route = null;
				return false;
			}

			try
			{
				var protocol = match.Groups["protocol"].Value;
				var address = match.Groups["address"].Value;
				var port = Int32.Parse(match.Groups["port"].Value);
				var parameters = match.Groups["params"].Value;
				if (!Int32.TryParse(match.Groups["endport"].Value, out var endPort))
					endPort = port;
				if (!Int32.TryParse(match.Groups["remoteport"].Value, out var remotePort))
					remotePort = port;

				route = new Route
				{
					Protocol = String.IsNullOrEmpty(protocol) ? "tcp" : protocol,
					Address = String.IsNullOrEmpty(address) ? "*" : address,
					Port = port,
					EndPort = endPort,
					Remote = match.Groups["remote"].Value,
					RemotePort = remotePort,
					Parameters = (parameters == "") ? new string[0] : parameters.Split(',')
				};
				return true;
			}
			catch (Exception)
			{
				route = null;
				return false;
			}
		}

		#endregion
	}
}
