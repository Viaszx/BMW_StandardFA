using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Text.RegularExpressions;
using System.Windows.Forms;
using System.Net.NetworkInformation;
using System.Threading.Tasks;

namespace StandardFA
{
    public class ZgwInfo
    {
        public string ZgwVIN { get; set; }
        public string ZgwIP { get; set; }

        public ZgwInfo(string vin, string ip)
        {
            ZgwVIN = vin;
            ZgwIP = ip;
        }
    }

    public class FindZGW
    {
        public static string ZgwVIN;
        public static string ZgwIP;

        private const int UDP_DIAG_PORT = 6811;
        static byte[] helloZGW = new byte[] { 0, 0, 0, 0, 0, 0x11 };

        static IPAddress GetBroadcastIP(IPAddress host, IPAddress mask)
        {
            uint hostAddress = BitConverter.ToUInt32(host.GetAddressBytes(), 0);
            uint maskAddress = BitConverter.ToUInt32(mask.GetAddressBytes(), 0);
            uint broadcastAddress = hostAddress | ~maskAddress;

            byte[] broadcastBytes = BitConverter.GetBytes(broadcastAddress);

            return new IPAddress(broadcastBytes);
        }

        static async Task PingZGWAsync(NetworkInterface networkInterface)
        {
            var ipProps = networkInterface.GetIPProperties();
            foreach (var ipAddr in ipProps.UnicastAddresses)
            {
                if (!IPAddress.IsLoopback(ipAddr.Address) && ipAddr.Address.AddressFamily == AddressFamily.InterNetwork)
                {
                    var broadcast = GetBroadcastIP(ipAddr.Address, ipAddr.IPv4Mask);
                    var ep = new IPEndPoint(broadcast, UDP_DIAG_PORT);

                    using (var client = new UdpClient())
                    {
                        client.Client.ReceiveTimeout = 100;
                        try
                        {
                            if (broadcast.ToString() == "255.255.255.255")
                            {
                                ep = new IPEndPoint(IPAddress.Broadcast, UDP_DIAG_PORT);
                            }

                            await client.SendAsync(helloZGW, helloZGW.Length, ep);

                            UdpReceiveResult result = await client.ReceiveAsync();
                            var ZGW_reply = Encoding.ASCII.GetString(result.Buffer);
                            ProcessZGWReply(ZGW_reply, result.RemoteEndPoint);
                            break;
                        }
                        catch (SocketException ex)
                        {
                            // Error handling
                        }
                    }
                }
            }
        }

        static void ProcessZGWReply(string reply, EndPoint remoteEndpoint)
        {
            var pattern = @"DIAGADR(.*)BMWMAC(.*)BMWVIN(.*)";
            var match = Regex.Match(reply, pattern);
            if (match.Success)
            {
                ZgwVIN = match.Groups[3].Value.Trim();
                ZgwIP = ((IPEndPoint)remoteEndpoint).Address.ToString();
            }
        }

        public static async Task<ZgwInfo> SearchZGWAsync()
        {
            var interfaces = NetworkInterface.GetAllNetworkInterfaces();
            List<Task> tasks = new List<Task>();

            foreach (var networkInterface in interfaces)
            {
                if (networkInterface.OperationalStatus == OperationalStatus.Up)
                {
                    tasks.Add(PingZGWAsync(networkInterface));
                }
            }

            try
            {
                var timeout = Task.Delay(1000); 
                var completedTask = await Task.WhenAny(Task.WhenAll(tasks), timeout);

                if (completedTask == timeout)
                {
                    return new ZgwInfo(ZgwVIN, ZgwIP);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error during ZGW search: " + ex.Message);
            }

            return null;
        }
    }
}
