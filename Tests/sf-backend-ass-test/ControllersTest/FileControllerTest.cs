namespace sf_backend_ass_test.services
{

    using System.IO;
    using System.Threading.Tasks;
    using FileStorage.Controllers;
    using FileStorage.Services.Contracts;
    using Microsoft.AspNetCore.Http;
    using Microsoft.AspNetCore.Mvc;
    using Microsoft.Extensions.Logging;
    using Moq;
    using Xunit;

    public class FileControllerTest
    {
        Mock<ILogger<FileStorageController>> _logger;
        Mock<IAwsS3Service> _awsS3Service;
        FileStorageController _controller;

        public FileControllerTest()
        {
            _logger = new Mock<ILogger<FileStorageController>>();
            _awsS3Service = new Mock<IAwsS3Service>();
            _controller = new FileStorageController(_logger.Object, _awsS3Service.Object);
        }


        [Fact]
        public async Task Upload_ReturnsBadRequest_WhenFileIsNull()
        {
            // Arrange

            // Act
            var result = await _controller.Upload(null);

            // Assert
            var badRequest = Assert.IsType<BadRequestObjectResult>(result);
            Assert.Equal("No file provided.", badRequest.Value);
        }

        [Fact]
        public async Task Upload_ReturnsBadRequest_WhenFileIsTooSmall()
        {
            // Arrange
            var s3ServiceMock = new Mock<IAwsS3Service>();
            var fileMock = new Mock<IFormFile>();
            fileMock.Setup(f => f.Length).Returns(100 * 1024); // 100KB
            var controller = new FileStorageController(_logger.Object, s3ServiceMock.Object);

            // Act
            var result = await controller.Upload(fileMock.Object);

            // Assert
            var badRequest = Assert.IsType<BadRequestObjectResult>(result);
            Assert.Equal("File size must be at least 128KB.", badRequest.Value);
        }

        [Fact]
        public async Task Upload_ReturnsBadRequest_WhenFileIsTooLarge()
        {
            // Arrange
            var fileMock = new Mock<IFormFile>();
            fileMock.Setup(f => f.Length).Returns((long)2 * 1024 * 1024 * 1024 + 1); // >2GB
            var controller = new FileStorageController(_logger.Object, _awsS3Service.Object);

            // Act
            var result = await controller.Upload(fileMock.Object);

            // Assert
            var badRequest = Assert.IsType<BadRequestObjectResult>(result);
            Assert.Equal("File size exceeds the limit of 2GB.", badRequest.Value);
        }

        [Fact]
        public async Task Upload_ReturnsOk_WhenFileIsValid()
        {
            // Arrange
            var s3ServiceMock = new Mock<IAwsS3Service>();
            var fileMock = new Mock<IFormFile>();
            fileMock.Setup(f => f.Length).Returns(200 * 1024); // 200KB
            fileMock.Setup(f => f.FileName).Returns("test.txt");
            var stream = new MemoryStream();
            fileMock.Setup(f => f.OpenReadStream()).Returns(stream);

            s3ServiceMock.Setup(s => s.UploadFileAsync("test.txt", stream)).Returns(Task.CompletedTask);

            var controller = new FileStorageController(_logger.Object, s3ServiceMock.Object);

            // Act
            var result = await controller.Upload(fileMock.Object);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            Assert.Equal("File uploaded.", okResult.Value);
            s3ServiceMock.Verify(s => s.UploadFileAsync("test.txt", stream), Times.Once);
        }
    }
}
