using System;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Text.RegularExpressions;

namespace FirmwareProviderAPI.Messaging
{
    public class Telegram
    {
        static Telegram(){}
        
        public static Telegram Instance { get; } = new();
        
        private const string TokenPath = "telegram-token.conf";
        private readonly string Token;
        private readonly HttpClient _client = new();

        private Telegram()
        {
            try
            {
                Token = Regex.Replace(File.ReadAllText(TokenPath), @"\r\n?|\n", string.Empty);
            }
            catch (FileNotFoundException ex)
            {
                Log.E<Telegram>("Token file missing. Telegram integration disabled: " + ex);
                Token = string.Empty;
            }
        }
        
        public async void SendMarkup(string message)
        {
            if (Token == string.Empty)
            {
                Log.W<Telegram>("Telegram integration disabled");
                return;
            }
            
            var url = $"https://api.telegram.org/bot{Token}/sendMessage?chat_id=665100591&parse_mode=markdown&text={Uri.EscapeDataString(message)}";
            try
            {
                var request = new HttpRequestMessage(HttpMethod.Get, url);
                await _client.SendAsync(request);
            }
            catch (Exception ex)
            {
                Log.E<Telegram>("Failed to send message: " + ex);
            }
        }

        public static void Send(string message)
        {
            Instance.SendMarkup(message);
        }
    }
}