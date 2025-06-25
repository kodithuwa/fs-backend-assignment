namespace FileStorage.Services.Contracts
{
    public interface IDynamoTableService
    {
        Task PutItemAsync(string key, DateTime uploadedAt);
    }
}
