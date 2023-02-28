using System;
using System.Net.Http;
using Newtonsoft.Json;
using System.Net.Http.Headers;
using System.Text;

namespace TeamsPresence
{
    public class HomeAssistantService
    {
        private string Token { get; set; }
        private string Url { get; set; }
        private HttpClient Client { get; set; }

        public HomeAssistantService(string url, string token)
        {
            Token = token;
            Url = url;
            Client = new HttpClient();
            Client.BaseAddress = new Uri(url);
            Client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", Token);
            Client.DefaultRequestHeaders.Add("User-Agent", "Teams-Presence");
        }

        public async void UpdateEntity(string entity, string state, string friendlyName, string icon, string[] options = null)
        {
            var update = new HomeAssistantEntityStateUpdate()
            {
                State = state,
                Attributes = new HomeAssistantEntityStateUpdateAttributes()
                {
                    FriendlyName = friendlyName,
                    Icon = icon
                }
            };

            if (options != null)
            {
                update.Attributes.DeviceClass = "enum";
                update.Attributes.Options = options;
            }
            
            try
            {
                HttpResponseMessage response = await Client.PostAsync($"api/states/{entity}", new StringContent(JsonConvert.SerializeObject(update), Encoding.UTF8, "application/json"));
                response.EnsureSuccessStatusCode();
            }
            catch (HttpRequestException e)
            {
                Console.WriteLine(e.Message);
            }

        }
    }
}
