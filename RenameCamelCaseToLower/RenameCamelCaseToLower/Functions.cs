using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.Azure.WebJobs;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Blob;

namespace RenameCamelCaseToLower
{
    public class Functions
    {
        public static void ProcessQueueMessage([QueueTrigger("queue")] string message, TextWriter log)
        {
            log.WriteLine(message);

            Copy(log).GetAwaiter().GetResult();
        }

        private static async Task Copy(TextWriter log)
        {
            var connectionString = " ";

            var storageAccount = CloudStorageAccount.Parse(connectionString);

            CloudBlobClient fileClient = storageAccount.CreateCloudBlobClient();

            var sentry = fileClient.GetContainerReference("sentry");

            var CamelCaseDir = sentry.GetDirectoryReference("Family");

            var sourceFiles = CamelCaseDir.ListBlobs();

            foreach (var sourceFile in sourceFiles)
            {
                var name = sourceFile.StorageUri.PrimaryUri.Segments[3];

                try
                {
                    var source = sentry.GetBlockBlobReference($"Family/{name}");

                    var destination = sentry.GetBlockBlobReference($"family/{name}");

                    if (!destination.Exists())
                    {
                        using (var stream = await source.OpenReadAsync())
                        {
                            await destination.UploadFromStreamAsync(stream);
                        }
                    }
                }
                catch(Exception ex)
                {
                    log.WriteLine($"Problem with {name} {ex.Message} {ex?.InnerException?.Message}");
                }
            }
        }
    }
}
