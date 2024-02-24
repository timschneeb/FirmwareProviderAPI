using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Primitives;

namespace FirmwareProviderAPI
{
    public static class FirmwareIndexer
    {
        private static PhysicalFileProvider? _fileProvider;
        private static IChangeToken? _fileChangeToken;
        private static readonly string RootPath = Path.Combine(Directory.GetCurrentDirectory(), "Firmware");
        private static readonly FirmwareScraper FirmwareScraper = new(RootPath);
        
        public static IReadOnlyList<Firmware> Firmwares = new List<Firmware>();
        
        public static void Init()
        {
            Directory.CreateDirectory("Firmware/");
            
            FirmwareScraper.Resume();
            _fileProvider = new PhysicalFileProvider(RootPath);
            Rescan();
            WatchForFileChanges();
        }
        
        private static void WatchForFileChanges()
        {
            if (_fileProvider == null)
            {
                return;
            }
            
            _fileChangeToken = _fileProvider.Watch("FOTA_*.bin");
            _fileChangeToken.RegisterChangeCallback(Notify, default);
        }
        
        private static void Notify(object state)
        {
            Rescan();
            WatchForFileChanges();
        }

        private static void Rescan()
        {
            Firmwares = new DirectoryInfo(RootPath).GetFiles("FOTA_*.bin")
                .Select(BuildFirmware)
                .Where(x => x != null)
                .Cast<Firmware>()
                .OrderByDescending(x => x.Year)
                .ThenByDescending(x => x.Month)
                .ThenByDescending(x => x.Revision)
                .ToList();
        }

        private static Firmware? BuildFirmware(FileInfo file)
        {
            try
            {
                var build = Path.GetFileNameWithoutExtension(file.Name);
                build = build.Replace("FOTA_", "");
                return new Firmware(build, file.FullName);
            }
            catch (InvalidDataException ex)
            {
                Console.WriteLine(ex);
                return null;
            }
        }
    }
}