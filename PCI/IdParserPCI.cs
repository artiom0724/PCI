using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Management;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace PCI
{
    class IdParserPCI
    {
        public string[] GetDataBase()
        {
            var webClient = new WebClient();
            string returnString;
            using (webClient)
            {
                returnString = webClient.DownloadString("http://pci-ids.ucw.cz/v2.2/pci.ids");
            }
            return returnString.Split('\n');
        }

        public List<List<string>> GetDevices()
        {
            var connectionScope = new ManagementScope();
            var serialQuery = new SelectQuery("SELECT * FROM Win32_PnPEntity");
            var searcher = new ManagementObjectSearcher(connectionScope, serialQuery);
            var dev = new Regex("DEV_.{4}");
            var ven = new Regex("VEN_.{4}");
            var buffer = new List<List<string>>();

            var file = GetDataBase();

            foreach (var item in searcher.Get())
            {
                var deviceId = item["DeviceID"].ToString();
                if (deviceId.Contains("PCI"))
                    buffer.Add(SearchInFile(dev.Match(deviceId).Value.Substring(4).ToLower(),
                        ven.Match(deviceId).Value.Substring(4).ToLower(),
                        file));
            }
            return buffer;
        }

        private static List<string> SearchInFile(string dev, string ven, string[] PCIids)
        {
            var result = new List<string>();
            var vendorFound = false;
            var deviceFound = false;
            var vendor = new Regex("^" + ven + "  ");
            var device = new Regex("^\\t" + dev + "  ");
            foreach (var item in PCIids)
            {
                if (item != null)
                {
                    if (vendorFound == false && vendor.Match(item).Success)
                    {
                        result.Add("VendorID: " + ven + " (" + item.Substring(6) + ")");
                        vendorFound = true;
                    }
                    else if (vendorFound == true && device.Match(item).Success)
                    {
                        result.Add("DeviceID: " + dev + " (" + item.Substring(7) + ")");
                        deviceFound = true;
                        break;
                    }
                }
            }
            if (!vendorFound)
            {
                result.Add("Device with VendorID " + ven + " and DeviceID " + dev + "wasn't found in base");
                result.Add("");
            }

            if (!deviceFound)
                result.Add("DeviceID: " + dev + " (name can't be found in base)");

            return result;
        }
    }
}
