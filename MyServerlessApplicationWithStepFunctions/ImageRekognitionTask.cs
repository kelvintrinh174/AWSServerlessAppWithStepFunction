using Amazon;
using Amazon.DynamoDBv2;
using Amazon.DynamoDBv2.DataModel;
using Amazon.Lambda.Core;
using Amazon.Lambda.S3Events;
using Amazon.Rekognition;
using Amazon.Rekognition.Model;
using Amazon.S3;
using Amazon.S3.Model;
using Amazon.S3.Util;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading.Tasks;

[assembly: LambdaSerializer(typeof(Amazon.Lambda.Serialization.Json.JsonSerializer))]
namespace MyServerlessApplicationWithStepFunctions
{
    public class ImageRekognitionTask
    {
        /// <summary>
        /// The default minimum confidence used for detecting labels.
        /// </summary>
        public const float DEFAULT_MIN_CONFIDENCE = 90f;

        /// <summary>
        /// The name of the environment variable to set which will override the default minimum confidence level.
        /// </summary>
        public const string MIN_CONFIDENCE_ENVIRONMENT_VARIABLE_NAME = "MinConfidence";

        // This const is the name of the environment variable that the serverless.template will use to set
        // the name of the DynamoDB table used to store blog posts.
        const string TABLENAME_ENVIRONMENT_VARIABLE_LOOKUP = "ImageMetadata";

        public const string ID_QUERY_STRING_NAME = "Id";
        IDynamoDBContext DDBContext { get; set; }


        IAmazonS3 S3Client { get; }

        IAmazonRekognition RekognitionClient { get; }

   
        float MinConfidence { get; set; } = DEFAULT_MIN_CONFIDENCE;

        HashSet<string> SupportedImageTypes { get; } = new HashSet<string> { ".png", ".jpg", ".jpeg" };

        /// <summary>
        /// Default constructor used by AWS Lambda to construct the function. Credentials and Region information will
        /// be set by the running Lambda environment.
        /// 
        /// This constuctor will also search for the environment variable overriding the default minimum confidence level
        /// for label detection.
        /// </summary>
        public ImageRekognitionTask()
        {
            this.S3Client = new AmazonS3Client();
            this.RekognitionClient = new AmazonRekognitionClient();

            // Check to see if a table name was passed in through environment variables and if so 
            // add the table mapping.
            //var tableName = System.Environment.GetEnvironmentVariable(TABLENAME_ENVIRONMENT_VARIABLE_LOOKUP);
            var tableName = "ImageMetadata";
            if (!string.IsNullOrEmpty(tableName))
            {
                AWSConfigsDynamoDB.Context.TypeMappings[typeof(ImageMetadata)] = new Amazon.Util.TypeMapping(typeof(ImageMetadata), tableName);
            }

            var config = new DynamoDBContextConfig { Conversion = DynamoDBEntryConversion.V2 };
            this.DDBContext = new DynamoDBContext(new AmazonDynamoDBClient(), config);

            var environmentMinConfidence = System.Environment.GetEnvironmentVariable(MIN_CONFIDENCE_ENVIRONMENT_VARIABLE_NAME);
            if (!string.IsNullOrWhiteSpace(environmentMinConfidence))
            {
                float value;
                if (float.TryParse(environmentMinConfidence, out value))
                {
                    this.MinConfidence = value;
                    Console.WriteLine($"Setting minimum confidence to {this.MinConfidence}");
                }
                else
                {
                    Console.WriteLine($"Failed to parse value {environmentMinConfidence} for minimum confidence. Reverting back to default of {this.MinConfidence}");
                }
            }
            else
            {
                Console.WriteLine($"Using default minimum confidence of {this.MinConfidence}");
            }
        }

        /// <summary>
        /// Constructor used for testing which will pass in the already configured service clients.
        /// </summary>
        /// <param name="s3Client"></param>
        /// <param name="rekognitionClient"></param>
        /// <param name="minConfidence"></param>
        public ImageRekognitionTask(S3Event s3Event,IAmazonS3 s3Client, IAmazonRekognition rekognitionClient, float minConfidence, IAmazonDynamoDB ddbClient, string tableName)
        {
            this.S3Client = s3Client;
            this.RekognitionClient = rekognitionClient;
            this.MinConfidence = minConfidence;
            if (!string.IsNullOrEmpty(tableName))
            {
                AWSConfigsDynamoDB.Context.TypeMappings[typeof(ImageMetadata)] = new Amazon.Util.TypeMapping(typeof(ImageMetadata), tableName);
            }

            var config = new DynamoDBContextConfig { Conversion = DynamoDBEntryConversion.V2 };
            this.DDBContext = new DynamoDBContext(ddbClient, config);
        }

        /// <summary>
        /// A function for responding to S3 create events. It will determine if the object is an image and use Amazon Rekognition
        /// to detect labels and add the labels as tags on the S3 object.
        /// </summary>
        /// <param name="input"></param>
        /// <param name="context"></param>
        /// <returns></returns>
        public async Task<State> ImageRekognition(State state, ILambdaContext context)
        {
            
            string bucketName = state.detail.requestParameters.bucketName;
            string key = state.detail.requestParameters.key;

            Console.WriteLine($"Looking for labels in image {bucketName}:{key}");
            var detectResponses = await this.RekognitionClient.DetectLabelsAsync(new DetectLabelsRequest
            {
                MinConfidence = MinConfidence,
                Image = new Image
                {
                    S3Object = new Amazon.Rekognition.Model.S3Object
                    {
                        Bucket = bucketName,
                        Name = key
                    }
                }
            });

            var tags = new List<Tag>();
            foreach (var label in detectResponses.Labels)
            {
                if (tags.Count < 10)
                {
                    Console.WriteLine($"\tFound Label {label.Name} with confidence {label.Confidence}");
                    tags.Add(new Tag { Key = label.Name, Value = label.Confidence.ToString() });
                }
                else
                {
                    Console.WriteLine($"\tSkipped label {label.Name} with confidence {label.Confidence} because the maximum number of tags has been reached");
                }
            }

            await this.S3Client.PutObjectTaggingAsync(new PutObjectTaggingRequest
            {
                BucketName = bucketName,
                Key = key,
                Tagging = new Tagging
                {
                    TagSet = tags
                }
            });
            
            //save metadata to dynamodb
            ImageMetadata image = new ImageMetadata();
            image.Id = Guid.NewGuid().ToString();
            image.Key = key;
            image.BucketName = bucketName;
            image.Tag = tags;
            context.Logger.LogLine($"Saving blog with id {image.Id}");
            await DDBContext.SaveAsync<ImageMetadata>(image);
            
            return state;
        }
    
        


    }
}
