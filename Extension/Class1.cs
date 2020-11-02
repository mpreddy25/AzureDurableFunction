using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.DurableTask;
using Microsoft.Extensions.Logging;
using System;

namespace Extension
{
    public static class ActivityFunctions
    {
        [FunctionName("ChainPatternExample_ActivityFunction2")]
        public static string SayHello([ActivityTrigger] string name, ILogger log)
        {
            return $"Hello {name}!";
        }
    }
}
