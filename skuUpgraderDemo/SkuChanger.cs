using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace skuUpgraderDemo
{
    public class SkuChanger
    {
        static string ResourceGroup = ConfigurationManager.AppSettings["ResourceGroup"];
        static string AnalysisServicesResource = ConfigurationManager.AppSettings["AnalysisServicesResource"];
        static string Location = ConfigurationManager.AppSettings["Location"];
        static string Subscription = ConfigurationManager.AppSettings["AzureSubscription"];


        public static async Task LoginAndUpdateSKUAsync(string newSku)
        {
            string tenantId = ConfigurationManager.AppSettings["AzureTenantId"];
            string clientId = ConfigurationManager.AppSettings["AzureClientId"];
            string clientSecret = ConfigurationManager.AppSettings["AzureClientSecret"];

            string token = await AuthenticationHelpers.AcquireTokenBySPN(tenantId, clientId, clientSecret);

            using (var client = new HttpClient(new HttpClientHandler()))
            {
                client.DefaultRequestHeaders.Add("Authorization", "Bearer " + token);
                client.BaseAddress = new Uri("https://management.azure.com/");
                var requestUri = $"/subscriptions/{Subscription}/resourceGroups/{ResourceGroup}/providers/Microsoft.AnalysisServices/servers/{AnalysisServicesResource}?api-version=2016-05-16";
                using (var response = await client.GetAsync(requestUri)) 
                {
                    response.EnsureSuccessStatusCode();
                    string responseBody = await response.Content.ReadAsStringAsync();
                    var json = IterateJsonAndReplaceSku(responseBody, newSku);
                    using (var newResponse = await client.PutAsJsonAsync(requestUri, json))
                        {
                            newResponse.EnsureSuccessStatusCode();
                        }
                    

                }

            }

        }

        public static async Task<string> LoginAndGetAnalysisServicesPropertyValue(string propertyName, string path)
        {
            string tenantId = ConfigurationManager.AppSettings["AzureTenantId"];
            string clientId = ConfigurationManager.AppSettings["AzureClientId"];
            string clientSecret = ConfigurationManager.AppSettings["AzureClientSecret"];

            string token = await AuthenticationHelpers.AcquireTokenBySPN(tenantId, clientId, clientSecret);

            using (var client = new HttpClient(new HttpClientHandler()))
            {
                client.DefaultRequestHeaders.Add("Authorization", "Bearer " + token);
                client.BaseAddress = new Uri("https://management.azure.com/");
                var requestUri = $"/subscriptions/{Subscription}/resourceGroups/{ResourceGroup}/providers/Microsoft.AnalysisServices/servers/{AnalysisServicesResource}?api-version=2016-05-16";
                using (var response = await client.GetAsync(requestUri)) // 2017-08-01-beta
                {
                    response.EnsureSuccessStatusCode();
                    string responseBody = await response.Content.ReadAsStringAsync();
                    return GetAASProperty(responseBody, propertyName, path);

                }
            }
        }
        private static string GetAASProperty(string responseBody, string propertyName, string path)
        {
            try
            {
                var data = (JObject)Newtonsoft.Json.JsonConvert.DeserializeObject(responseBody);
                JToken jtoken;
                var props = data.TryGetValue(propertyName, out jtoken);

                foreach (JToken item in jtoken.Children())
                {
                    if (item.Path == path)
                    {
                        return item.First.Value<string>();
                    }

                }
            }
            catch (Exception ex)
            {

               // log error here
            }


            return null;
        }

        public static string GetCurrentAASSku()
        {
            return LoginAndGetAnalysisServicesPropertyValue("sku", "sku.name").Result;
        }

        public static string CheckTheAnalysisServicesStatus()
        {
            return LoginAndGetAnalysisServicesPropertyValue("properties", "properties.provisioningState").Result;

        }

        public static Dictionary<string, object> IterateJsonAndReplaceSku(string responseBody, string newSku)
        {
            var data = (JObject)Newtonsoft.Json.JsonConvert.DeserializeObject(responseBody);
            Dictionary<string, object> newObj = new Dictionary<string, object>();
            foreach (var item in data)
            {
                if (item.Key.ToLowerInvariant() == "sku")
                {
                    var sku =
                     new
                     {
                         Name = newSku
                     };


                    newObj[item.Key] = sku;
                }
                else
                {
                    newObj[item.Key] = item.Value;
                }
            }

            return newObj;

        }

        public static void EnsureAnalysisServicesReady()
        {
            int retryCount = 0;
            while (CheckTheAnalysisServicesStatus() != "Succeeded" && retryCount < 10)
            {
                Thread.Sleep(60000);
                retryCount++;
            }
        }
    }
}
