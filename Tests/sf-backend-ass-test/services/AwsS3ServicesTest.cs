namespace sf_backend_ass_test.services
{
    using Amazon.DynamoDBv2;
    using Amazon.S3;
    using Amazon.S3.Model;
    using FileStorage.Models;
    using FileStorage.Services.Contracts;
    using Microsoft.Extensions.Options;
    using Moq;
    using System;
    using System.IO;
    using System.Text;
    using System.Threading;
    using System.Threading.Tasks;
    using Xunit;

    public class AwsS3ServiceTests
    {
        [Fact]
        public async Task UploadFileAsync_UploadsFileAndUpdatesDynamo()
        {
            // Arrange
            var s3Mock = new Mock<IAmazonS3>();
            var amazonDb = new Mock<IAmazonDynamoDB>();
            var dynamoTableServiceMock = new Mock<IDynamoTableService>();
            var is3BucketUtilMock = new Mock<IS3BucketUtil>();
            var options = Options.Create(new AWSModels
            {
                AccessKey = "test",
                SecretKey = "test",
                BucketName = "bucket",
                ServiceURL = "http://localhost"
            });

            s3Mock.Setup(s => s.InitiateMultipartUploadAsync(It.IsAny<InitiateMultipartUploadRequest>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new InitiateMultipartUploadResponse { UploadId = "upload-id" });

            s3Mock.Setup(s => s.UploadPartAsync(It.IsAny<UploadPartRequest>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new UploadPartResponse { ETag = "etag" });

            s3Mock.Setup(s => s.CompleteMultipartUploadAsync(It.IsAny<CompleteMultipartUploadRequest>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new CompleteMultipartUploadResponse());

            s3Mock.Setup(s => s.AbortMultipartUploadAsync(It.IsAny<AbortMultipartUploadRequest>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new AbortMultipartUploadResponse());

            dynamoTableServiceMock.Setup(d => d.PutItemAsync(It.IsAny<string>(), It.IsAny<DateTime>()))
                .Returns(Task.CompletedTask);

            var service = new AwsS3Service(options, amazonDb.Object, dynamoTableServiceMock.Object, is3BucketUtilMock.Object);
            typeof(AwsS3Service)
                .GetField("_s3Client", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
                .SetValue(service, s3Mock.Object);

            // Use a stream with data less than _partSize to keep it simple
            var stream = new MemoryStream(new byte[1024]);

            // Act
            await service.UploadFileAsync("test-key", stream);

            // Assert
            s3Mock.Verify(s => s.InitiateMultipartUploadAsync(It.IsAny<InitiateMultipartUploadRequest>(), It.IsAny<CancellationToken>()), Times.Once);
            s3Mock.Verify(s => s.UploadPartAsync(It.IsAny<UploadPartRequest>(), It.IsAny<CancellationToken>()), Times.AtLeastOnce);
            s3Mock.Verify(s => s.CompleteMultipartUploadAsync(It.IsAny<CompleteMultipartUploadRequest>(), It.IsAny<CancellationToken>()), Times.Once);
            dynamoTableServiceMock.Verify(d => d.PutItemAsync("test-key", It.IsAny<DateTime>()), Times.Once);
        }

        [Fact]
        public async Task GetFileAsync_ReturnsStreamFromS3()
        {
            // Arrange
            var s3Mock = new Mock<IAmazonS3>();
            var dynamoDbMock = new Mock<IAmazonDynamoDB>();
            var dynamoTableServiceMock = new Mock<IDynamoTableService>();
            var s3BucketUtilMock = new Mock<IS3BucketUtil>();
            var options = Options.Create(new AWSModels
            {
                AccessKey = "test",
                SecretKey = "test",
                BucketName = "bucket",
                ServiceURL = "http://localhost"
            });

            var expectedContent = "test file content";
            var memoryStream = new MemoryStream(Encoding.UTF8.GetBytes(expectedContent));

            s3Mock.Setup(s => s.GetObjectAsync(
                    It.IsAny<string>(),
                    It.IsAny<string>(),
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(new GetObjectResponse
                {
                    ResponseStream = memoryStream
                });

            var service = new AwsS3Service(options, dynamoDbMock.Object, dynamoTableServiceMock.Object, s3BucketUtilMock.Object);
            typeof(AwsS3Service)
                .GetField("_s3Client", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
                .SetValue(service, s3Mock.Object);

            // Act
            var resultStream = await service.GetFileAsync("test-key");

            // Assert
            Assert.NotNull(resultStream);
            using var reader = new StreamReader(resultStream);
            var content = reader.ReadToEnd();
            Assert.Equal(expectedContent, content);
        }

        [Fact]
        public async Task ListFilesAsync_ReturnsFileKeys()
        {
            // Arrange
            var s3Mock = new Mock<IAmazonS3>();
            var dynamoDbMock = new Mock<IAmazonDynamoDB>();
            var dynamoTableServiceMock = new Mock<IDynamoTableService>();
            var s3BucketUtilMock = new Mock<IS3BucketUtil>();
            var options = Options.Create(new AWSModels
            {
                AccessKey = "test",
                SecretKey = "test",
                BucketName = "bucket",
                ServiceURL = "http://localhost"
            });

            var s3Objects = new List<S3Object>
            {
                new S3Object { Key = "file1.txt" },
                new S3Object { Key = "file2.txt" }
            };

            s3Mock.Setup(s => s.ListObjectsV2Async(
                    It.IsAny<ListObjectsV2Request>(),
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(new ListObjectsV2Response
                {
                    S3Objects = s3Objects
                });

            var service = new AwsS3Service(options, dynamoDbMock.Object, dynamoTableServiceMock.Object, s3BucketUtilMock.Object);
            typeof(AwsS3Service)
                .GetField("_s3Client", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
                .SetValue(service, s3Mock.Object);

            // Act
            var result = await service.ListFilesAsync();

            // Assert
            Assert.NotNull(result);
            Assert.Equal(2, result.Count);
            Assert.Contains("file1.txt", result);
            Assert.Contains("file2.txt", result);
        }
    }
}
