namespace FileStorage.Services.Contracts
{
    public interface IAwsS3Service
    {
        Task UploadFileAsync(string key, Stream inputStream);

        Task<Stream> GetFileAsync(string key);

        Task<List<string>> ListFilesAsync();
    }
}
