using Amazon.S3;

namespace FileStorage.Services.Contracts
{
    public interface IS3BucketUtil
    {
        Task<bool> DoesBucketExistAsync(IAmazonS3 s3Client);

    }
}
