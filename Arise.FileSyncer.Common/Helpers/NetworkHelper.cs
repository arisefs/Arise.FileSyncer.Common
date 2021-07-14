using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;

namespace Arise.FileSyncer.Common.Helpers
{
    public static class NetworkHelper
    {
        /// <summary>
        /// Gets first active LAN IP address.
        /// </summary>
        /// <param name="addressFamily">Address family to search for</param>
        /// <returns>LAN IP address</returns>
        public static IPAddress GetLocalIPAddress(AddressFamily addressFamily)
        {
            return GetIPWithHostDNS(addressFamily);
        }

        private static IPAddress GetIPWithHostDNS(AddressFamily addressFamily)
        {
            IPAddress selectedAddress = IPAddress.Any;
            IPHostEntry hostEntry = Dns.GetHostEntry(Dns.GetHostName());

            foreach (var ip in hostEntry.AddressList)
            {
                if (ip.AddressFamily == addressFamily && ip.IsInternal())
                {
                    selectedAddress = ip;
                    break;
                }
            }

            return selectedAddress;
        }

        private static IPAddress GetIPWithNetworkInterfaces(AddressFamily addressFamily)
        {
            IPAddress selectedAddress = IPAddress.Any;

            foreach (var networkInterface in NetworkInterface.GetAllNetworkInterfaces())
            {
                if (networkInterface.OperationalStatus == OperationalStatus.Up)
                {
                    bool selected = false;
                    var ipProps = networkInterface.GetIPProperties();

                    foreach (var uniAddress in ipProps.UnicastAddresses)
                    {
                        var ip = uniAddress.Address;

                        if (ip.AddressFamily == addressFamily && ip.IsInternal())
                        {
                            selectedAddress = ip;
                            selected = true;
                            break;
                        }
                    }

                    if (selected && ipProps.GatewayAddresses.Count > 0)
                    {
                        break;
                    }
                }
            }

            return selectedAddress;
        }

        /// <summary>
        /// An extension method to determine if an IP address is internal, as specified in RFC1918
        /// </summary>
        /// <param name="toTest">The IP address that will be tested</param>
        /// <returns>Returns true if the IP is internal, false if it is external</returns>
        public static bool IsInternal(this IPAddress toTest)
        {
            byte[] bytes = toTest.GetAddressBytes();
            return bytes[0] switch
            {
                10 => true,
                172 => bytes[1] < 32 && bytes[1] >= 16,
                192 => bytes[1] == 168,
                _ => false,
            };
        }
    }
}
