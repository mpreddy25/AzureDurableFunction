using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Microsoft.Azure.WebJobs.Extensions.DurableTask;
using System.Collections.Generic;
using System.Threading;
using System.Text;

namespace DurableFunctions
{
    public static class Fan_out_Fan_in_Parallelism
    {
        [FunctionName("Fan_out_Fan_in_Parallelism")]
        public static async Task<IActionResult> Run(
            [HttpTrigger(AuthorizationLevel.Function, "get", "post", Route = null)] HttpRequest req,
            [DurableClient] IDurableClient starter,
            ILogger log)
        {
            //string requestBody = await new StreamReader(req.Body).ReadToEndAsync();
            //GreetingsRequest data = JsonConvert.DeserializeObject<GreetingsRequest>(requestBody);

            string instanceId = await starter.StartNewAsync("FanOutInParallelismOrchestrator", null);

            log.LogInformation($"Started orchestration with ID = '{instanceId}'.");

            return starter.CreateCheckStatusResponse(req, instanceId);
        }


        [FunctionName("FanOutInParallelismOrchestrator")]
        public static async Task<string> FanOutInParallelismOrchestrator(
            [OrchestrationTrigger] IDurableOrchestrationContext context, ILogger log)
        {
            var parallelActivities = new HashSet<Task<string>>();
            var sb = new StringBuilder();
            for (int i= 0; i<10; i++)
            {
                if (parallelActivities.Count > 4)
                {
                    Task<string> finished = await Task.WhenAny(parallelActivities);
                    sb.AppendLine(finished.Result);
                    parallelActivities.Remove(finished);
                }

                Task<string> task = context.CallActivityAsync<string>("FanOutIn_SayHelloActivity", i.ToString());
                parallelActivities.Add(task);
            }

            await Task.WhenAll(parallelActivities);

            
            foreach (var completedParallelActivity in parallelActivities)
            {
                sb.AppendLine(completedParallelActivity.Result);
            }

            return sb.ToString();
        }

        [FunctionName("FanOutIn_SayHelloActivity")]
        public static string SayHelloActivity([ActivityTrigger] string i, ILogger log)
        {
            // simulate longer processing delay to demonstrate parallelism
            Thread.Sleep(10000);

            return $"{i}";
        }
    }
}
