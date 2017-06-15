using System.IO;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace Bogosoft.Xml.Xhtml5
{
    static class HttpContentExtensions
    {
        internal static Task CopyToAsync(
            this HttpContent content,
            Stream destination,
            CancellationToken token
            )
        {
            token.ThrowIfCancellationRequested();

            return content.CopyToAsync(destination);
        }
    }
}