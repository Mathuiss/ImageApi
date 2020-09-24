using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.Azure.WebJobs;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace ImageApi
{
    public static class QueryHandler
    {
        private static ILogger _log;

        private static Dictionary<string, string> _dict;

        [FunctionName("QueryHandler")]
        public static async Task Run(
            [QueueTrigger("queryqueue")] string queueResult,
            [Queue("imagequeue")] IAsyncCollector<Dictionary<string, string>> imageQueue,
            IBinder binder,
            ILogger log
            )
        {
            _log = log;

            // queueResult parsen naar dictionary
            try
            {
                _dict = JsonConvert.DeserializeObject<Dictionary<string, string>>(queueResult);
            }
            catch
            {
                throw new NotImplementedException("Failed to parse json");
            }

            log.LogInformation($"C# Queue trigger function processing: {_dict["query"]}");

            // We use an http client to download the image and text in paralel
            // We also save the image to the blob storage, and the _dict to the imagequeue
            // When both tasks are finished the http client is Dispose()'d
            using (var httpClient = new HttpClient())
            {
                var tasks = new Task[]
                {
                    DownloadImageAsync(httpClient, binder),
                    DownloadTextAsync(httpClient, imageQueue)
                };

                await Task.WhenAll(tasks);

                // Save dictionary to new queue
                await imageQueue.AddAsync(_dict);
            }
        }

        private static async Task DownloadImageAsync(HttpClient httpClient, IBinder binder)
        {
            // Query uit dict lezen en daarmee een api call doen naar externe api 'Unsplash'
            HttpResponseMessage res = await httpClient.GetAsync($"https://api.unsplash.com/photos/random?query={_dict["query"]}&client_id=Yv_82U8Vbp-hcda8RjmjTjqyuxeFS6U4tLYaXpKNstI");
            string jsonContent = await res.Content.ReadAsStringAsync();

            try
            {
                // Unsplash api result
                var jobj = JObject.Parse(jsonContent);
                string pictureUrl = jobj.SelectToken("$.urls.regular").Value<string>();
                _log.LogInformation(pictureUrl);

                // Download raw image from url
                res = await httpClient.GetAsync(pictureUrl);
                byte[] picture = await res.Content.ReadAsByteArrayAsync();

                // Save image to blob storage
                using (var writer = binder.Bind<Stream>(new BlobAttribute($"images/{_dict["fileHandle"]}", FileAccess.Write)))
                {
                    await writer.WriteAsync(picture);
                }

                _log.LogInformation($"Written picture to blob storage: {_dict["fileHandle"]}");
            }
            catch
            {
                _log.LogInformation($"No image was found for {_dict["query"]}");

                // Download error image
                res = await httpClient.GetAsync("https://howfix.net/wp-content/uploads/2018/02/sIaRmaFSMfrw8QJIBAa8mA-article.png");
                byte[] picture = await res.Content.ReadAsByteArrayAsync();

                // Save image to blob storage
                using (var writer = binder.Bind<Stream>(new BlobAttribute($"images/{_dict["fileHandle"]}", FileAccess.Write)))
                {
                    await writer.WriteAsync(picture);
                }

                _dict["exception"] = $"Failed to download image for query: {_dict["query"]}";
            }
        }

        private static async Task DownloadTextAsync(HttpClient httpClient, IAsyncCollector<Dictionary<string, string>> imageQueue)
        {
            // Gerelateerde tekst van de query ophalen uit externe api
            HttpResponseMessage res = await httpClient.GetAsync($"https://en.wikipedia.org/w/api.php?action=query&format=json&list=search&srsearch={_dict["query"]}");
            string jsonContent = await res.Content.ReadAsStringAsync();

            try
            {
                // Mediawiki api result
                var jobj = JObject.Parse(jsonContent);
                _dict["pictureText"] = jobj.SelectToken("$.query.search[0].title").Value<string>();
                _log.LogInformation(_dict["pictureText"]);

                _log.LogInformation($"Downloaded text from wikipedia: {_dict["pictureText"]}");
            }
            catch
            {
                _log.LogInformation($"No text was found for {_dict["query"]}");
                _dict["exception"] = $"Failed to download text from wikipedia for query: {_dict["query"]}";
            }
        }
    }
}
