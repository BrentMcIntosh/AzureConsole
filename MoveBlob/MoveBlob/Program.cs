using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Blob;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace MoveBlob
{
    class Program
    {
        const string SOURCE_FOLDER = "Family";
        const string DESTINATION_FOLDER = "family";
        const string CONNECTION = " ";

        static void Main()
        {
            Console.WriteLine($"Started Copy from {SOURCE_FOLDER} to {DESTINATION_FOLDER}.");

            Copy().GetAwaiter().GetResult();

            Console.WriteLine($"Finished Copy from {SOURCE_FOLDER} to {DESTINATION_FOLDER}.");
        }

        private static async Task CopySegment(CloudBlobContainer cloudBlobContainer, List<IListBlobItem> listBlobItems)
        {
            Console.WriteLine($"got list of source blobs from {SOURCE_FOLDER}");

            var count = 0;

            foreach (var blobItem in listBlobItems)
            {
                string name = string.Empty;

                try
                {
                    if (blobItem?.StorageUri?.PrimaryUri?.Segments != null && blobItem.StorageUri.PrimaryUri.Segments.Length > 3)
                    {
                        name = blobItem.StorageUri.PrimaryUri.Segments[3];

                        var source = cloudBlobContainer.GetBlockBlobReference($"{SOURCE_FOLDER}/{name}");

                        var destination = cloudBlobContainer.GetBlockBlobReference($"{DESTINATION_FOLDER}/{name}");

                        if (!await destination.ExistsAsync())
                        {
                            using (var stream = await source.OpenReadAsync())
                            {
                                await destination.UploadFromStreamAsync(stream);

                                count++;

                                if (count % 100 == 0)
                                {
                                    Console.WriteLine($"Copied So Far {count} Last Item {name}");
                                }
                            }
                        }
                    }
                    else
                    {
                        Console.WriteLine($"{blobItem} could NOT be copied");
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Problem with {name} {ex.Message} {ex?.InnerException?.Message}");
                }
            }

        }

        private static async Task Copy()
        {
            var storageAccount = CloudStorageAccount.Parse(CONNECTION);

            var fileClient = storageAccount.CreateCloudBlobClient();

            var sentry = fileClient.GetContainerReference("sentry");

            await ListBlobsAsync(sentry.GetDirectoryReference(SOURCE_FOLDER));

            await CopySegment(sentry, ListBlobItems1);

            if (ListBlobItems2.Count > 0)
            {
                await CopySegment(sentry, ListBlobItems2);
            }

            if (ListBlobItems3.Count > 0)
            {
                await CopySegment(sentry, ListBlobItems3);
            }
        }

        static readonly int MAX = 50;

        static List<IListBlobItem> ListBlobItems1 = new List<IListBlobItem>();
        static List<IListBlobItem> ListBlobItems2 = new List<IListBlobItem>();
        static List<IListBlobItem> ListBlobItems3 = new List<IListBlobItem>();

        private static async Task ListBlobsAsync(CloudBlobDirectory cloudBlobDirectory)
        {
            BlobContinuationToken continuationToken = null;

            BlobResultSegment blobResultSegment = null;

            do
            {
                try
                {
                    blobResultSegment = await cloudBlobDirectory.ListBlobsSegmentedAsync(continuationToken);

                    continuationToken = blobResultSegment.ContinuationToken;

                    if (ListBlobItems1.Count < MAX)
                    {
                        ListBlobItems1.AddRange(blobResultSegment.Results);

                        Console.WriteLine($"Segment ONE Count {ListBlobItems1.Count} Last Item {ListBlobItems1[ListBlobItems1.Count - 1].StorageUri.PrimaryUri.Segments[3]}");
                    }
                    else if (ListBlobItems2.Count < MAX)
                    {
                        ListBlobItems2.AddRange(blobResultSegment.Results);

                        Console.WriteLine($"Segment TWO Count {ListBlobItems2.Count} Last Item {ListBlobItems2[ListBlobItems2.Count - 1].StorageUri.PrimaryUri.Segments[3]}");
                    }
                    else if (ListBlobItems3.Count < MAX)
                    {
                        ListBlobItems3.AddRange(blobResultSegment.Results);

                        Console.WriteLine($"Segment THREE Count {ListBlobItems3.Count} Last Item {ListBlobItems3[ListBlobItems3.Count - 1].StorageUri.PrimaryUri.Segments[3]}");
                    }

                    if (ListBlobItems3.Count >= MAX)
                    {
                        break;
                    }
                }
                catch(Exception ex)
                {
                    Console.WriteLine($"Problem with {blobResultSegment?.Results} {ex.Message} {ex?.InnerException?.Message}");
                }
            }
            while (continuationToken != null);
        }
    }
}
