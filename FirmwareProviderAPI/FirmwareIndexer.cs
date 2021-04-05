﻿using System;
using System.Collections.Generic;
using System.ComponentModel;
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

        public static IReadOnlyList<Firmware> Firmwares = new List<Firmware>();
        
        public static void Init()
        {
            _fileProvider = new PhysicalFileProvider(RootPath);
            Rescan();
            WatchForFileChanges();
        }
        
        private static void WatchForFileChanges()
        {
            if (_fileProvider == null)
            {
                Console.WriteLine("Warning: FileProvider not ready");
                return;
            }
            
            _fileChangeToken = _fileProvider.Watch("*.bin");
            _fileChangeToken.RegisterChangeCallback(Notify, default);
        }
        
        private static void Notify(object state)
        {
            Console.WriteLine($"Update detected");
            Rescan();
            WatchForFileChanges();
        }

        private static void Rescan()
        {
            Firmwares = new DirectoryInfo(RootPath).GetFiles("*.bin")
                .Select(BuildFirmware)
                .Where(x => x != null)
                .Cast<Firmware>()
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