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
using Newtonsoft.Json;
using PushRequest.Models;

namespace PushRequest.Controllers
{
    [Route("api/[Controller]/[Action]")]
    [AllowAnonymous]
    public class WebHookController : Controller
    {
        private IHubContext<ChatHub> _hubContext;
        private string apiurl = "https://api.line.me/v2/bot/";
        private string apiurloauth = "https://api.line.me/v2/oauth/";

        public WebHookController(IHubContext<ChatHub> hubContext)
        {
            _hubContext = hubContext;
        }

        /// <summary>
        /// PushRequest
        /// </summary>
        /// <returns></returns>
        [HttpPost]
        public IActionResult Commit()
        {
            using (StreamReader reader = new StreamReader(Request.Body))
            {
                string content = reader.ReadToEndAsync().Result;
                string str = content.Replace("&lt;", "");
                str = str.Replace("&gt;", "");
                str = str.Replace("&#xD;", "");

                EventXmlModel2 json = new EventXmlModel2();
                json.Title = str.Split("/Title")[0].Split("Title")[1].Trim();
                _hubContext.Clients.All.SendAsync("Commit", JsonConvert.SerializeObject(json));
            }
            return Ok();
        }

        /// <summary>
        /// PushRequest
        /// </summary>
        /// <returns></returns>
        [HttpPost]
        public IActionResult Push([FromQuery]string id, [FromQuery]string channelaccesstoken)
        {
            using (StreamReader reader = new StreamReader(Request.Body))
            {
                string content = reader.ReadToEndAsync().Result;
 
                string str = content.Replace("&lt;", "");
                str = str.Replace("&gt;", "");
                str = str.Replace("&#xD;", "");

                EventXmlModel json = new EventXmlModel();
                json.CreatedBy = str.Split("/CreatedBy")[0].Split("CreatedBy")[1].Trim();
                json.PullRequestUri = str.Split("/PullRequestUri")[0].Split("PullRequestUri")[1].Trim();

                _hubContext.Clients.All.SendAsync("Push", JsonConvert.SerializeObject(json));
                LinePush p = new LinePush();
                p.to = id;
                p.messages = new List<LineMessage>();
                LineMessage message = new LineMessage();
                message.type = "text";
                message.text = json.CreatedBy + " : " + json.PullRequestUri;
                p.messages.Add(message);
                APIPost(channelaccesstoken, "message/push", p);
            }
            return Ok(id);
        }
        public void APIPost(string channelaccesstoken, string action, object data)
        {
            HttpClient client = new HttpClient();
            client.BaseAddress = new Uri(apiurl);
            client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", channelaccesstoken);
            HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Post, action);
            request.Content = new StringContent(JsonConvert.SerializeObject(data), Encoding.UTF8, "application/json");
            HttpResponseMessage result = client.SendAsync(request).Result;
            if (result.StatusCode == HttpStatusCode.OK)
            {
            }
            else
            {
            }
        }
        /// <summary>
        /// Chat
        /// </summary>
        /// <returns></returns>
        [HttpPost]
        public IActionResult Chat()
        {
            using (StreamReader reader = new StreamReader(Request.Body))
            {
                string content = reader.ReadToEndAsync().Result;
                _hubContext.Clients.All.SendAsync("Chat", content);
            }
            return Ok();
        }
        public class LinePush
        {
            public LinePush()
            {
                messages = new List<LineMessage>();
            }
            public string to { get; set; }
            public List<LineMessage> messages { get; set; }
        }
        public class LineMessage
        {
            public string type { get; set; }
            public string id { get; set; }
            public string text { get; set; }
        }
    }
}