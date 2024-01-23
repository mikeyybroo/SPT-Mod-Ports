using Aki.Common.Http;
using Newtonsoft.Json;

namespace QuestZoneAPI.Helpers
{
    public class WebRequestHelper
    {
        public static T Get<T>(string url)
        {
            var req = RequestHandler.GetJson(url);
            return JsonConvert.DeserializeObject<T>(req);
        }
    }
}
