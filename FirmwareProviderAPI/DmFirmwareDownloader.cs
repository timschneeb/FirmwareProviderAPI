using System;
using System.IO;
using System.Net;
using System.Timers;
using System.Xml;

namespace FirmwareProviderAPI
{
    public class DmFirmwareDownloader : IDisposable
    {
        static readonly string API_REQUEST = "https://wsu-dms.samsungdm.com/common/support/firmware/downloadUrlList.do?prd_mdl_name={0}FOTA&loc=global";
        private static readonly string BASEDIR = "/opt/firmware-provider/Firmware/";
        
        private readonly Timer _timer;

        public DmFirmwareDownloader()
        {
            _timer = new Timer
            {
                Interval = 600000, // every 10 minutes
                AutoReset = true
            };
            _timer.Elapsed += (sender, args) => CheckAll();
            _timer.Start();
            
            CheckAll();
        }

        public void Pause()
        {
            _timer.Stop();
        }

        public void Resume()
        {
            _timer.Start();
        }

        public static void CheckAll()
        {
            CheckForUpdate("SM-R170");
            CheckForUpdate("SM-R175");
        }
        
        public static bool CheckForUpdate(string device)
        {
            try
            {
                var reader = new XmlTextReader(string.Format(API_REQUEST, device.ToUpper()));
                string? url = null;
                string? name = null;
                while (reader.Read())
                {
                    switch (reader.NodeType)
                    {
                        case XmlNodeType.CDATA:
                            if (reader.Value.Contains("https://"))
                            {
                                url = reader.Value;
                            }

                            break;
                        case XmlNodeType.Text:
                            if (reader.Value.Contains("R1"))
                            {
                                name = $"FOTA_{reader.Value}.bin";
                            }

                            break;
                    }
                }

                if (url == null || name == null)
                {
                    Console.WriteLine("Warning: URL or name null");
                    return false;
                }

                var path = BASEDIR + name;

                if (File.Exists(path))
                {
                    return false;
                }

                using var client = new WebClient();
                client.DownloadFile(new Uri(url), path);
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
                return false;
            }
        }

        public void Dispose()
        {
            _timer?.Dispose();
        }
    }
}