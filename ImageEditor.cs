using System;
using System.IO;
using System.Threading.Tasks;
using SixLabors.Fonts;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Drawing.Processing;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;

namespace ImageApi
{
    class ImageEditor
    {
        public ImageEditor() { }

        public async Task<byte[]> AddTextToImage(byte[] img, params (string text, (float x, float y) position, int fontSize, string colorHex)[] texts)
        {
            Stream stream;
            MemoryStream memoryStream = new MemoryStream();

            try
            {
                stream = new MemoryStream(img);
            }
            catch (Exception error)
            {
                throw new Exception(error.Message);
            }

            var image = Image.Load(stream);

            await image.Clone(img =>
            {
                var textGraphicsOptions = new TextGraphicsOptions()
                {
                    TextOptions = {
                            WrapTextWidth = image.Width-10
                        }
                };

                foreach (var (text, (x, y), fontSize, colorHex) in texts)
                {
                    var font = SystemFonts.CreateFont("Verdana", fontSize);
                    var color = Rgba32.ParseHex(colorHex);

                    img.DrawText(textGraphicsOptions, text, font, color, new PointF(x, y));
                }
            })
                .SaveAsPngAsync(memoryStream);

            memoryStream.Position = 0;

            byte[] imageBytes = memoryStream.ToArray();

            return imageBytes;
        }
    }
}