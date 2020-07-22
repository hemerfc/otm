
using System;
using System.Net;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading.Tasks;
using Otm.Shared.ContextConfig;

namespace Otm.Client.Services
{
    public class EnvironmentService
    {
        private readonly HttpClient client;

        public EnvironmentService(HttpClient client)
        {
            this.client = client;
        }

        public async Task<RootConfig[]> GetAll()
        {
            var configs = await client.GetFromJsonAsync<RootConfig[]>("Enviroment");
            return configs;
        }

        public async Task<RootConfig> GetById(string id)
        {
            var config = await client.GetFromJsonAsync<RootConfig>($"Enviroment/{id}");
            return config;
        }

        public async Task<RootConfig> Create(RootConfig config)
        {
            var response = await client.PostAsJsonAsync("Enviroment", config);
            if (response.StatusCode == HttpStatusCode.OK)
            {
                var newConfig = await response.Content.ReadFromJsonAsync<RootConfig>();
                return newConfig;
            }

            return null;
        }

        public async Task<RootConfig> Update(RootConfig config)
        {
            var response = await client.PutAsJsonAsync("Enviroment", config);
            if (response.StatusCode == HttpStatusCode.OK)
            {
                var newConfig = await response.Content.ReadFromJsonAsync<RootConfig>();
                return newConfig;
            }

            return null;
        }

        public async Task Delete(string id)
        {
            await client.DeleteAsync($"Enviroment/{id}");
        }
    }
}