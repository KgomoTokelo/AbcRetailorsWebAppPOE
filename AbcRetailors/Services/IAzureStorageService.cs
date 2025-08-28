namespace AbcRetailors.Services
{
    public interface IAzureStorageService
    {
        //Table operations
        Task<List<T>> GetAllEntitiesAsync<T>() where T : class, Azure.Data.Tables.ITableEntity, new();
        Task<T?> GetEntityAsync<T>(string partitionKey,string rowKey) where T : class, Azure.Data.Tables.ITableEntity, new();
        Task<T> AddEntityAsync<T>(T entity) where T : class, Azure.Data.Tables.ITableEntity, new();
        Task<T> UpdateEntityAsync<T>(T entity) where T : class, Azure.Data.Tables.ITableEntity, new();
        Task DeleteEntityAsync<T>(string partitionKey, string rowKey) where T : class, Azure.Data.Tables.ITableEntity, new();

        //blob operations
        Task<string> UploadImageAsync(IFormFile file, string ContainerName);
        Task<string> UploadFileAsync(IFormFile file, string ContainersName);
        Task DeleteBlobAsync(string blobName, string ContainerName);

        //Queue operations
        Task SendMessageAsync(string queueName,string messsage);
        Task<string> RecieveMessageAsync(string queueName);

        //FileShare operations
        Task<string> UploadToFileShareAsync(IFormFile file,string shareName, string directoryName ="");
        Task<byte[]> DownloadFormFileShareAsync(string fileName, string shareName, string directoryName = "");

    }
}
