QuickTunnel
===========

QuickTunnel - Redirect TCP streams and UDP datagrams to/from the running machine.



## Usage

`QuickTunnel [[protocol:]address:]port[-endport]:remote[:remoteport][(param1[,param2[,...]])] [...]`



## Protocols

`tcp` - TCP/IP protocol (default if omitted).

`udp` - UDP/IP protocol.



## Parameters

`debug` - Print transiting packets and connections to STDERR.



## Examples

### Redirecting

`QuickTunnel 80:example.com` - Redirect incoming requests on TCP port 80 to example.com on same port.

`QuickTunnel *:80:example.com` - Same as previous example.

`QuickTunnel tcp:*:80:example.com` - Same as previous example.

`QuickTunnel 8080:example.com:80` - Redirect requests on TCP port 8080 to example.com on TCP port 80.

`QuickTunnel tcp:*:10-20:example.com` - Redirect requests on TCP ports from 10 to 20 to example.com on same ports.

`QuickTunnel tcp:*:10-20:example.com:30`- Redirect requests on TCP ports from 10 to 20 to example.com on port from 30 to 40.

`QuickTunnel 10-20:example.com:30` - Same as previous example.

`QuickTunnel 99:192.168.1.1 55:192.168.5.5` - Redirect requests on TCP port 99 to 192.168.1.1 and incoming requests on TCP port 55 to 192.168.5.5

`QuickTunnel udp:*:1234:server` - Redirect requests on UDP port 1234 to server on same port from a random UDP port.

`QuickTunnel udp:*:1111:server:2222` - Redirect requests on UDP port 1111 to server on UDP port 2222 from a random UDP port.

### Debugging

`QuickTunnel 80:example.com(debug)` - Redirect requests on TCP port 80 to example.com on same port and print all transiting packets and connections to STDERR.



## License

Copyright © 2018 Fabio Iotti. QuickTunnel is released under the terms of [the MIT License](LICENSE).
