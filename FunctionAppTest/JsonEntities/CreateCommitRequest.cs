using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using System;
using System.Collections.Generic;
using System.Text;

namespace FunctionAppTest.JsonEntities
{
    class CreateCommitRequest
    {
        [JsonProperty("message")]
        public string Message { get; set; }
        [JsonProperty("parents")]
        public string[] Parents { get; set; }
        [JsonProperty("tree")]
        public string Tree;
    }
}
