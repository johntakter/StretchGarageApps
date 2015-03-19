using System;
using System.Collections.Generic;
using System.Text;
using System.Web;
using System.Net;
using System.IO;
using System.Threading.Tasks;
using System.Text.RegularExpressions;

namespace StretchGarage.Shared
{
    public class ApiRequest
    {
        /// <summary>
        /// Method creates unit on server and its db.
        /// Adds users name to unit.
        /// Returns the id in db for the user.
        /// </summary>
        /// <returns></returns>
        public async static Task<int> GetUnitId(string name)
        {
            string url = string.Format("http://localhost:3186/api/Unit/{0}/{1}", name, 0);

            int id = -1;
            try
            {
                var content = await GetContent(url);
                id = Convert.ToInt32(content);
            }
            catch (Exception) { throw; }
            return id;
        }

        /// <summary>
        /// Gets intervall from server for unit decided
        /// by params lat/long given on call.
        /// </summary>
        /// <param name="latitude"></param>
        /// <param name="longitude"></param>
        /// <returns></returns>
        public async static Task<object> GetInterval(int id, double latitude, double longitude)
        {
            string url = "http://stretchgarageweb.azurewebsites.net/api/Unit/";

            var content = await GetContent(url);
            return content;
        }

        /// <summary>
        /// Gets content from http request from server with given
        /// url in param.
        /// returns content of response from server to caller.
        /// </summary>
        /// <param name="url"></param>
        /// <returns></returns>
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

        internal class HttpResponseMessage
        {
            public bool Success { get; set; }
            public string Message { get; set; }
            public bool Content { get; set; }
            //"{\"success\":true,\"message\":\"\",\"content\":4}";
            public HttpResponseMessage(string response, int request)
            {
                Success = Convert.ToBoolean(Between(response, "\"success\":", ",\"message\":"))
            }

            private ParseContentFromResponse(string response, int request)
            {
                switch (request)
                {
                    case 0:

                        break;
                }
            }

            static string Between(string source, string left, string right)
            {
                return Regex.Match(
                    source, 
                    string.Format("{0}(.*){1}", left, right))
                    .Groups[1].Value;
            }
        } 
    }
}
