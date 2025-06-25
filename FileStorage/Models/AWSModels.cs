namespace FileStorage.Models
{
    public class AWSModels
    {
        public string Profile { get; set; }
        public string AuthenticationRegion { get; set; }
        public string Region { get; set; }
        public string ServiceURL { get; set; }
        public string MaxRetries { get; set; }
        public string AccessKey { get; set; }
        public string SecretKey { get; set; }
        public string BucketName { get; set; }
        public string DynamoTableName { get; set; }
    }
}
