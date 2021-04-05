using System;
using System.IO;
using System.Linq;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;

namespace FirmwareProviderAPI
{
    public class Firmware
    {
        private readonly string[] _swMonth = {"A", "B", "C", "D", "E", "F", "G", "H", "I", "J", "K", "L"};
 
        private readonly string[] _swRelVer =
        {
            "0", "1", "2", "3", "4", "5", "6", "7", "8", "9", "A", "B", "C", "D", "E", "F", "G", "H", "I", "J", "K",
            "L", "M", "N", "O", "P", "Q", "R", "S", "T", "U", "V", "W", "X", "Y", "Z"
        };
 
        private readonly string[] _swVer = {"E", "U"};
        private readonly string[] _swYear = {"O", "P", "Q", "R", "S", "T", "U", "V", "W", "X", "Y", "Z"};
        
        public Models Model { get; }
        
        public string BuildName { get; }
        public string Region { get; }
        public string BootloaderVersion { get; }
        public char ReservedField { get; }
        public int Year { get; }
        public int Month { get; }
        public int Revision { get; }

        [JsonIgnore] public string Path { get; }

        /*
         * Build string:
         * 
         * R175 XX U0 A T F 2
         * R175 XX U0 A U B 3
         * Model     
         *      Region
         *         Bootloader
         *            Reserved
         *              Year
         *                Month
         *                  Revision
         */
        
        public Firmware(string build, string path)
        {
            try
            {
                Model = build.Substring(0, 4) switch {
                    "R170" => Models.Buds,
                    "R175" => Models.BudsPlus,
                    "R180" => Models.BudsLive,
                    "R190" => Models.BudsPro,
                    _ => Models.Unknown
                };
                
                BuildName = build;
                Path = path;
                Region = build.Substring(4, 2);
                BootloaderVersion = build.Substring(6, 2);
                ReservedField = build[8];
                
                Year = Array.FindIndex(_swYear, x => x == build[9].ToString()) + 2015;
                Month = Array.FindIndex(_swMonth, x => x == build[10].ToString()) + 1;
                Revision = Array.FindIndex(_swRelVer, x => x == build[11].ToString());
            }
            catch (ArgumentOutOfRangeException ex)
            {
                throw new InvalidDataException("Invalid firmware string", ex);
            }
        }
    }
}