
namespace FileStorage.Controllers
{
    using FileStorage.Services.Contracts;
    using Microsoft.AspNetCore.Mvc;

    [ApiController]
    [Route("[controller]")]
    public class FileStorageController : ControllerBase
    {
        private readonly ILogger<FileStorageController> logger;
        private readonly IAwsS3Service s3Service;

        public FileStorageController(ILogger<FileStorageController> logger, IAwsS3Service s3Service)
        {
            this.logger = logger;
            this.s3Service = s3Service;
        }

        [RequestSizeLimit(2L * 1024 * 1024 * 1024)] // 2GB
        [HttpPost("upload")]
        public async Task<IActionResult> Upload([FromForm] IFormFile file)
        {
            if (file == null || file.Length == 0)
            {
                return BadRequest("No file provided.");
            }

            if(file.Length < 128 * 1024) // 128KB minimum size
            {
                return BadRequest("File size must be at least 128KB.");
            }

            if(file.Length > 2L*1024*1024*1024) // 2GB maximum size   
            {
                return BadRequest("File size exceeds the limit of 2GB.");
            }

            using var stream = file.OpenReadStream();
            await this.s3Service.UploadFileAsync(file.FileName, stream);
            return Ok("File uploaded.");
        }

        [HttpGet("list")]
        public async Task<IActionResult> List()
        {
            var files = await this.s3Service.ListFilesAsync();
            return Ok(files);
        }
    }
}