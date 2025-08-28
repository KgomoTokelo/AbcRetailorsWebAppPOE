using Azure.Data.Tables;
using Azure.Storage.Blobs;
using Azure.Storage.Queues;
using Azure.Storage.Files.Shares;
using AbcRetailors.Models;
using System.Text.Json;

namespace AbcRetailors.Services
{ 
    public class AzureStorageService: IAzureStorageService
{
    private readonly TableServiceClient _tableServiceClient;
    private readonly BlobServiceClient _blobServiceClient;
    private readonly QueueServiceClient _queueServiceClient;
    private readonly ShareServiceClient _shareServiceClient;
    private readonly ILogger<AzureStorageService> _logger;

    public AzureStorageService(

        IConfiguration configuration,
        ILogger<AzureStorageService> logger)
    {
        string connectionString = configuration.GetConnectionString("AzureStorage")
            ?? throw new InvalidOperationException("Connection string 'AzureStorage' not found.");

        _tableServiceClient = new TableServiceClient(connectionString);
         _blobServiceClient = new BlobServiceClient(connectionString);
         _queueServiceClient = new QueueServiceClient(connectionString);
         _shareServiceClient = new ShareServiceClient(connectionString);
        _logger = logger;

        InitializeStorageAsync().Wait();
    }

        public async Task InitializeStorageAsync()
        {
            try
            {
                _logger.LogInformation("Initializing Azure Storage...");
                // Initialize Tables
                await _tableServiceClient.CreateTableIfNotExistsAsync("Products");
                await _tableServiceClient.CreateTableIfNotExistsAsync("Customers");
                await _tableServiceClient.CreateTableIfNotExistsAsync("Orders");
                _logger.LogInformation("Tables created successfully.");

                // Initialize Blob Containers
                var productsImageContainer = _blobServiceClient.GetBlobContainerClient("productimages");
                await productsImageContainer.CreateIfNotExistsAsync(Azure.Storage.Blobs.Models.PublicAccessType.Blob);

                var paymentProofsContainer = _blobServiceClient.GetBlobContainerClient("paymentproofs");
                await paymentProofsContainer.CreateIfNotExistsAsync(Azure.Storage.Blobs.Models.PublicAccessType.None);

                _logger.LogInformation("Blob containers created successfully.");

                // Initialize Queues
                var orderQueue = _queueServiceClient.GetQueueClient("orders-notifications");
                await orderQueue.CreateIfNotExistsAsync();

                var stockQueue = _queueServiceClient.GetQueueClient("stock-updates");
                await stockQueue.CreateIfNotExistsAsync();

                _logger.LogInformation("Queues created successfully.");

                // Initialize File Shares

                var ContractsShare = _shareServiceClient.GetShareClient("contracts");
                await ContractsShare.CreateIfNotExistsAsync();

                var contractsDirectory = ContractsShare.GetDirectoryClient("payments");
                await contractsDirectory.CreateIfNotExistsAsync();

                _logger.LogInformation("File shares created successfully.");

                _logger.LogInformation("Azure Storage initialization completed.");



            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during Azure Storage initialization.{Message}", ex.Message);
                throw;
            }
        }

    public Task DeleteBlobAsync(string blobName, string ContainerName)
    {
        var containerClient = _blobServiceClient.GetBlobContainerClient(ContainerName);
            var blobClient = containerClient.GetBlobClient(blobName);
            return blobClient.DeleteIfExistsAsync();
        }

    public async Task<byte[]> DownloadFormFileShareAsync(string fileName, string shareName, string directoryName = "")
    {
            var sharelistClient = _shareServiceClient.GetShareClient(shareName);
            var directoryClient = string.IsNullOrEmpty(directoryName)
                ? sharelistClient.GetRootDirectoryClient()
                : sharelistClient.GetDirectoryClient(directoryName);

            var fileClient = directoryClient.GetFileClient(fileName);
            var response = await fileClient.DownloadAsync();

            using var memoryStream = new MemoryStream();
            await response.Value.Content.CopyToAsync(memoryStream);

            return memoryStream.ToArray();
        }

    public async Task<string?> RecieveMessageAsync(string queueName)
    {
       var queueClient = _queueServiceClient.GetQueueClient(queueName);
            var Response = await queueClient.ReceiveMessageAsync();
           if (Response.Value != null)
            {
                queueClient.DeleteMessageAsync(Response.Value.MessageId, Response.Value.PopReceipt);
                return Response.Value.MessageText;
            }
            return null;
        }

    public async Task SendMessageAsync(string queueName, string messsage)
    {
       var queueClient = _queueServiceClient.GetQueueClient(queueName);
         await queueClient.SendMessageAsync(messsage);
        }

        public async Task<string> UploadFileAsync(IFormFile file, string ContainersName)
        {
            try
            {
                var containerClient = _blobServiceClient.GetBlobContainerClient(ContainersName);
                await containerClient.CreateIfNotExistsAsync(Azure.Storage.Blobs.Models.PublicAccessType.None);
                var fileName = $"{DateTime.Now:yyyyMMdd_HHmmss}_{file.FileName}";
                var blobClient = containerClient.GetBlobClient(fileName);

                using var stream = file.OpenReadStream();
                await blobClient.UploadAsync(stream, overwrite: true);
                return fileName;

            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error uploading file to Blob Storage. Message: {containerName}: {Message}", ContainersName, ex.Message);
                throw;
            }
        }

        public async Task<string> UploadImageAsync(IFormFile file, string ContainerName)
        {
            try
            {
                var containerClient = _blobServiceClient.GetBlobContainerClient(ContainerName);


                await containerClient.CreateIfNotExistsAsync(Azure.Storage.Blobs.Models.PublicAccessType.Blob);

                var fileName = $"{Guid.NewGuid()}{Path.GetExtension(file.FileName)}";
                var blobClient = containerClient.GetBlobClient(file.FileName);
                using var stream = file.OpenReadStream();
                await blobClient.UploadAsync(stream, overwrite: true);

                return blobClient.Uri.ToString();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error uploading image to Blob Storage. Message: {containerName}: {Message}",ContainerName, ex.Message);
                throw;
            }
        }
    

    public async Task<string> UploadToFileShareAsync(IFormFile file, string shareName, string directoryName = "")
    {
        var sharelistClient = _shareServiceClient.GetShareClient(shareName);
            var directoryClient = string.IsNullOrEmpty(directoryName) 
                ? sharelistClient.GetRootDirectoryClient() 
                : sharelistClient.GetDirectoryClient(directoryName);

           
            await directoryClient.CreateIfNotExistsAsync();

            var fileName =$"{DateTime.Now:yyyyMMdd_HHmmss}_{file.FileName}";
            var fileClient = directoryClient.GetFileClient(file.FileName);
           

           
            using var stream = file.OpenReadStream();
            await fileClient.CreateAsync(file.Length);
            await fileClient.UploadAsync(stream);

            return fileName;
        }

    public async Task<T> AddEntityAsync<T>(T entity) where T : class, ITableEntity, new()
        {
            var tableName = GetTableName<T>();
            var tableClient = _tableServiceClient.GetTableClient(tableName);

            await tableClient.AddEntityAsync(entity);
            return entity;
        }
    

     public async Task DeleteEntityAsync<T>(string partitionKey, string rowKey)where T : class, ITableEntity, new()
        {
            var tableName = GetTableName<T>();
            var tableClient = _tableServiceClient.GetTableClient(tableName);
            await tableClient.DeleteEntityAsync(partitionKey, rowKey);
        }


        public async Task<List<T>> GetAllEntitiesAsync<T>() where T : class, ITableEntity, new()
        {
      var tableName = GetTableName<T>();
            var tableClient = _tableServiceClient.GetTableClient(tableName);
            var entities = new List<T>();
            await foreach (var entity in tableClient.QueryAsync<T>())
            {
                entities.Add(entity);
            }
            return entities;

        }

    public async Task<T?> GetEntityAsync<T>(string partitionKey, string rowKey) where T : class, ITableEntity, new()
        {
            var tableName = GetTableName<T>();
            var tableClient = _tableServiceClient.GetTableClient(tableName);
            try
            {
                var response = await tableClient.GetEntityAsync<T>(partitionKey, rowKey);
                return response.Value;
            }
            catch (Azure.RequestFailedException ex) when (ex.Status == 404)
            {
                
                return null;
            }
        }
    
       
    

     public async Task<T> UpdateEntityAsync<T>(T entity)where T : class, ITableEntity, new()
        {
            var tableName = GetTableName<T>();
            var tableClient = _tableServiceClient.GetTableClient(tableName);

            try
            {
                await tableClient.UpdateEntityAsync(entity, entity.ETag, TableUpdateMode.Replace);
                return entity;
            }catch(Azure.RequestFailedException ex) when (ex.Status == 412)
            {
                _logger.LogWarning("Concurrency conflict detected for entity with PartitionKey: {PartitionKey}, RowKey: {RowKey}. Retrying with ETag.All.", 
                typeof(T).Name, entity.RowKey);
                throw new InvalidOperationException("The entity was modified by another process. Please reload and try again.");
            }catch(Exception ex)
            {
                _logger.LogError(ex, "Error updating entity with RowKey: {RowKey}. Message: {Message}", 
                    typeof(T).Name, entity.RowKey, ex.Message);
                throw;
            }

        }
        private static string GetTableName<T>()
        {
            return typeof(T).Name switch
            {
                "Product" => "Products",
                "Customer" => "Customers",
                "Order" => "Orders",
                _ => typeof(T).Name + "s",
            };// Simple pluralization by adding 's'
        }

    }
}
