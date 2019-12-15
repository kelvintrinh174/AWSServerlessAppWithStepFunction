# AWSServerlessAppWithStepFunction
### Summary
1.	The Serverless application include a Lambda function to use AWS Rekognition service to detect label and add all those labels with confidence greater than 90 to image metadata 
which is stored in a DynamoDB table
2.	The second lambda function is to make a thumbnail for the image which should be stored in S3. 

### Technologies:
- .NET SDK for AWS
- Amazon Web Services:
      - Rekognition
      - Simple Storage Service
      - DynamoDB
      - Lambda Serverless Application(Deployment)
      - StepFunctions

### Guide:
![ezgif com-gif-maker](https://user-images.githubusercontent.com/39202933/70865351-99582680-1f2a-11ea-9a3f-e59f2bcae7b9.gif)

