using System;
using System.IO;
using System.Text;

namespace FirmwareProviderAPI
{
    public static class Log
    {
        private const string LogFile = "FirmwareProvider.log";
        private static FileStream _stream;

        static Log()
        {
            _stream = new FileStream(LogFile, FileMode.Append);
        }
        
        public enum Severity
        {
            VRB,
            DBG,
            INF,
            WRN,
            ERR
        }
        
        public static void V<T>(string str)
        {
            WriteLine<T>(Severity.VRB, str);
        }

        public static void D<T>(string str)
        {
            WriteLine<T>(Severity.DBG, str);
        }

        public static void I<T>(string str)
        {
            WriteLine<T>(Severity.INF, str);
        }

        public static void W<T>(string str)
        {
            WriteLine<T>(Severity.WRN, str);
        }

        public static void E<T>(string str)
        {
            WriteLine<T>(Severity.ERR, str);
        } 

        public static void WriteLine<T>(Severity sev, string msg, bool writeToFile = true)
        {
            WriteLine(sev, msg, typeof(T), writeToFile);
        }
        
        public static void WriteLine(Severity sev, string msg, Type? section = null, bool writeToFile = true)
        {
            var prefix = section?.Name switch
            {
                "Telegram" => "TMSG",
                "FumoScraper" => "FUMO",
                "FirmwareScraper" => "FWSC",
                _ => "MISC"
            };
            
            WriteLine(sev, prefix, msg, writeToFile);
        }
        
        public static void WriteLine(Severity sev, string prefix, string msg, bool writeToFile = true)
        {
            var line = $"[{sev}] [{prefix}] {msg}";
#if !DEBUG
            if(sev is (Severity.DBG or Severity.VRB))
            {
                return;
            }
#else
            Console.WriteLine(line);
#endif

            if (writeToFile && sev is not (Severity.DBG or Severity.VRB))
            {
                try
                {
                    _stream.Write(Encoding.UTF8.GetBytes($"{line}\r\n"));
                    _stream.Flush(true);
                }
                catch (Exception ex)
                {
                    // Do not use logging methods here to avoid endless loops
                    Console.WriteLine("[ERR] [LOGX] WriteLine: Cannot append to file: " + ex);
                }
            }
        }
    }
}