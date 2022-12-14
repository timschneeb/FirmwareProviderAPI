using System;
using System.IO;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using FirmwareProviderAPI.Messaging;
using SamsungFumoClient;
using SamsungFumoClient.Exceptions;
using SamsungFumoClient.SyncML;
using SamsungFumoClient.SyncML.Commands;
using SamsungFumoClient.SyncML.Elements;
using SamsungFumoClient.SyncML.Enum;
using SamsungFumoClient.Utils;

namespace FirmwareProviderAPI
{
    public class FumoScraper
    {
        public string? DownloadPath { init; get; }

        static FumoScraper()
        {
            SamsungFumoClient.Utils.Log.OnLogEvent += (_, args) =>
            {
                var s = (Log.Severity) args.Severity;
                switch (s)
                {
                    case Log.Severity.DBG:
                    case Log.Severity.VRB:
                        return;
                    default:
                        Log.WriteLine<FumoScraper>(s, args.Message, s is Log.Severity.ERR or Log.Severity.WRN);
                        break;
                }
            };
        }
        
        public async Task<FirmwareObject?> FindUpdate(string model)
        {
            var device = new Device
            {
                Model = model,
                CustomerCode = "KOO",
            };

            var session = new DmSession(device);
            var body = new SyncBody
            {
                Cmds = new Cmd[]
                {
                    new Alert
                    {
                        CmdID = 1,
                        Data = AlertTypes.CLIENT_INITIATED_MGMT /* Client-initiated session */
                    },
                    new Replace
                    {
                        CmdID = 2,
                        Item = SyncMlUtils.BuildItemList(session.Device.AsDevInfNodes()) /* Upload device info */
                    },
                    new Alert
                    {
                        CmdID = 3,
                        Data = AlertTypes.GENERIC,
                        Item = new[] /* Submit FUMO service request */
                        {
                            new Item
                            {
                                Source = new Source
                                {
                                    LocURI = "./FUMO/DownloadAndUpdate"
                                },
                                Meta = new Meta
                                {
                                    Format = "chr",
                                    Type = "org.openmobilealliance.dm.firmwareupdate.devicerequest"
                                },
                                Data = new PcData
                                {
                                    Data = "0"
                                }
                            }
                        }
                    }
                }
            };

            try
            {
                // Initiate authentication handshakes by solving challenge objects
                SyncDocument? initialRequest = null;
                var attempt = 0;
                const int maxAttempt = 5;
                do
                {
                    if (attempt >= maxAttempt)
                    {
                        Telegram.Send(
                            $"[FUMO] Error while searching updates for {model}. Failed to authenticate after {maxAttempt} transactions");
                        Log.E<FumoScraper>(
                            $"[{model}] Giving up to authenticate after failed {maxAttempt} challenge transactions");
                        return null;
                    }

                    attempt++;
                    Log.D<FumoScraper>($"[{model}] Authenticating with server ({attempt}/{maxAttempt} transactions)");
                    try
                    {
                        initialRequest = await session.SendAsync(body);
                    }
                    catch (HttpException ex)
                    {
                        Log.W<FumoScraper>($"[{model}] Failed to authenticate. Server returned error {ex}");
                    }

                    if (session.IsAborted)
                    {
                        Log.E<FumoScraper>($"[{model}] Server has aborted the connection. Closing session.");
                        return null;
                    }
                } while (initialRequest == null || !SyncMlUtils.IsAuthorizationAccepted(initialRequest.SyncBody?.Cmds));

                // Check whether server is requested next information
                if (!SyncMlUtils.HasCommand<Get>(initialRequest?.SyncBody?.Cmds))
                {
                    Log.E<FumoScraper>($"[{model}] Server is unexpectedly not requesting additional device information");
                    return null;
                }

                // Process and respond to GET request
                var dataSource = ArrayUtils.ConcatArray(
                    session.Device.AsDevInfNodes(),
                    await session.Device.AsDevDetailNodes()
                );
                var getResultCmds = SyncMlUtils.BuildGetResults(initialRequest, dataSource, 2);

                var fullResultCmds = ArrayUtils.ConcatArray(
                    new[] {session.BuildAuthenticationStatus()},
                    getResultCmds
                );
                var firmwareUpdateResult = await session.SendAsync(new SyncBody
                {
                    Cmds = fullResultCmds
                });
                if (session.IsAborted)
                {
                    Log.E<FumoScraper>($"[{model}] Server has aborted the connection. Closing session.");
                    return null;
                }

                // Check for SvcState error code in response
                var svcState = SyncMlUtils.ExtractSvcState(firmwareUpdateResult.SyncBody?.Cmds);
                switch (svcState)
                {
                    case 260:
                        // No update for your device configuration detected
                        return null;
                    case 220:
                        Log.E<FumoScraper>(
                            $"[{model}] SvcState = 220: No suitable firmware version found. Please check the supplied parameters.");
                        return null;
                    case >= 0:
                        Telegram.Send(
                            $"[FUMO] SvcState '{svcState.ToString()}' returned while searching updates for {model}.");
                        Log.E<FumoScraper>(
                            $"[{model}] Firmware request failed: ./FUMO/Ext/SvcState is set to {svcState.ToString()}");
                        break;
                }

                // Acknowledge message and end transaction
                await session.SendAsync(new SyncBody
                {
                    Cmds = SyncMlUtils.BuildSuccessResponses(firmwareUpdateResult, new (string cmdType, string code)[]
                    {
                        ("Exec", "202")
                    })
                });

                // Extract firmware download URI from response if found
                var descriptorUri = SyncMlUtils.FindFirmwareUpdateUri(firmwareUpdateResult);
                if (descriptorUri == null)
                {
                    Telegram.Send(
                        $"[FUMO] Error while searching updates for {model}\n" +
                        $"The server did not include a download descriptor uri with its response");
                    Log.E<FumoScraper>(
                        $"[{model}] The server did not include a download descriptor uri with its response. Cannot continue.");
                    return null;
                }

                var firmware = await session.RetrieveFirmwareObjectAsync(descriptorUri);
                if (firmware == null)
                {
                    Telegram.Send(
                        $"[FUMO] Error while accessing URI '{descriptorUri}'\n" +
                        $"Download descriptor cannot be downloaded or is unreadable.");
                    Log.E<FumoScraper>($"[{model}] Download descriptor cannot be downloaded or is unreadable. Cannot continue.");
                    return null;
                }

                Log.D<FumoScraper>($"Firmware found: {firmware.Version.ApplicationProcessor}");

                await session.AbortSessionAsync();
                return firmware;
            }
            catch (IndexOutOfRangeException ex)
            {
                Log.E<FumoScraper>($"[{model}] Failed to parse wbxml: " + ex);
                return null;
            }
            catch (SyncMlParseException ex)
            {
                Log.E<FumoScraper>($"[{model}] Failed to parse wbxml: " + ex);
                return null;
            }
        }

        public async Task DownloadUpdate(FirmwareObject firmwareObject)
        {
            if (DownloadPath == null)
            {
                Log.E<FumoScraper>("Download path not yet set");
                return;
            }
            
            using var client = new WebClient();
            var binary = await client.DownloadDataTaskAsync(new Uri(firmwareObject.Uri));

            var fwString = firmwareObject.Version.ApplicationProcessor[..4];

            string path;
            if (Utils.ArrayUtils.FindPattern(binary, Encoding.ASCII.GetBytes(fwString)) < 0)
            {
                Log.E<FumoScraper>($"Firmware did not contain '{fwString}' ASCII byte pattern");
                Telegram.Send($"*Firmware verification check failed for {firmwareObject.Version.ApplicationProcessor}*.\n" +
                              $"Manual interaction requested");
                path = $"{DownloadPath}/DISCARDED_{firmwareObject.Version.ApplicationProcessor}.bin";
            }
            else
            {
                path = $"{DownloadPath}/FOTA_{firmwareObject.Version.ApplicationProcessor}.bin";
            }
            
            if (File.Exists(path))
            {
                // Update already retrieved
                return;
            }
            
            Telegram.Send($"\\[PRD] Firmware update '*{firmwareObject.Version.ApplicationProcessor}*' has been released");
            await File.WriteAllBytesAsync(path, binary);
        }
    }
}