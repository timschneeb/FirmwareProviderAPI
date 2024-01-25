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
        public string ModelString { get; }
        
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
                Model = ModelFromBuild(build);

                ModelString = build.Split("XX").First();
                
                BuildName = build;
                Path = path;
                Region = build.Substring(ModelString.Length, 2);
                BootloaderVersion = build.Substring(ModelString.Length + 2, 2);
                ReservedField = build[ModelString.Length + 4];
                
                Year = Array.FindIndex(_swYear, x => x == build[ModelString.Length + 5].ToString()) + 2015;
                Month = Array.FindIndex(_swMonth, x => x == build[ModelString.Length + 6].ToString()) + 1;
                Revision = Array.FindIndex(_swRelVer, x => x == build[ModelString.Length + 7].ToString());
            }
            catch (ArgumentOutOfRangeException ex)
            {
                throw new InvalidDataException("Invalid firmware string", ex);
            }
        }
        
        public static Models ModelFromBuild(string build)
        {
            return build.Split("XX").First() switch {
                "R170" => Models.Buds,
                "R175" => Models.BudsPlus,
                "R180" => Models.BudsLive,
                "R190" => Models.BudsPro,
                "R177" => Models.Buds2,
                "R510" => Models.Buds2Pro,
                "R400N" => Models.BudsFe,
                _ => Models.Unknown
            };
        }
    }
}