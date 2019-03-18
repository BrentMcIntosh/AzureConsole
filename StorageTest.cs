using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Blob;

namespace StorageTest
{
    class Program
    {
        public async Task<List<CloudBlobContainer>> ListContainersAsync(CloudBlobClient cloudBlobClient)
        {
            BlobContinuationToken continuationToken = null;

            List<CloudBlobContainer> results = new List<CloudBlobContainer>();

            do
            {
                var response = await cloudBlobClient.ListContainersSegmentedAsync(continuationToken);

                continuationToken = response.ContinuationToken;

                results.AddRange(response.Results);
            }
            while (continuationToken != null);

            return results;
        }

        public async Task<List<IListBlobItem>> ListBlobsAsync(CloudBlobContainer cloudBlobContainer)
        {
            BlobContinuationToken continuationToken = null;

            List<IListBlobItem> results = new List<IListBlobItem>();

            do
            {
                var response = await cloudBlobContainer.ListBlobsSegmentedAsync(continuationToken);

                continuationToken = response.ContinuationToken;

                results.AddRange(response.Results);
            }
            while (continuationToken != null);

            return results;
        }

        public async Task<List<IListBlobItem>> ListBlobsAsync(CloudBlobDirectory cloudBlobDirectory)
        {
            BlobContinuationToken continuationToken = null;

            List<IListBlobItem> results = new List<IListBlobItem>();

            do
            {
                var response = await cloudBlobDirectory.ListBlobsSegmentedAsync(continuationToken);

                continuationToken = response.ContinuationToken;

                results.AddRange(response.Results);
            }
            while (continuationToken != null);

            return results;
        }

        private async Task CopyAzureFileToDisk(CloudBlobContainer cloudBlobContainer, string baseFileName)
        {
            var containerExists = await cloudBlobContainer.ExistsAsync(new BlobRequestOptions(), new OperationContext());

            if (containerExists)
            {
                CloudBlockBlob blob = cloudBlobContainer.GetBlockBlobReference(baseFileName);

                var blobExists = await blob.ExistsAsync(new BlobRequestOptions(), new OperationContext());

                var destinationFile = $@"C:\Temp\{baseFileName}";

                if (blobExists)
                {
                    await blob.DownloadToFileAsync(destinationFile, FileMode.Create);
                }
            }
        }

        private async Task CopyAzureFileToDisk(CloudBlockBlob cloudBlockBlob)
        {
            var baseFileName = cloudBlockBlob.StorageUri.PrimaryUri.LocalPath;

            var destinationFile = $@"C:\Temp\{baseFileName}";

            await cloudBlockBlob.DownloadToFileAsync(destinationFile, FileMode.Create);
        }

        static void Main(string[] args)
        {
            var builder = new ConfigurationBuilder()
                .SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true);

            IConfigurationRoot configuration = builder.Build();

            var connectionString = configuration.GetConnectionString("Sentry");
        
            var storageAccount = CloudStorageAccount.Parse(connectionString);

            CloudBlobClient fileClient = storageAccount.CreateCloudBlobClient();

            var program = new Program();

            var containers = program.ListContainersAsync(fileClient).GetAwaiter().GetResult();

            foreach (var container in containers)
            {
                Console.WriteLine(container.Name);

                var blobItems = program.ListBlobsAsync(container).GetAwaiter().GetResult();

                foreach (var blobItem in blobItems)
                {
                    Console.WriteLine(blobItem.StorageUri);

                    var blobDirectory = blobItem as CloudBlobDirectory;

                    if (blobDirectory != null && blobDirectory.StorageUri.PrimaryUri.LocalPath.Contains("sentry"))
                    {
                        var blobFiles = program.ListBlobsAsync(blobDirectory).GetAwaiter().GetResult();

                        foreach (var blobFile in blobFiles)
                        {
                            Console.WriteLine(blobFile.StorageUri);

                            program.CopyAzureFileToDisk(blobFile as CloudBlockBlob).GetAwaiter().GetResult();
                        }
                    }
                }
            }

            Console.ReadKey();
        }
    }
}
