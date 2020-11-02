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
using System.Threading;

namespace DurableFunctions
{
    public static class FunctionChaining
    {
        [FunctionName("FunctionChaining_HttpStart")]
        public static async Task<IActionResult> Run(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", "post", Route = null)] HttpRequest req,
            [DurableClient(TaskHub = "%MyTaskHub%")] IDurableClient starter,
            ILogger log)
        {
            string requestBody = await new StreamReader(req.Body).ReadToEndAsync();
            string preInstanceId = JsonConvert.DeserializeObject<string>(requestBody);

            string instanceId = await starter.StartNewAsync("ChainPatternExample", preInstanceId);

            log.LogInformation($"Started orchestration with ID = '{instanceId}'.");

            return starter.CreateCheckStatusResponse(req, instanceId);
        }

        [FunctionName("ChainPatternExample")]
        public static async Task<string> RunOrchestrator(
            [OrchestrationTrigger] IDurableOrchestrationContext context, ILogger log)
        {
            log.LogInformation($"************** RunOrchestrator method executing ********************");

            string greeting = await context.CallActivityAsync<string>("ChainPatternExample_SleepActivityFunction", "Prasanth");
            //string greeting = await context.CallActivityAsync<string>("ChainPatternExample_ActivityFunction", "London");
            //string toUpper = await context.CallActivityAsync<string>("ChainPatternExample_ActivityFunction_ToUpper", greeting);
            //string withTimestamp = await context.CallActivityAsync<string>("ChainPatternExample_ActivityFunction_AddTimestamp", toUpper);

            //log.LogInformation(withTimestamp);
            return $"{greeting} - {DateTimeOffset.Now}]";
        }

        [FunctionName("ChainPatternExample_SleepActivityFunction")]
        public static string Sleep([ActivityTrigger] string name, ILogger log)
        {
            Thread.Sleep(60000);
            return $"Hello {name}!";
        }

        //[FunctionName("ChainPatternExample_ActivityFunction")]
        //public static string SayHello([ActivityTrigger] string name, ILogger log)
        //{
        //    return $"Hello {name}!";
        //}

        //[FunctionName("ChainPatternExample_ActivityFunction_ToUpper")]
        //public static string ToUpper([ActivityTrigger] string s, ILogger log)
        //{
        //    return s.ToUpperInvariant();
        //    //throw new InvalidOperationException("Test Exception");
        //}

        //[FunctionName("ChainPatternExample_ActivityFunction_AddTimestamp")]
        //public static string AddTimeStamp([ActivityTrigger] string s, ILogger log)
        //{
        //    return $"{s} [{DateTimeOffset.Now}]";
        //}

    }
}
