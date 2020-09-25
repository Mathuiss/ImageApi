using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using System.Collections.Generic;

namespace ImageApi
{
    public static class Entrypoint
    {
        [FunctionName("Entrypoint")]
        public static async Task<IActionResult> GetImageHandle(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "image")] HttpRequest req,
            [Queue("queryqueue")] IAsyncCollector<Dictionary<string, string>> queryQueue,
            IBinder binder,
            ILogger log
            )
        {
            string query;

            if (req.Query.ContainsKey("query"))
            {
                query = req.Query["query"];
                query = query.Replace("-", ""); // We guarantee the first - seperates the query from the guid
            }
            else
            {
                log.LogInformation($"No query found in request {req.QueryString}");
                return new BadRequestObjectResult($"No query found in request {req.QueryString}");
            }

            log.LogInformation($"Requesting a new image handle for {query}");

            // Generating a new file handle for the image
            string fileHandle = $"{query}-{Guid.NewGuid().ToString()}.png";

            // Save temp image to blob storage
            using (var writer = binder.Bind<Stream>(new BlobAttribute($"images/{fileHandle}", FileAccess.Write)))
            {
                await writer.WriteAsync(new byte[0]);
            }

            var storage = new BlobService();
            string fileUrl = await storage.GetBlobUrl(fileHandle);

            // Placing the request with the query and fileHandle in the queue
            var dict = new Dictionary<string, string>();
            dict.Add("query", query);
            dict.Add("fileHandle", fileHandle);
            dict.Add("fileUrl", fileUrl);

            try
            {
                await queryQueue.AddAsync(dict);
            }
            catch (Exception ex)
            {
                return new BadRequestObjectResult(ex.Message);
            }

            return new OkObjectResult(dict);
        }
    }
}
