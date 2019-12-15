using Amazon.Rekognition.Model;
using Amazon.S3.Model;
using System;
using System.Collections.Generic;
using System.Text;

namespace MyServerlessApplicationWithStepFunctions
{
    public class ImageMetadata
    {
        public string Id { get; set; }
        public  string BucketName { get; set; }
        public string Key { get; set; }

        public List<Tag> Tag { get; set; }
        public ImageMetadata()
        {

        }
        //private string 

    }
}
