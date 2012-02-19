using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Runtime.Serialization;
using System.Net.NetworkInformation;
using System.Runtime.InteropServices;

namespace Kngine
{

    public static class IPUtilities
    {

        public static string MachineName
        {
            get
            {
                return Environment.MachineName;
            }
        }

        public static string MachineLastIPAddress
        {
            get
            {
                var ipAddresses = MachineIPAddresses;
                if (ipAddresses.Length == 1)
                    return ipAddresses.First().ToString();
                else
                {
                    try
                    {
                        var selected = ipAddresses.OrderByDescending(o => int.Parse(o.ToString().Substring(o.ToString().LastIndexOf(".") + 1))).FirstOrDefault();
                        return selected.ToString();
                    }
                    catch
                    {
                        return ipAddresses.First().ToString();
                    }

                }
            }
        }

        public static string[] MachineIPAddresses
        {
            get
            {
                List<string> IP = new List<string>();
                foreach (var item in NetworkInterface.GetAllNetworkInterfaces())
                {
                    IPInterfaceProperties ipProps = item.GetIPProperties();

                    if (ipProps.GatewayAddresses.Count == 0 || (ipProps.DhcpServerAddresses.Count > 0 && ipProps.DhcpServerAddresses.FirstOrDefault().Address == 0))
                        continue;

                    for (int i = 0; i < ipProps.UnicastAddresses.Count; i++)
                        if (!ipProps.UnicastAddresses[i].Address.IsIPv6LinkLocal)
                            IP.Add(ipProps.UnicastAddresses[i].Address.ToString());
                }
                return IP.ToArray();
            }
            
        }

    }
}
