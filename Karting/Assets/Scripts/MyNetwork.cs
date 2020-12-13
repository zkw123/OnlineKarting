using System.Collections;
using System.Collections.Generic;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using UnityEngine;
using UnityEngine.Networking;
public class MyNetwork : NetworkDiscovery
{
    NetworkManager n;
    // Start is called before the first frame update
    private void Start()
    {
        n = GetComponent<NetworkManager>();
        n.networkAddress = GetIP();
    }
    public override void OnReceivedBroadcast(string fromAddress, string data)
    {
        Debug.Log("Received broadcast from: " + fromAddress + " with the data: " + data);
        n.networkAddress = fromAddress;
    }
    public string GetIP()
    {
        string output = "";
        foreach (NetworkInterface item in NetworkInterface.GetAllNetworkInterfaces())
        {
#if UNITY_EDITOR_WIN || UNITY_STANDALONE_WIN
            NetworkInterfaceType _type1 = NetworkInterfaceType.Wireless80211;
            NetworkInterfaceType _type2 = NetworkInterfaceType.Ethernet;

            if ((item.NetworkInterfaceType == _type1 || item.NetworkInterfaceType == _type2) && item.OperationalStatus == OperationalStatus.Up)
#endif 
            {
                foreach (UnicastIPAddressInformation ip in item.GetIPProperties().UnicastAddresses)
                {

                    if (ip.Address.AddressFamily == AddressFamily.InterNetwork)
                    {
                        output = ip.Address.ToString();
                        //Debug.Log("IP:" + output);
                    }
                    else if (ip.Address.AddressFamily == AddressFamily.InterNetworkV6)
                    {
                        output = ip.Address.ToString();
                    }

                }
            }
        }
        Debug.Log(output);
        return output;
    }
}
