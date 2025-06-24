namespace sf_backend_ass_test.services
{
    using Amazon.DynamoDBv2;
    using Amazon.DynamoDBv2.DocumentModel;
    using Amazon.S3;
    using Amazon.S3.Model;
    using Amazon.S3.Util;
    using FileStorage.Services.Contracts;
    using System.Security.Cryptography;

    public class AwsS3Service : IAwsS3Service
    {
        private readonly IAmazonS3 s3Client;
        private readonly IAmazonDynamoDB dynamoClient;
        private readonly string bucketName;
        private readonly string dynamoTableName;
        private const int PartSize = 5 * 1024 * 1024; // 5MB

        public AwsS3Service(IConfiguration configuration, IAmazonDynamoDB dynamoClient)
        {
            var awsConfig = new AmazonS3Config
            {
                ServiceURL = configuration["AWS:ServiceURL"],
                ForcePathStyle = true
            };

            this.s3Client = new AmazonS3Client("test", "test", awsConfig);
            this.bucketName = configuration["AWS:BucketName"] ?? throw new ArgumentNullException(nameof(configuration), "AWS:BucketName is not configured.");
            this.dynamoClient = dynamoClient;
            this.dynamoTableName = "Files";
            EnsureBucketExistsAsync().Wait();
        }

        private async Task EnsureBucketExistsAsync()
        {
            // Use AmazonS3Util to check if the bucket exists
            var exists = await AmazonS3Util.DoesS3BucketExistV2Async(this.s3Client, bucketName);
            if (!exists)
            {
                await this.s3Client.PutBucketAsync(new PutBucketRequest
                {
                    BucketName = bucketName
                });
            }
        }

        public async Task UploadFileAsync(string key, Stream inputStream)
        {
            var initRequest = new InitiateMultipartUploadRequest
            {
                BucketName = bucketName,
                Key = key
            };
            var initResponse = await this.s3Client.InitiateMultipartUploadAsync(initRequest);
            string uploadId = initResponse.UploadId;

            var partETags = new List<PartETag>();
            int partNumber = 1;

            using var sha256 = SHA256.Create();
            byte[] buffer = new byte[PartSize];
            int bytesRead;

            try
            {
                while ((bytesRead = await inputStream.ReadAsync(buffer, 0, buffer.Length)) > 0)
                {
                    sha256.TransformBlock(buffer, 0, bytesRead, null, 0);

                    using var partStream = new MemoryStream(buffer, 0, bytesRead);
                    var uploadPartRequest = new UploadPartRequest
                    {
                        BucketName = bucketName,
                        Key = key,
                        UploadId = uploadId,
                        PartNumber = partNumber,
                        InputStream = partStream,
                        PartSize = bytesRead
                    };

                    var uploadPartResponse = await this.s3Client.UploadPartAsync(uploadPartRequest);
                    partETags.Add(new PartETag(partNumber, uploadPartResponse.ETag));
                    partNumber++;
                }

                sha256.TransformFinalBlock(Array.Empty<byte>(), 0, 0);
                string sha256Hex = BitConverter.ToString(sha256.Hash).Replace("-", "").ToLowerInvariant();

                var completeRequest = new CompleteMultipartUploadRequest
                {
                    BucketName = bucketName,
                    Key = key,
                    UploadId = uploadId,
                    PartETags = partETags
                };
                await this.s3Client.CompleteMultipartUploadAsync(completeRequest);

                var table = Table.LoadTable(dynamoClient, dynamoTableName);
                var doc = new Document
                {
                    ["Filename"] = key,
                    ["UploadedAt"] = DateTime.UtcNow.ToString("o")
                };
                await table.PutItemAsync(doc);
            }
            catch
            {
                await this.s3Client.AbortMultipartUploadAsync(new AbortMultipartUploadRequest
                {
                    BucketName = bucketName,
                    Key = key,
                    UploadId = uploadId
                });
                throw;
            }
        }

        public async Task<Stream> GetFileAsync(string key)
        {
            var response = await this.s3Client.GetObjectAsync(bucketName, key);
            return response.ResponseStream;
        }

        public async Task<List<string>> ListFilesAsync()
        {
            var request = new ListObjectsV2Request
            {
                BucketName = bucketName
            };

            var response = await this.s3Client.ListObjectsV2Async(request);
            return response.S3Objects.Select(o => o.Key).ToList();
        }
    }
}
