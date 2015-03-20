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
        private static string _weburl = "http://localhost:3186/api"; //http://stretchgarageweb.azurewebsites.net/api

        /// <summary>
        /// Method creates unit on server and its db.
        /// Adds users name to unit.
        /// Returns the id in db for the user.
        /// </summary>
        /// <returns></returns>
        public async static Task<WebApiResponse> GetUnitId(string name)
        {
            string url = string.Format("http://localhost:3186/api/Unit/{0}/{1}", name, 0);
            //var content = await GetContent(url);
            string test = "{\"success\":true,\"message\":\"\",\"content\":4}";

            var response = new WebApiResponse(test, ApiCall.CreateUnit);
            return response;
        }

        /// <summary>
        /// Gets intervall from server for unit decided
        /// by params lat/long given on call.
        /// </summary>
        /// <param name="latitude"></param>
        /// <param name="longitude"></param>
        /// <returns></returns>
        public async static Task<WebApiResponse> GetInterval(int id, double latitude, double longitude)
        {
            string url = string.Format("{0}/CheckLocation/{1}/{2}/{3}", _weburl, id, latitude, longitude);

            var content = await GetContent(url);

            var response = new WebApiResponse(Convert.ToString(content), ApiCall.GetInterval);
            return response;
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
            request.Timeout = 1000;
            object content = null;
            try
            {
                using (WebResponse response = await request.GetResponseAsync()) // Send the request to the server and wait for the response
                {
                    using (Stream stream = response.GetResponseStream()) // Get a stream representation of the HTTP web response
                    {
                        using (StreamReader reader = new StreamReader(stream))
                        {
                            content = reader.ReadToEnd();
                        }
                    }
                }
            }
            catch (Exception) { throw; }

            return content;
        }
    }
}
