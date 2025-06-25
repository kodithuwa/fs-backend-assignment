namespace sf_backend_ass_test.services
{
    using Amazon.S3;
    using Amazon.S3.Util;
    using FileStorage.Models;
    using FileStorage.Services.Contracts;
    using Microsoft.Extensions.Options;
    using System.Threading.Tasks;

    public class S3BucketUtil : IS3BucketUtil
    {
        private readonly string _bucketName;

        public S3BucketUtil(IOptions<AWSModels> awsOptions) 
        {
            _bucketName = awsOptions.Value.BucketName;   
        }

        public async Task<bool> DoesBucketExistAsync(IAmazonS3 s3Client)
        {
           var isExisted = await AmazonS3Util.DoesS3BucketExistV2Async(s3Client, _bucketName);
            return isExisted;
        }

    }
}
