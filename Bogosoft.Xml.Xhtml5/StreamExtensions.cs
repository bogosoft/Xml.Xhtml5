using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace Bogosoft.Xml.Xhtml5
{
    static class StreamExtensions
    {
        internal static Task CopyToAsync(this Stream source, Stream target, CancellationToken token)
        {
            token.ThrowIfCancellationRequested();

            return source.CopyToAsync(target);
        }
    }
}