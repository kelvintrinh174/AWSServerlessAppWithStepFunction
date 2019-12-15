using Amazon.Lambda.Core;
using Amazon.S3;
using GrapeCity.Documents.Imaging;
using GrapeCity.Documents.Text;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Text;
using System.IO;
using GrapeCity.Documents.Drawing;
using Amazon.S3.Model;
using System.Threading.Tasks;

namespace MyServerlessApplicationWithStepFunctions
{
    public class GenerateThumbnailTask
    {
        IAmazonS3 S3Client { get; set; }

        public GenerateThumbnailTask()
        {
            S3Client = new AmazonS3Client();
        }

        public GenerateThumbnailTask(IAmazonS3 s3Client)
        {
            this.S3Client = s3Client;
        }

        public string GetConvertedImage(byte[] stream)
        {
            using (var bmp = new GcBitmap())
            {
                
                bmp.Load(stream);
                               
                //  Convert to grayscale 
                //bmp.ApplyEffect(GrayscaleEffect.Get(GrayscaleStandard.BT601));
                //  Resize to thumbnail 
                var resizedImage = bmp.Resize(100, 100, InterpolationMode.NearestNeighbor);
                return GetBase64(resizedImage);
            }
        }

        private string GetBase64(GcBitmap bmp)
        {
            using (MemoryStream m = new MemoryStream())
            {
                bmp.SaveAsJpeg(m);
                return Convert.ToBase64String(m.ToArray());
            }
        }

        public async Task<State> GenerateThumbnail(State state, ILambdaContext context)
        {
            state.Message += state.detail.requestParameters.bucketName + " " + state.detail.requestParameters.key + " , Goodbye";
            string bucketName = state.detail.requestParameters.bucketName;
            string key = state.detail.requestParameters.key;

            if (!string.IsNullOrEmpty(state.Name))
            {
                state.Message += " " + state.Name;
            }

            //return state;

            try
            {
                var rs = await this.S3Client.GetObjectMetadataAsync(
                    bucketName,
                    key);

                if (rs.Headers.ContentType.StartsWith("image/"))
                {
                    using (GetObjectResponse response = await S3Client.GetObjectAsync(bucketName,key))
                    {
                        using (Stream responseStream = response.ResponseStream)
                        {
                            using (StreamReader reader = new StreamReader(responseStream))
                            {
                                using (var memstream = new MemoryStream())
                                {
                                    var buffer = new byte[512];
                                    var bytesRead = default(int);
                                    while ((bytesRead = reader.BaseStream.Read(buffer, 0, buffer.Length)) > 0)
                                        memstream.Write(buffer, 0, bytesRead);
                                    
                                    // Perform image manipulation 
                                    var transformedImage = GetConvertedImage(memstream.ToArray());
                                    byte[] bytes = Convert.FromBase64String(transformedImage);


                                    PutObjectRequest putRequest = new PutObjectRequest()
                                    {
                                        BucketName = "imageprocessingthumbnail",
                                        Key = $"thumbnail-{key}",
                                        CannedACL = S3CannedACL.PublicRead
                                    };

                                    
                                    using (var ms = new MemoryStream(bytes))
                                    {
                                        putRequest.InputStream = ms;
                                        await S3Client.PutObjectAsync(putRequest);
                                    }
                                    
                                }
                            }
                        }
                    }
                }
                return state;
            }
            catch (Exception)
            {
                throw;
            }


   
        }


    }


   
 }

