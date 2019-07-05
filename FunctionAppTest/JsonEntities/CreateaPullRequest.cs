using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Text;

namespace FunctionAppTest.JsonEntities
{
    public class CreatePullRequest
    {
        [JsonProperty("title")]
        public string Title { get; set; }
        [JsonProperty("head")]
        public string Head { get; set; }
        [JsonProperty("base")]
        public string Base { get; set; }
        [JsonProperty("body")]
        public string Body { get; set; }
    }
}
