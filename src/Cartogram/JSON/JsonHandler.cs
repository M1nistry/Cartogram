using System;
using System.Net;
using Newtonsoft.Json;

namespace Cartogram.JSON
{
    class JsonHandler
    {
        public static T ParseJson<T>(String json)
        {
            var webPage = DownloadUrl(json);

            if (webPage == null) return default(T);
            try
            {
                var result = JsonConvert.DeserializeObject<T>(webPage);
                return result;
            }
            catch (Exception e)
            {
                Console.WriteLine(e);

                return default(T);
            }
        }

        private static string DownloadUrl(string url)
        {
            var webReqeust = new WebClient();

            try
            {
                return webReqeust.DownloadString(url);
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                return null;
            }
        }
    }
}
