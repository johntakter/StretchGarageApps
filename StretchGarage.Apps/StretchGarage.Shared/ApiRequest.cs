using System;
using System.Collections.Generic;
using System.Text;
using System.Web;
using System.Net;
using System.IO;
using System.Threading.Tasks;

namespace StretchGarage.Shared
{
    public class ApiRequest
    {
        public async static Task<object> GetUnitId()
        {
            sbytetring url = "h" +
                             "ttp://stretchgarageweb.azurewebsites.net/api/ParkingPlace/0";

            var content = await GetContent(url);
            return content;
        }

        public async static Task<object> GetInterval(double latitude, double longitude)
        {
            string url = "http://stretchgarageweb.azurewebsites.net/api/Unit/";

            var content = await GetContent(url);
            return content;
        }

        async static Task<object> GetContent(string url)
        {
            // Create an HTTP web request using the URL:
            HttpWebRequest request = (HttpWebRequest)HttpWebRequest.Create(new Uri(url));
            request.ContentType = "application/json";
            request.Method = "GET";

            // Send the request to the server and wait for the response:
            using (WebResponse response = await request.GetResponseAsync())
            {
                // Get a stream representation of the HTTP web response:
                using (Stream stream = response.GetResponseStream())
                {
                    using (StreamReader reader = new StreamReader(stream))
                    {
                        var content = reader.ReadToEnd();
                        return content;
                    }

                    // Use this stream to build a JSON document object:
                    //JsonValue jsonDoc = await Task.Run(() => JsonObject.Load(stream));
                    //Console.Out.WriteLine("Response: {0}", jsonDoc.ToString());
                    // Return the JSON document:
                    //return jsonDoc;
                }
            }
        }
    }
}
