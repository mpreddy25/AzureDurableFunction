using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using System.Threading;
using Microsoft.Azure.WebJobs.Extensions.DurableTask;

namespace DurableFunctions
{
    public static class MonitorPattern
    {
        [FunctionName("MonitorPattern")]
        public static async Task<IActionResult> Run(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", "post", Route = null)] HttpRequest req,
            [DurableClient] IDurableClient starter,
            ILogger log)
        {
            string requestBody = await new StreamReader(req.Body).ReadToEndAsync();
            dynamic data = JsonConvert.DeserializeObject<dynamic>(requestBody);
            
            string fileName = data.FileName;

            string instanceId = await starter.StartNewAsync("MonitorPatternExample", fileName);

            return starter.CreateCheckStatusResponse(req, instanceId);
        }

        [FunctionName("MonitorPatternExample")]
        public static async Task RunOrchestrator(
            [OrchestrationTrigger] IDurableOrchestrationContext context, ILogger log)
        {
            string fileName = context.GetInput<string>();

            // start encoding
            await context.CallActivityAsync<string>("MonitorPatternExample_BeginEncode", fileName);


            // We don't want the orchestration to run infinitely
            // If the operation has not completed within 30 mins, end the orchestration
            var operationTimeoutTime = context.CurrentUtcDateTime.AddMinutes(3);

            while (true)
            {
                var operationHasTimedOut = context.CurrentUtcDateTime > operationTimeoutTime;

                if (operationHasTimedOut)
                {
                    context.SetCustomStatus("Encoding has timed out, please submit the job again.");
                    break;
                }

                var isEncodingComplete = await context.CallActivityAsync<bool>("MonitorPatternExample_IsEncodingComplete", fileName);

                if (isEncodingComplete)
                {
                    context.SetCustomStatus("Encoding has completed successfully.");
                    break;
                }

                // If no timeout and encoding still being processed we want to put the orchestration to sleep,
                // and awaking it again after a specified interval
                var nextCheckTime = context.CurrentUtcDateTime.AddSeconds(15);
                log.LogInformation($"************** Sleeping orchestration until {nextCheckTime.ToLongTimeString()}");
                await context.CreateTimer(nextCheckTime, CancellationToken.None);
            }
        }

        [FunctionName("MonitorPatternExample_BeginEncode")]
        public static void BeginEncodeVideo([ActivityTrigger] string fileName, ILogger log)
        {
            // Call API, start an async process, queue a message, etc.
            log.LogInformation($"************** Starting encoding of {fileName}");

            // This activity returns before the job is complete, its job is to just start the async/long running operation
        }


        [FunctionName("MonitorPatternExample_IsEncodingComplete")]
        public static bool IsEncodingComplete([ActivityTrigger] string fileName, ILogger log)
        {
            log.LogInformation($"************** Checking if {fileName} encoding is complete...");
            // Here you would make a call to an API, query a database, check blob storage etc 
            // to check whether the long running asyn process is complete

            // For demo purposes, we'll just signal completion every so often
            bool isComplete = new Random().Next() % 2 == 0;

            log.LogInformation($"************** {fileName} encoding complete: {isComplete}");

            return isComplete;
        }
    }
}
