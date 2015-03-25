using System;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;

namespace StretchGarage.Shared
{
    public class WebApiResponse
    {
        public bool Success { get; set; }
        public string Message { get; set; }
        public object Content { get; set; }
        

        public WebApiResponse(string response, ApiCall call)
        {
            Success = Convert.ToBoolean(Between(response, "\"success\":", ",\"message\":"));
            Message = Between(response, ",\"message\":", ",\"content\":");

            Content = SetContentFromResponse(response, call);
        }

        private object SetContentFromResponse(string response, ApiCall call)
        {
            switch (call)
            {
                case ApiCall.CreateUnit:
                    return GetUnitId(response);
                case ApiCall.GetInterval:
                    return GetIntervalObject(response);
                default:
                    return null;
            }
        }

        private object GetUnitId(string response)
        {
            int id = -1;
            try
            {
                id = Convert.ToInt32(Between(response, ",\"content\":", "}"));
            }
            catch (Exception)
            {
                Success = false;
                Message = "Failed to parse content from server.";
                return null;
            }
            return id;
        }
        private CheckLocation GetIntervalObject(string response)
        {
            //"{\"success\":true,\"message\":\"\",\"content\":{\"interval\":10,\"checkSpeed\":false,\"isParked\":false}}"
            int interval = -1;
            bool checkSpeed = false;
            bool isParked = false;
            try
            {
                interval = Convert.ToInt32(Between(response, "\"interval\":", ",\"checkSpeed\"")) * 1000;
                checkSpeed = Convert.ToBoolean(Between(response, "\"checkSpeed\":", ",\"isParked\":"));
                isParked = Convert.ToBoolean(Between(response, ",\"isParked\":", "}}"));
            }
            catch (Exception)
            {
                Success = false;
                Message = "Failed to parse content from server.";
                return null;
            }
            return new CheckLocation(interval, checkSpeed, isParked);
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
