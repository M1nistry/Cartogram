using System;
using System.Net;
using Newtonsoft.Json;

namespace Cartogram.JSON
{
    class JsonHandler
    {
        //All Ladders currently running
        public static T ParseJson<T>(String json)
        {
            string webPage = DownloadUrl(json);
            if (webPage != null)
            {
                try
                {
                    var poeLadderAll = JsonConvert.DeserializeObject<T>(webPage);


                    return poeLadderAll;
                }
                catch (Exception e)
                {
                    Console.WriteLine(e);

                    return default(T);
                }
            }
            return default(T);
        }

        public static dynamic ParseJsonObject(string json)
        {
            string webPage = DownloadUrl(json);
            if (webPage != null)
            {
                try
                {
                    if (webPage.Contains("No data returned")) return null;
                    var jObject = JsonConvert.DeserializeObject(webPage);
                    return jObject;
                }
                catch (Exception e)
                {
                    Console.WriteLine(e);
                    return null;
                }
            }
            return null;
        }

        //Download provided URL and return as String
        public static String DownloadUrl(String url)
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
