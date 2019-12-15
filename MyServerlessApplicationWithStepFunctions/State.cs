using Amazon.Lambda.S3Events;
using Amazon.S3.Model;
using Amazon.S3.Util;
using System;
using System.Collections.Generic;
using System.Text;

namespace MyServerlessApplicationWithStepFunctions
{
    /// <summary>
    /// The state passed between the step function executions.
    /// </summary>
    public class State
    {
       public S3EventDetail detail { get; set; }
       
        /// <summary>
        /// Input value when starting the execution
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// The message built through the step function execution.
        /// </summary>
        public string Message { get; set; }

        /// <summary>
        /// The number of seconds to wait between calling the Salutations task and Greeting task.
        /// </summary>
        public int WaitInSeconds { get; set; }

        //public string Image { get; set; }
    }
}
