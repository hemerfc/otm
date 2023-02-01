//using Flurl.Http.Configuration;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json;
using System.Threading.Tasks;

namespace Otm.Server.Device.Licensing
{
    public class License
    {
        private const string parameter_licenseserverurl = "https://www.softwarewms.com.br/api/v1/LicenseValidate";

        private string HostIdentifier;
        private string DeviceIdentifier;
        private string Key;
        public License(string hostIdentifier, string deviceIdentifier, string key)
        {
            HostIdentifier = hostIdentifier;
            DeviceIdentifier = deviceIdentifier;
            Key = key;
        }

        public int GetRemainingHours()
        {
            var result = 0;

            try
            {
                var x = JsonPost(new LicensingRequest(HostIdentifier, DeviceIdentifier, Key));
                if (x.isValid)
                    result = x.remainingHours;
            }
            catch (Exception)
            {

            }

            return result;
        }


        public static LicensingResponse JsonPost(LicensingRequest request)
        {
            var result = new LicensingResponse();

            var httpClient = new HttpClient();

            string jsonRequest = JsonSerializer.Serialize(request);
            var response = httpClient.PostAsJsonAsync(parameter_licenseserverurl, jsonRequest);

            var responseJson = response.Result.Content.ToString();

            Console.WriteLine($"Response:\n: {responseJson}");

            result = JsonSerializer.Deserialize<LicensingResponse>(responseJson);

            return result;
        }
    }
}
