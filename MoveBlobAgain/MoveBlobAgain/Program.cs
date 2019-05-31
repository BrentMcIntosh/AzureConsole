using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Blob;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace MoveBlobAgain
{
    class Program
    {
        const string SOURCE_FOLDER = "SystemDocument";
        const string DESTINATION_FOLDER = "systemdocument";
        const string CONNECTION = " ";

        static void Main()
        {
            Console.WriteLine($"Started Copy from {SOURCE_FOLDER} to {DESTINATION_FOLDER}.");

            Copy().GetAwaiter().GetResult();

            Console.WriteLine($"Finished Copy from {SOURCE_FOLDER} to {DESTINATION_FOLDER}.");
        }

        private static async Task Copy()
        {
            var storageAccount = CloudStorageAccount.Parse(CONNECTION);

            var fileClient = storageAccount.CreateCloudBlobClient();

            var sentry = fileClient.GetContainerReference("sentry");

            var sourceFiles = await ListBlobsAsync(sentry.GetDirectoryReference(SOURCE_FOLDER));

            foreach (var sourceFile in sourceFiles)
            {
                var name = sourceFile.StorageUri.PrimaryUri.Segments[3];

                try
                {
                    var source = sentry.GetBlockBlobReference($"{SOURCE_FOLDER}/{name}");

                    var destination = sentry.GetBlockBlobReference($"{DESTINATION_FOLDER}/{name}");

                    if (!await destination.ExistsAsync())
                    {
                        using (var stream = await source.OpenReadAsync())
                        {
                            await destination.UploadFromStreamAsync(stream);
                        }
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Problem with {name} {ex.Message} {ex?.InnerException?.Message}");
                }
            }
        }

        private static async Task<List<IListBlobItem>> ListBlobsAsync(CloudBlobDirectory cloudBlobDirectory)
        {
            BlobContinuationToken continuationToken = null;

            var results = new List<IListBlobItem>();

            do
            {
                var response = await cloudBlobDirectory.ListBlobsSegmentedAsync(continuationToken);

                continuationToken = response.ContinuationToken;

                results.AddRange(response.Results);
            }
            while (continuationToken != null);

            return results;
        }
    }
}
