using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using System.Text.RegularExpressions;
using System.Diagnostics;
using System.Text;
using Newtonsoft.Json.Linq;
using FunctionAppTest.JsonEntities;
using System.Collections.Generic;
using System.Net;

namespace FunctionAppTest
{


    public static class AutoContribute
    {
        private static string path = Path.GetDirectoryName(Path.Combine(System.IO.Directory.GetCurrentDirectory(), @"..\\..\\..\\middlefile"));
        private static string sourceFile = path + @"/origin.md";

        private static string url_fork = @"https://api.github.com/repos/Peachying/testinlineedit/forks";
        private static string url_getRef = @"https://api.github.com/repos/GraceXu96/testinlineedit/git/refs/heads/master";
        private static string url_createBlob = @"https://api.github.com/repos/GraceXu96/testinlineedit/git/blobs";
        private static string url_createTree = @"https://api.github.com/repos/GraceXu96/testinlineedit/git/trees";
        private static string url_getCommit = @"https://api.github.com/repos/GraceXu96/testinlineedit/git/commits/";
        private static string url_createCommit = @"https://api.github.com/repos/GraceXu96/testinlineedit/git/commits";
        private static string url_updateRef = @"https://api.github.com/repos/GraceXu96/testinlineedit/git/refs/heads/master";
        private static string url_pullRequest = @"https://api.github.com/repos/Peachying/testinlineedit/pulls";


        [FunctionName("AutoContribute")]
        public static async Task<IActionResult> Run(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", "post", Route = null)] HttpRequest req,
            ILogger log)
        {
            log.LogInformation("C# HTTP trigger function processed a request.");

            string name = req.Query["name"];

            string requestBody = await new StreamReader(req.Body).ReadToEndAsync();
            dynamic data = JsonConvert.DeserializeObject(requestBody);
            name = name ?? data?.name;
            Console.WriteLine(path);
            batchPandoc();
            modifyMdfile();
            Commit();
            PullRequest();
            
            return name != null
                ? (ActionResult)new OkObjectResult($"{name} pull request to edit the docs.")
                : new BadRequestObjectResult("Please pass a name on the query string or in the request body");
        }

        public static void batchPandoc()
        {
            string head = @"pandoc -f html -t markdown ";
            string tail = @" -o " + path + @"\frag_";
            string[] filenames = Directory.GetFiles(path, "*.html");
            foreach (string file in filenames)
            {
                removeHtmlTags(file);
                string exec = head + file + tail + file.Substring(file.IndexOf("_") + 1, 1) + ".md\n";
                RunCmd(exec);
                Console.WriteLine("Convert all fragile html files to md");
            }
        }
        public static void removeHtmlTags(string file)
        {
            StreamReader sr = new StreamReader(file);
            string content = sr.ReadToEnd();
            sr.Close();
            //content = content.Replace("<del[^>]*?>[\\s\\S]*?<\\/del>", "");
            //content = content.Replace("<\\/?ins.*?>", "");
            string regex_del = @"<del[^>]*?>.*?</del>";
            string regex_ins = @"<\/?ins.*?>";
            content = Regex.Replace(content, regex_del, string.Empty, RegexOptions.IgnoreCase);
            content = Regex.Replace(content, regex_ins, string.Empty, RegexOptions.IgnoreCase);
            StreamWriter sw = new StreamWriter(file);
            sw.WriteLine(content);
            sw.Close();
        }
        private static void RunCmd(string cmd)
        {
            cmd = cmd.Trim().TrimEnd('&') + " & exit";
            Process p = new Process();
            p.StartInfo.FileName = "cmd.exe";
            p.StartInfo.UseShellExecute = false;
            p.StartInfo.RedirectStandardInput = true;
            p.StartInfo.RedirectStandardOutput = true;
            p.StartInfo.RedirectStandardError = true;
            p.StartInfo.CreateNoWindow = true;
            p.Start();

            p.StandardInput.WriteLine(cmd);
            p.StandardInput.AutoFlush = true;
            string output = p.StandardOutput.ReadToEnd();
            Console.WriteLine(output);
            p.WaitForExit();
            p.Close();
        }

        public static void modifyMdfile()
        {
            StreamReader sr = new StreamReader(path + @"\editinfo.txt");
            string infoStr = sr.ReadToEnd();
            var infoArray = JsonConvert.DeserializeObject<FragInfo[]>(infoStr);
            int fileNum = 0;
            foreach (FragInfo info in infoArray)
            {
                string fragFile = path + @"\frag_" + fileNum + @".md";

                EditFile(info.StartLine, info.Endline, sourceFile, fragFile);
                fileNum += 1;
                Console.WriteLine();
            }
            Console.WriteLine("Replace modified parts in the original md file");
        }

        private static void EditFile(int startLine, int endLine, string sourcePath, string newPath)
        {
            StreamReader sr_new = new StreamReader(newPath);
            string newLines = sr_new.ReadToEnd().Replace("\r\n", " ");

            FileStream fs = new FileStream(sourcePath, FileMode.Open, FileAccess.Read);
            StreamReader sr = new StreamReader(fs, Encoding.GetEncoding("utf-8"));
            string line = sr.ReadLine();
            string text = "";

            for (int i = 1; line != null; i++)
            {
                if (i != startLine)
                    text += line + "\r\n";
                else

                    text += newLines + "\r\n";
                line = sr.ReadLine();
            }
            sr.Close();
            FileStream fs1 = new FileStream(sourcePath, FileMode.Open, FileAccess.Write);
            StreamWriter sw = new StreamWriter(fs1, Encoding.GetEncoding("utf-8"));
            sw.Write(text);
            sw.Close();
            sr_new.Close();
            fs1.Close();
        }

        private static void ForkRepo()
        {
            string fork_res = JObject.Parse(Post(url_fork, "")).ToString();
            Console.WriteLine("Fork the original reposity.");
        }

        private static void PullRequest()
        {
            CreatePullRequest pullRequestBody = new CreatePullRequest
            {
                Title = "test PR with Github API",
                Head = "GraceXu96:master",
                Base = "master",
                Body = "Please pull this in!"
            };
            string reqBody = JsonConvert.SerializeObject(pullRequestBody);
            string pr_res = JObject.Parse(Post(url_pullRequest, reqBody)).ToString();
            Console.WriteLine(pr_res);
        }

        private static void Commit()
        {
            Console.WriteLine("******************Six steps for Commit***************************");
            //get reference & tree to commit 
            string parent_sha = JObject.Parse(Get(url_getRef, new Dictionary<string, string>()))["object"]["sha"].ToString();
            string baseTree_sha = JObject.Parse(Get(url_getCommit + parent_sha, new Dictionary<string, string>()))["tree"]["sha"].ToString();

            //create a  blob
            StreamReader sr = new StreamReader(sourceFile, Encoding.GetEncoding("utf-8"));
            CreateBlobRequest createBlobRequest = new CreateBlobRequest
            {
                Content = sr.ReadToEnd(),
                Encoding = "utf-8"
            };
            string createBlobBody = JsonConvert.SerializeObject(createBlobRequest);
            string blob_sha = JObject.Parse(Post(url_createBlob, createBlobBody))["sha"].ToString();

            //create a new tree for commit
            CreateTreeRequest createTreeRequest = new CreateTreeRequest
            {
                BaseTree = baseTree_sha,
                Tree = new TreeNode[] {
                    new TreeNode{
                        Path = @"node-azure-tools.md",
                        Mode = "100644",
                        Type = "blob",
                        Sha = blob_sha
                    }
                }
            };
            string createTreeBody = JsonConvert.SerializeObject(createTreeRequest);
            string treeSubmit_sha = JObject.Parse(Post(url_createTree, createTreeBody))["sha"].ToString();

            //create a  new commit
            CreateCommitRequest createCommitRequest = new CreateCommitRequest
            {
                Message = "Commit automatically!",
                Parents = new string[] { parent_sha },
                Tree = treeSubmit_sha
            };
            string createCommitBody = JsonConvert.SerializeObject(createCommitRequest);
            string createSubmit_sha = JObject.Parse(Post(url_createCommit, createCommitBody))["sha"].ToString();

            //update reference
            UpdateReferenceRequest updateReferenceRequest = new UpdateReferenceRequest
            {
                Sha = createSubmit_sha,
                Force = true
            };
            string updateReferenceBody = JsonConvert.SerializeObject(updateReferenceRequest);
            string updateRef_res = Post(url_updateRef, updateReferenceBody).ToString();
        }

        private static string Post(string url, string content)
        {
            string result = "";
            HttpWebRequest req = (HttpWebRequest)WebRequest.Create(url);
            req.Method = "POST";
            req.ContentType = "application/vnd.github.v3+json";
            req.Headers.Add("Authorization", "token **");
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



        private static string Get(string url, Dictionary<string, string> dic)
        {
            string result = "";
            StringBuilder builder = new StringBuilder();
            builder.Append(url);

            if (dic.Count > 0)
            {
                builder.Append("?");
                int i = 0;
                foreach (var item in dic)
                {
                    if (i > 0)
                        builder.Append("&");
                    builder.AppendFormat("{0}={1}", item.Key, item.Value);
                    i++;
                }
            }

            HttpWebRequest req = (HttpWebRequest)WebRequest.Create(builder.ToString());
            req.ContentType = "application/vnd.github.v3+json";
            req.Headers.Add("Authorization", "token **");
            req.UserAgent = "Code Sample Web Client";
            HttpWebResponse resp = (HttpWebResponse)req.GetResponse();

            Stream stream = resp.GetResponseStream();
            try
            {
                using (StreamReader reader = new StreamReader(stream))
                {
                    result = reader.ReadToEnd();
                }
            }
            finally
            {
                stream.Close();
            }
            return result;
        }
    }

}
