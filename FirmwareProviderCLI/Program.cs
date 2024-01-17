using System;
using System.IO;
using System.Threading.Tasks;
using FirmwareProviderAPI;

namespace FirmwareProviderCLI
{
    public static class Program
    {
        private static readonly string RootPath = Path.Combine(Directory.GetCurrentDirectory(), "Firmware");
        private static readonly FirmwareScraper FirmwareScraper = new(RootPath);

        private static async Task Main(string[] args)
        {
            Console.WriteLine("Scraping firmware...");
            Directory.CreateDirectory("Firmware/");
            await FirmwareScraper.CheckAll();
            Console.WriteLine("Done!");
        }
    }
}