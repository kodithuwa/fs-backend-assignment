namespace sf_backend_ass_test.services
{
    using Amazon.DynamoDBv2;
    using Amazon.DynamoDBv2.DocumentModel;
    using Amazon.S3;
    using Amazon.S3.Model;
    using FileStorage.Models;
    using FileStorage.Services.Contracts;
    using Microsoft.Extensions.Options;
    using System.Security.Cryptography;

    public class AwsS3Service : IAwsS3Service
    {
        private readonly IAmazonS3 _s3Client;
        private readonly IAmazonDynamoDB _dynamoClient;
        private readonly string _bucketName;
        private const int _partSize = 5 * 1024 * 1024; // 5MB
        private readonly IDynamoTableService _dynamoTableService;
        private readonly IS3BucketUtil _s3BucketUtil;

        public AwsS3Service(IOptions<AWSModels> awsOptions,IAmazonDynamoDB dynamoClient, IDynamoTableService dynamoTableService, IS3BucketUtil s3BucketUtil)
        {
            var options = awsOptions.Value;
            var awsConfig = new AmazonS3Config
            {
                ServiceURL = options.ServiceURL,
                ForcePathStyle = true
            };

            _s3Client = new AmazonS3Client(options.AccessKey, options.SecretKey, awsConfig);
            _bucketName = options.BucketName;
            _dynamoClient = dynamoClient;
            _dynamoTableService = dynamoTableService;
            _s3BucketUtil = s3BucketUtil;
            _s3BucketUtil.DoesBucketExistAsync(_s3Client).GetAwaiter().GetResult();
        }

        public async Task UploadFileAsync(string key, Stream inputStream)
        {
            var initRequest = new InitiateMultipartUploadRequest
            {
                BucketName = _bucketName,
                Key = key
            };
            var initResponse = await _s3Client.InitiateMultipartUploadAsync(initRequest);
            string uploadId = initResponse.UploadId;

            var partETags = new List<PartETag>();
            int partNumber = 1;

            using var sha256 = SHA256.Create();
            byte[] buffer = new byte[_partSize];
            int bytesRead;

            try
            {
                while ((bytesRead = await inputStream.ReadAsync(buffer, 0, buffer.Length)) > 0)
                {
                    sha256.TransformBlock(buffer, 0, bytesRead, null, 0);

                    using var partStream = new MemoryStream(buffer, 0, bytesRead);
                    var uploadPartRequest = new UploadPartRequest
                    {
                        BucketName = _bucketName,
                        Key = key,
                        UploadId = uploadId,
                        PartNumber = partNumber,
                        InputStream = partStream,
                        PartSize = bytesRead
                    };

                    var uploadPartResponse = await _s3Client.UploadPartAsync(uploadPartRequest);
                    partETags.Add(new PartETag(partNumber, uploadPartResponse.ETag));
                    partNumber++;
                }

                sha256.TransformFinalBlock(Array.Empty<byte>(), 0, 0);
                string sha256Hex = BitConverter.ToString(sha256.Hash).Replace("-", "").ToLowerInvariant();

                var completeRequest = new CompleteMultipartUploadRequest
                {
                    BucketName = _bucketName,
                    Key = key,
                    UploadId = uploadId,
                    PartETags = partETags
                };
                await _s3Client.CompleteMultipartUploadAsync(completeRequest);
                await _dynamoTableService.PutItemAsync(key, sha256Hex, DateTime.UtcNow);
            }
            catch
            {
                await _s3Client.AbortMultipartUploadAsync(new AbortMultipartUploadRequest
                {
                    BucketName = _bucketName,
                    Key = key,
                    UploadId = uploadId
                });
                throw;
            }
        }

        public async Task<Stream> GetFileAsync(string key)
        {
            var response = await _s3Client.GetObjectAsync(_bucketName, key);
            return response.ResponseStream;
        }

        public async Task<List<string>> ListFilesAsync()
        {
            var request = new ListObjectsV2Request
            {
                BucketName = _bucketName
            };

            var response = await _s3Client.ListObjectsV2Async(request);
            return response.S3Objects.Select(o => o.Key).ToList();
        }

    }
}
