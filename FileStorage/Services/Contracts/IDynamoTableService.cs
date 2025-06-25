namespace FileStorage.Services.Contracts
{
    public interface IDynamoTableService
    {
        Task PutItemAsync(string fileName, string sha256, DateTime uploadedAt);
    }
}
