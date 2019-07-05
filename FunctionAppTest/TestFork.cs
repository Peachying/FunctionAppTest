using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Net;
using System.Text;

namespace FunctionAppTest
{
    public static class TestFork
    {
        private static string url_fork = @"https://api.github.com/repos/Peachying/Priceprediction/forks";

        [FunctionName("TestFork")]
        public static async Task<IActionResult> Run(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", "post", Route = null)] HttpRequest req,
            ILogger log)
        {
            log.LogInformation("C# HTTP trigger function processed a request.");

            string fork_res = JObject.Parse(ForkRepo()).ToString();
            return (ActionResult)new OkObjectResult("Successfully Fork!");
            //string name = req.Query["name"];
            //string requestBody = await new StreamReader(req.Body).ReadToEndAsync();
            //dynamic data = JsonConvert.DeserializeObject(requestBody);
            //name = name ?? data?.name;
            //return name != null
            //    ? (ActionResult)new OkObjectResult($"Hello, {name}")
            //    : new BadRequestObjectResult("Please pass a name on the query string or in the request body");
        }

        private static string ForkRepo()
        {
            string fork_res = JObject.Parse(Post(url_fork, "")).ToString();
            Console.WriteLine("Fork the original reposity.");
            return fork_res;
        }

        private static string Post(string url, string content)
        {
            string result = "";
            HttpWebRequest req = (HttpWebRequest)WebRequest.Create(url);
            req.Method = "POST";
            req.ContentType = "application/vnd.github.v3+json";
            req.Headers.Add("Authorization", "token 4b912f17da8837e14c528b2c491511217cb3a1cb");
            req.UserAgent = "Code Sample Web Client";
            using (var streamWriter = new StreamWriter(req.GetRequestStream()))
            {
                streamWriter.Write(content);
            }

            HttpWebResponse resp = (HttpWebResponse)req.GetResponse();
            Stream stream = resp.GetResponseStream();
            using (StreamReader reader = new StreamReader(stream, Encoding.UTF8))
            {
                result = reader.ReadToEnd();
            }
            return result;
        }
    }
}
