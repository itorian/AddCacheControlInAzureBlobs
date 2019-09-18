using System;
using System.Linq;
using System.Threading.Tasks;
using System.Collections.Generic;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Auth;
using Microsoft.WindowsAzure.Storage.Blob;

namespace ConsoleApp1
{
    // Reference https://stackoverflow.com/a/26319001/1386991
    class Program
    {
        static StorageCredentials storageCredentials = new StorageCredentials("storage_account_name", @"storage_account_access_key");
        private static string newCacheSettings = "public, max-age=604800"; // 1 week
        private static string[] containersToProcess = { "container1", "container2" };

        static void Main(string[] args)
        {
            var account = new CloudStorageAccount(storageCredentials, false);

            CloudBlobClient blobClient = account.CreateCloudBlobClient();

            var tasks = new List<Task>();
            foreach (var container in blobClient.ListContainers())
            {
                if (containersToProcess.Contains(container.Name))
                {
                    var c = container;
                    tasks.Add(Task.Run(() => FixHeaders(c)));
                }
            }

            Task.WaitAll(tasks.ToArray());
        }

        private static async Task FixHeaders(CloudBlobContainer cloudBlobContainer)
        {
            int totalCount = 0, updateCount = 0, errorCount = 0;

            Console.WriteLine("Starting container: " + cloudBlobContainer.Name);
            IEnumerable<IListBlobItem> blobInfos = cloudBlobContainer.ListBlobs(useFlatBlobListing: true);

            foreach (var blobInfo in blobInfos)
            {
                try
                {
                    CloudBlockBlob blockBlob = (CloudBlockBlob)blobInfo;
                    var blob = await cloudBlobContainer.GetBlobReferenceFromServerAsync(blockBlob.Name);
                    blob.FetchAttributes();

                    // set cache-control header if necessary
                    if (blob.Properties.CacheControl != newCacheSettings)
                    {
                        blob.Properties.CacheControl = newCacheSettings;
                        blob.SetProperties();
                        updateCount++;
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex.Message);
                    errorCount++;
                }
                totalCount++;
            }

            Console.WriteLine("Finished container: " + cloudBlobContainer.Name + ", TotalCount = " + totalCount + ", Updated = " + updateCount + ", Errors = " + errorCount);
        }
    }
}