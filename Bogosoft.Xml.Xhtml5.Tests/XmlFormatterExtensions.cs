using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using System.Xml;

namespace Bogosoft.Xml.Xhtml5.Tests
{
    internal static class XmlFormatterExtensions
    {
        internal static async Task<String> ToStringAsync(
            this IFormatXml formatter,
            XmlNode node,
            CancellationToken token = default(CancellationToken)
            )
        {
            using (var writer = new StringWriter())
            {
                await formatter.FormatAsync(node, writer, token);

                return writer.ToString();
            }
        }
    }
}