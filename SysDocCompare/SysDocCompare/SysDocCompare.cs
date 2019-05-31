using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Blob;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

using static System.Console;

namespace SysDocCompare
{
    class SysDocCompare
    {
        const string SOURCE_FOLDER = "SystemDocument";

        const string DESTINATION_FOLDER = "systemdocument";

        const string CONNECTION = " ";

        private static async Task<List<IListBlobItem>> ListBlobsAsync(CloudBlobDirectory cloudBlobDirectory)
        {
            BlobContinuationToken continuationToken = null;

            BlobResultSegment blobResultSegment = null;

            List<IListBlobItem> blobItems = new List<IListBlobItem>();

            do
            {
                try
                {
                    blobResultSegment = await cloudBlobDirectory.ListBlobsSegmentedAsync(continuationToken);

                    continuationToken = blobResultSegment.ContinuationToken;

                    blobItems.AddRange(blobResultSegment.Results);

                    if (blobItems.Count >= 100000) break;
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Problem with {blobResultSegment?.Results} {ex.Message} {ex?.InnerException?.Message}");
                }
            }
            while (continuationToken != null);

            return blobItems;
        }

        static async Task CompareBlobNames()
        {
            var storageAccount = CloudStorageAccount.Parse(CONNECTION);

            var fileClient = storageAccount.CreateCloudBlobClient();

            var sentry = fileClient.GetContainerReference("sentry");

            var sourceBlobs = await ListBlobsAsync(sentry.GetDirectoryReference(SOURCE_FOLDER));

            WriteLine($"Found {sourceBlobs.Count} blobs in {SOURCE_FOLDER}");

            var count = 0;

            foreach (var blobItem in sourceBlobs)
            {
                string name = string.Empty;

                try
                {
                    if (blobItem?.StorageUri?.PrimaryUri?.Segments != null && blobItem.StorageUri.PrimaryUri.Segments.Length > 3)
                    {
                        name = blobItem.StorageUri.PrimaryUri.Segments[3];

                        var source = sentry.GetBlockBlobReference($"{SOURCE_FOLDER}/{name}");

                        var destination = sentry.GetBlockBlobReference($"{DESTINATION_FOLDER}/{name}");

                        if (await destination.ExistsAsync())
                        {
                            await source.FetchAttributesAsync();

                            await destination.FetchAttributesAsync();

                            if (source.Properties.ContentMD5 != destination.Properties.ContentMD5)
                            {
                                WriteLine($"{name} was different");
                            }
                        }
                        else
                        {
                            WriteLine($"{name} was NOT at destination");
                        }

                        count++;

                        if (count % 1000 == 0)
                        {
                            WriteLine($"{count} blobs compared so far {name} was the last one");
                        }
                    }
                    else
                    {
                        WriteLine($"{blobItem} could NOT be copied");
                    }
                }
                catch (Exception ex)
                {
                    WriteLine($"Problem with {name} {ex.Message} {ex?.InnerException?.Message}");
                }
            }
        }

        static void Main()
        {
            WriteLine("Confirm that every blob in SystemDocument is in systemdocument");

            CompareBlobNames().GetAwaiter().GetResult();
        }
    }
}
