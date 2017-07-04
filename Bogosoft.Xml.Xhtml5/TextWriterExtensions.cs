using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace Bogosoft.Xml.Xhtml5
{
    static class TextWriterExtensions
    {
        internal static Task WriteAsync(this TextWriter writer, string data, CancellationToken token)
        {
            token.ThrowIfCancellationRequested();

            return writer.WriteAsync(data);
        }

        internal static async Task WriteAsync(this TextWriter writer, Image image, CancellationToken token)
        {
            await writer.WriteAsync(@"{""id"":""", token);
            await writer.WriteAsync(image.Id, token);
            await writer.WriteAsync(@""",""data"":""", token);

            using (var stream = new FileStream(image.PhysicalPath, FileMode.Open, FileAccess.Read))
            using (var memory = new MemoryStream())
            {
                await stream.CopyToAsync(memory, token);

                await writer.WriteAsync(Convert.ToBase64String(memory.ToArray()), token);
            }

            await writer.WriteAsync(@"""}", token);
        }
    }
}