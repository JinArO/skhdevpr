using System.Collections.Generic;
using System.Xml.Serialization;

namespace PushRequest.Models
{
    public class EventXmlModel
    {
        public string CreatedBy { get; set; }
        public string PullRequestUri { get; set; }
    }

    public class EventXmlModel2
    {
        public string Title { get; set; }
    }
}
