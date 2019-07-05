using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Text;

namespace FunctionAppTest.JsonEntities
{
    public class TreeNode
    {
        [JsonProperty("path")]
        public string Path { get; set; }
        [JsonProperty("mode")]
        public string Mode { get; set; }
        [JsonProperty("type")]
        public string Type { get; set; }
        [JsonProperty("sha")]
        public string Sha { get; set; }
    }
}
