using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using System.Collections.Generic;
using System.Text;
using Microsoft.Azure.WebJobs.Extensions.DurableTask;
using System.Net.Http;
using System.Threading;

namespace DurableFunctions
{
    public static class Fan_out_Fan_in
    {
        [FunctionName("Fan_out_Fan_in_HttpStart")]
        public static async Task<IActionResult> Run(
            [HttpTrigger(AuthorizationLevel.Function, "get", "post", Route = null)] HttpRequest req,
            [DurableClient(TaskHub = "MprTaskHub")] IDurableClient starter,
            ILogger log)
        {
            string requestBody = await new StreamReader(req.Body).ReadToEndAsync();
            GreetingsRequest data = JsonConvert.DeserializeObject<GreetingsRequest>(requestBody);

            string instanceId = await starter.StartNewAsync("FanOutInOrchestrator", data);

            log.LogInformation($"Started orchestration with ID = '{instanceId}'.");

            return starter.CreateCheckStatusResponse(req, instanceId);
        }

        [FunctionName("FanOutInOrchestrator")]
        public static async Task<string> RunOrchestrator(
            [OrchestrationTrigger] IDurableOrchestrationContext context, ILogger log)
        {
            log.LogInformation($"************** RunOrchestrator method executing ********************");

            GreetingsRequest greetingsRequest = context.GetInput<GreetingsRequest>();

            // Fanning out
            log.LogInformation($"************** Fanning out ********************");
            var parallelActivities = new List<Task<string>>();
            foreach (var greeting in greetingsRequest.Greetings)
            {
                // Start a new activity function and capture the task reference
                Task<string> task = context.CallActivityAsync<string>("FanOutIn_ActivityFunction", greeting);

                // Store the task reference for later
                parallelActivities.Add(task);
            }

            // Wait until all the activity functions have done their work
            log.LogInformation($"************** 'Waiting' for parallel results ********************");
            await Task.WhenAll(parallelActivities);
            log.LogInformation($"************** All activity functions complete ********************");

            // Now that all parallel activity functions have completed,
            // fan in AKA aggregate the results, in this case into a single
            // string using a StringBuilder
            log.LogInformation($"************** fanning in ********************");
            var sb = new StringBuilder();
            foreach (var completedParallelActivity in parallelActivities)
            {
                sb.AppendLine(completedParallelActivity.Result);
            }

            return sb.ToString();
        }

        [FunctionName("FanOutIn_ActivityFunction")]
        public static string SayHello([ActivityTrigger] Greeting greeting, ILogger log)
        {
            // simulate longer processing delay to demonstrate parallelism
            Thread.Sleep(60000);

            return $"{greeting.Message} {greeting.CityName}";
        }
    }

    public class Greeting
    {
        public string CityName { get; set; }
        public string Message { get; set; }
    }

    public class GreetingsRequest
    {
        public List<Greeting> Greetings { get; set; }
    }
}
