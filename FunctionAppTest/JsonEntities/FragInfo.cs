using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Text;

namespace FunctionAppTest.JsonEntities
{
    class FragInfo
    {
        [JsonProperty("startline")]
        public int StartLine { get; set; }
        [JsonProperty("endline")]
        public int Endline { get; set; }
        [JsonProperty("url")]
        public string Url { get; set; }
    }
}
