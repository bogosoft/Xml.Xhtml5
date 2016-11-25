using System;
using System.IO;
using System.Threading.Tasks;
using System.Xml;

namespace Bogosoft.Xml.Tests
{
    internal static class XmlFormatterExtensions
    {
        internal static async Task<String> ToStringAsync(this XmlFormatter formatter, XmlNode node)
        {
            using (var writer = new StringWriter())
            {
                await formatter.FormatAsync(node, writer);

                return writer.ToString();
            }
        }
    }
}