using System;
using System.Threading.Tasks;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Blob;

namespace ImageApi
{
    class BlobService
    {
        public BlobService()
        {

        }

        public static CloudStorageAccount CreateStorageAccountFromConnectionString()
        {
            CloudStorageAccount storageAccount;

            try
            {
                string connection = System.Environment.GetEnvironmentVariable("AzureWebJobsStorage");
                storageAccount = CloudStorageAccount.Parse(connection);
            }
            catch (Exception error)
            {
                Console.WriteLine(error.Message);
                throw;
            }

            return storageAccount;
        }

        public async Task<string> GetBlobUrl(string imageName)
        {
            string policyName = "readImage";
            CloudStorageAccount storageAccount = CreateStorageAccountFromConnectionString();

            // Create a blob client for interacting with the blob service.
            CloudBlobClient blobClient = storageAccount.CreateCloudBlobClient();

            // get a reference to the container
            CloudBlobContainer blobContainer = blobClient.GetContainerReference("images");

            // create the stored policy we will use, with the relevant permissions and expiry time
            SharedAccessBlobPolicy storedPolicy = new SharedAccessBlobPolicy()
            {
                SharedAccessExpiryTime = DateTime.UtcNow.AddHours(1),
                Permissions = SharedAccessBlobPermissions.Read
            };

            // get the existing permissions (alternatively create new BlobContainerPermissions())
            BlobContainerPermissions permissions = await blobContainer.GetPermissionsAsync();

            // optionally clear out any existing policies on this container
            permissions.SharedAccessPolicies.Clear();
            // add in the new one
            permissions.SharedAccessPolicies.Add(policyName, storedPolicy);
            // save back to the container
            await blobContainer.SetPermissionsAsync(permissions);

            // Now we are ready to create a shared access signature based on the stored access policy
            string containerSignature = blobContainer.GetSharedAccessSignature(null, policyName);

            // create the URI a client can use to get access to just this container
            string uri = $"{blobContainer.Uri}/{imageName}{containerSignature}";

            return uri;
        }
    }
}