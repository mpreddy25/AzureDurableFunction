using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.DurableTask;
using Microsoft.Azure.WebJobs.Host;
using Microsoft.Extensions.Logging;

namespace DurableFunctions
{
    public static class EventSourcing
    {
        //[FunctionName("EventSourcing")]
        //public static async Task RunAsync([TimerTrigger("*/30 * * * * *")]TimerInfo myTimer,
        //    [DurableClient] IDurableClient starter,
        //    ILogger log)
        //{
        //    //log.LogInformation($"C# Timer trigger function executed at: {DateTime.Now}");
        //    string instanceId = await starter.StartNewAsync("EventSourcingExample", null);
        //    //starter.WaitForCompletionOrCreateCheckStatusResponseAsync()

        //    log.LogInformation($"Started orchestration with ID = '{instanceId}'.");

        //    //return starter.CreateCheckStatusResponse(null, instanceId);
        //}

        [FunctionName("EventSourcingExample")]
        public static async Task RunOrchestrator(
            [OrchestrationTrigger] IDurableOrchestrationContext context, ILogger log)
        {
            log.LogInformation($"************** RunOrchestrator method executing ********************");

            string message1 = await context.CallActivityAsync<string>("EventSourcing_ActivityFunction", "Method-1");
            string message2 = await context.CallActivityAsync<string>("EventSourcing_ActivityFunction", "Method-2");
            string message3 = await context.CallActivityAsync<string>("EventSourcing_ActivityFunction", "Method-3");

            log.LogInformation("************** Completed one cycle...************** \n");
        }

        [FunctionName("EventSourcing_ActivityFunction")]
        public static string SayHello([ActivityTrigger] string name, ILogger log)
        {
            Thread.Sleep(30000);
            log.LogInformation("************** "+name + " called ************** \n");
            return $"Hello {name}!";
        }
    }
}
