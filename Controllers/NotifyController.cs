using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Xml;
using System.Xml.Serialization;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Caching.Memory;
using Newtonsoft.Json;
using PushRequest.Models;

namespace PushRequest.Controllers
{
    [Route("api/[Controller]/[Action]")]
    [AllowAnonymous]
    public class NotifyController : Controller
    {
        private readonly IHubContext<ChatHub> _hubContext;
        private readonly string apiurl= "https://notify-api.line.me/api/";
        private readonly IMemoryCache _cache;

        private readonly string _cachekey = "";

        public NotifyController(IHubContext<ChatHub> hubContext, IMemoryCache cache)
        {
            _hubContext = hubContext;
            _cache = cache;
        }

        /// <summary>
        /// PushRequest
        /// </summary>
        /// <returns></returns>
        [HttpPost]
        public IActionResult Push([FromQuery]string accesstoken)
        {
            using (StreamReader reader = new StreamReader(Request.Body))
            {
                string content = reader.ReadToEndAsync().Result;

                string str = content.Replace("&lt;", "");
                str = str.Replace("&gt;", "");
                str = str.Replace("&#xD;", "");

                EventXmlModel json = new EventXmlModel
                {
                    CreatedBy = str.Split("/CreatedBy")[0].Split("CreatedBy")[1].Trim(),
                    PullRequestUri = str.Split("/PullRequestUri")[0].Split("PullRequestUri")[1].Trim()
                };

                _hubContext.Clients.All.SendAsync("Push", JsonConvert.SerializeObject(json));
                APIPost(accesstoken, "notify", json.CreatedBy + " 有一個PR，請有空的人看一下 : " + json.PullRequestUri);

                var prePullRequestUri = _cache.Get<string>(_cachekey);
                if (prePullRequestUri == json.PullRequestUri)
                {
                    _hubContext.Clients.All.SendAsync("Debug", "duplicate");
                }
                else
                {
                    _cache.Set(_cachekey, json.PullRequestUri);
                    APIPost(accesstoken, "notify", json.CreatedBy + " : " + json.PullRequestUri);
                }

            }
            return Ok();
        }

        public void APIPost(string accesstoken, string action, string message)
        {
            HttpClient client = new HttpClient
            {
                BaseAddress = new Uri(apiurl)
            };
            client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", accesstoken);
            Dictionary<string, string> formDataDictionary = new Dictionary<string, string>()
            {
                {"message", message}
            };

            var formData = new FormUrlEncodedContent(formDataDictionary);


            HttpResponseMessage result = client.PostAsync(action, formData).Result;
            if (result.StatusCode == HttpStatusCode.OK)
            {
            }
            else
            {
                _ = result.Content.ReadAsStringAsync().Result;
            }
        }
      
        public class LineNotify
        {
            public string Message { get; set; }
        }
    }
}