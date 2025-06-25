namespace sf_backend_ass_test.services
{
    using Amazon.DynamoDBv2;
    using Amazon.DynamoDBv2.DocumentModel;
    using FileStorage.Models;
    using FileStorage.Services.Contracts;
    using Microsoft.Extensions.Options;

    public class DynamoTableService : IDynamoTableService
    {
        private readonly IAmazonDynamoDB _dynamoClient;
        private readonly string _tableName;

        public DynamoTableService(IOptions<AWSModels> awsOptions, IAmazonDynamoDB dynamoClient)
        {
            _dynamoClient = dynamoClient;
            _tableName = awsOptions.Value.DynamoTableName;
        }

        public async Task PutItemAsync(string key, DateTime uploadedAt)
        {
            var table = Table.LoadTable(_dynamoClient, _tableName);
            var doc = new Document
            {
                ["Filename"] = key,
                ["UploadedAt"] = uploadedAt.ToString("o")
            };
            await table.PutItemAsync(doc);
        }
    }
}
