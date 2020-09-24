using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Microsoft.Azure.WebJobs;
using Microsoft.Extensions.Logging;

namespace ImageApi
{
    public static class ImageHandler
    {
        private static Dictionary<string, string> _dict;

        [FunctionName("ImageHandler")]
        public static async Task Run(
            [QueueTrigger("imagequeue")] Dictionary<string, string> dict,
            IBinder binder,
            ILogger log)
        {
            _dict = dict;

            try
            {
                byte[] buf;

                using (var reader = binder.Bind<Stream>(new BlobAttribute($"images/{_dict["fileHandle"]}", FileAccess.Read)))
                {
                    log.LogInformation($"{reader.Length}");
                    buf = new byte[reader.Length];
                    await reader.ReadAsync(buf);
                }

                using (var writer = binder.Bind<Stream>(new BlobAttribute($"images/{_dict["fileHandle"]}", FileAccess.Write)))
                {
                    if (!_dict.ContainsKey("exception"))
                    {
                        var editor = new ImageEditor();
                        byte[] newImage = await editor.AddTextToImage(buf, (_dict["pictureText"], (20f, 20f), 32, "f8f8ff"));
                        await writer.WriteAsync(newImage);
                    }
                    else
                    {
                        var editor = new ImageEditor();
                        byte[] newImage = await editor.AddTextToImage(buf, (_dict["exception"], (20f, 20f), 32, "000000"));
                        await writer.WriteAsync(newImage);
                    }
                }

                log.LogInformation($"C# Queue trigger function processed: {_dict["fileHandle"]}");
            }
            catch (Exception ex)
            {
                log.LogInformation($"{ex.Message}");
            }
        }
    }
}
