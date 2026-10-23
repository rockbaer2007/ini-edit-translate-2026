using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Ini_Edit___Translate;

namespace Ini_Edit___Translate
{
    //public class TranslateTextService : ITranslateText
    //{  
    //    ///<summary>  
    //    /// Translate the given text in to selected language.  
    //    /// </summary>  
    
    //    /// <param name="uri">Request uri</param>  
    //    /// <param name="text">The text is given for translation</param>  
    //    /// <param name="key">Subscription key</param>  
    //    /// <returns></returns>  
    //    public async Task<string> Translate(string uri, string text, string key)
    //    {
    //        System.Object[] body = new System.Object[] { new { Text = text } };
    //        var requestBody = JsonConvert.SerializeObject(body);

    //        using (var client = new HttpClient())
    //        using (var request = new HttpRequestMessage())
    //        {
    //            request.Method = HttpMethod.Post;
    //            request.RequestUri = new Uri(uri);
    //            request.Content = new StringContent(requestBody, Encoding.UTF8, "application/json");
    //            request.Headers.Add("Ocp-Apim-Subscription-Key", key);

    //            var response = await client.SendAsync(request);
    //            var responseBody = await response.Content.ReadAsStringAsync();
    //            dynamic result = JsonConvert.SerializeObject(JsonConvert.DeserializeObject(responseBody), Formatting.Indented);

    //            return result;
    //        }
    //    }
    //}
}

