using System;
using System.ComponentModel;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using FirmwareProviderAPI;
using Microsoft.OpenApi.Extensions;

namespace FirmwareProviderCLI
{
    public static class Program
    {
        private static readonly string RootPath = Directory.GetCurrentDirectory();
        private static readonly string TempDownloadPath = Path.Combine(RootPath, "Firmware");
        private static readonly FirmwareScraper FirmwareScraper = new(TempDownloadPath);
        
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
        
        private static async Task Main(string[] args)
        {
            Console.WriteLine("Scraping firmware...");
            Directory.CreateDirectory(TempDownloadPath);
            await FirmwareScraper.CheckAll();
            
            var firmware = new DirectoryInfo(TempDownloadPath).GetFiles("FOTA_*.bin")
                .Select(BuildFirmware)
                .Where(x => x != null)
                .Cast<Firmware>()
                .OrderByDescending(x => x.Year)
                .ThenByDescending(x => x.Month)
                .ThenByDescending(x => x.Revision)
                .GroupBy(x => x.ModelString)
                .ToList();


            foreach (var model in firmware)
            {
                var modelDirectory = Path.Combine(RootPath, model.Key);
                if (!Directory.Exists(modelDirectory))
                {
                    Directory.CreateDirectory(modelDirectory);
                }

                foreach (var fw in model)
                {
                    if (fw == null) continue;
                    File.Copy(fw.Path, Path.Combine(modelDirectory, Path.GetFileName(fw.Path)), true);
                }
            }
            
            Directory.Delete(TempDownloadPath, true);

            // Load all firmwares
            firmware = new DirectoryInfo(RootPath).GetFiles("FOTA_*.bin", SearchOption.AllDirectories)
                .Select(BuildFirmware)
                .Where(x => x != null)
                .Cast<Firmware>()
                .OrderByDescending(x => x.Year)
                .ThenByDescending(x => x.Month)
                .ThenByDescending(x => x.Revision)
                .GroupBy(x => x.ModelString)
                .ToList();

            
            var indexMd = "# Galaxy Buds Firmware Archive\n\n";
            indexMd += "An automated archive of firmware images for the Galaxy Buds family. " +
                       "Flashable with the [GalaxyBudsClient application](https://github.com/ThePBone/GalaxyBudsClient).\n\n";
            indexMd += "> [!CAUTION]\n" +
                       "> Do NOT mix up firmware binaries of different models.\n" +
                       ">\n" +
                       "> For instance, flashing Galaxy Buds 2 Pro firmware onto the Galaxy Buds Pro will permanently brick your earbuds.\n\n";
            
            indexMd += "## Table of contents\n\n";
            foreach (var model in firmware)
            {
                var modelName = Firmware.ModelFromBuild(model.Key).GetAttributeOfType<DescriptionAttribute>().Description;
                indexMd += $"- [SM-{model.Key}](#{modelName.ToLower().Replace(' ', '-')}-sm-{model.Key.ToLower()})\n";
            }
            indexMd += "\n";
            
            foreach (var model in firmware)
            {

                var modelName = Firmware.ModelFromBuild(model.Key).GetAttributeOfType<DescriptionAttribute>().Description;
                indexMd += $"## {modelName} (SM-{model.Key})\n\n";
                indexMd += "| Build | Year | Month | Revision |\n";
                indexMd += "| ----- | ---- | ----- | -------- |\n";
                foreach (var fw in model)
                {
                    if(fw == null) continue;
                    var url = $"https://github.com/ThePBone/galaxy-buds-firmware-archive/raw/main/{model.Key}/{Path.GetFileName(fw.Path)}";
                    indexMd += $"| [`{fw.BuildName}`]({url}) | {fw.Year} | {DateTimeFormatInfo.InvariantInfo.GetMonthName(fw.Month)} | {fw.Revision} |\n";
                }
                indexMd += "\n";
            }
            
            indexMd += "\n\n_________________\n\n" +
                       "This is an automated archive that scrapes the binaries from Samsung's OTA servers every hour.\n\n" +
                       "The firmware binaries are provided as-is. Use at your own risk.\n";
            
            await File.WriteAllTextAsync("README.md", indexMd);
            
            Console.WriteLine("Done!");
        }
    }
}