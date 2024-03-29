using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading.Tasks;

using Amazon.Lambda.Core;


// Assembly attribute to enable the Lambda function's JSON input to be converted into a .NET class.
//[assembly: LambdaSerializer(typeof(Amazon.Lambda.Serialization.Json.JsonSerializer))]

namespace MyServerlessApplicationWithStepFunctions
{
    public class StepFunctionTasks
    {
        /// <summary>
        /// Default constructor that Lambda will invoke.
        /// </summary>
        public StepFunctionTasks()
        {
        }

        public State Validation(State state, ILambdaContext context)
        {
            state.Message += "Starting Validation";
            /*
            if (!string.IsNullOrEmpty(state.Name))
            {
                state.Message += " " + state.Name;
            }*/

            return state;
        }
    }
}
