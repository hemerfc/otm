using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text.Json;
using System.Threading.Tasks;

namespace Otm.Server.Device.Licensing
{
    public class License
    {
        private const string parameter_licenseserverurl = "https://www.softwarewms.com.br/api/V1/KeyValidate";

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
            catch (Exception ex)
            {

            }

            return result;
        }



        public static LicensingResponse JsonPost(LicensingRequest request)
        {
            var result = new LicensingResponse();


            var httpWebRequest = (HttpWebRequest)WebRequest.Create(parameter_licenseserverurl);
            httpWebRequest.ContentType = "application/json";
            httpWebRequest.Method = "POST";

            using (var streamWriter = new StreamWriter(httpWebRequest.GetRequestStream()))
            {
                string json = JsonSerializer.Serialize(request);

                streamWriter.Write(json);
            }

            var httpResponse = (HttpWebResponse)httpWebRequest.GetResponse();
            using (var streamReader = new StreamReader(httpResponse.GetResponseStream()))
            {
                var responseJson = streamReader.ReadToEnd();

                Console.WriteLine($"Response:\n: {responseJson}");

                result = JsonSerializer.Deserialize<LicensingResponse>(responseJson);
            }

            return result;
        }
    }
}
