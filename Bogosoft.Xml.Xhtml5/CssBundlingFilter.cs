using Bogosoft.Hashing;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using System.Xml;

namespace Bogosoft.Xml.Xhtml5
{
    /// <summary>
    /// An XHTML5 document filter strategy that bundles multiple CSS files into a single file.
    /// </summary>
    public class CssBundlingFilter : IFilterXml
    {
        /// <summary>
        /// Get or set the hashing strategy associated with the current filter.
        /// </summary>
        protected IHash HashStrategy;

        /// <summary>
        /// Get or set the local filepath associated with the current web application.
        /// </summary>
        protected string PhysicalApplicationPath;

        /// <summary>
        /// Get or set the local filepath for cached files.
        /// </summary>
        protected string PhysicalCachePath;

        /// <summary>
        /// Get or set the virtual filepath of the associated web application.
        /// </summary>
        protected string VirtualApplicationPath;

        /// <summary>
        /// Create a new instance of the <see cref="CssBundlingFilter"/> class.
        /// </summary>
        /// <param name="physicalApplicationPath">
        /// A value corresponding to the physical directory within which the current web application is running.
        /// </param>
        /// <param name="virtualApplicationPath">
        /// A value corresponding to the virtual directory within which the current web application is running.
        /// </param>
        /// <param name="physicalCachePath">
        /// A value corresponding to the local filesystem directory where cached files are to be stored.
        /// </param>
        /// <param name="hashStrategy">A hashing strategy.</param>
        public CssBundlingFilter(
            string physicalApplicationPath,
            string virtualApplicationPath,
            string physicalCachePath,
            IHash hashStrategy
            )
        {
            HashStrategy = hashStrategy;
            PhysicalApplicationPath = physicalApplicationPath;
            PhysicalCachePath = physicalCachePath;
            VirtualApplicationPath = virtualApplicationPath;
        }

        /// <summary>
        /// Filter a given XML document.
        /// </summary>
        /// <param name="document">A document to filter.</param>
        /// <param name="token">A <see cref="CancellationToken"/> object.</param>
        /// <returns>
        /// A <see cref="Task"/> representing the asynchronous operation.
        /// </returns>
        public async Task FilterAsync(XmlDocument document, CancellationToken token)
        {
            var head = document.SelectNodes("/html/head").Cast<XmlNode>().FirstOrDefault();

            if(head == null)
            {
                return;
            }

            var attributes = head.SelectNodes("link")
                                 .Cast<XmlNode>()
                                 .Where(x => x.NodeType == XmlNodeType.Element)
                                 .Select(x => x as XmlElement)
                                 .Where(x => x.HasAttribute("href"));

            var locations = new List<string>();

            string href;

            XmlNode last = null;

            foreach(var x in attributes)
            {
                href = x.GetAttribute("href");

                if (!href.EndsWith(".css", StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                if (x.HasAttribute("data-bundle") && x.GetAttribute("data-bundle").ToLower() == "false")
                {
                    x.RemoveAttribute("data-bundle");

                    continue;
                }

                locations.Add(href);

                last = head.RemoveChild(x);
            }

            if(locations.Count < 2)
            {
                if(locations.Count == 1)
                {
                    head.AppendChild(last);
                }

                return;
            }

            var filename = HashStrategy.Compute(locations).ToHexString() + ".css";

            var link = document.CreateElement("link");

            head.AppendChild(link);

            link.SetAttribute("href", $"{VirtualApplicationPath}/{filename}");
            link.SetAttribute("rel", "stylesheet");

            var filepath = Path.Combine(PhysicalCachePath, filename);

            if (File.Exists(filepath))
            {
                return;
            }

            using (var client = new HttpClient())
            using (var output = new FileStream(filepath, FileMode.Create, FileAccess.Write))
            {
                foreach(var x in locations)
                {
                    using (var source = await GetStreamAsync(x, client, token))
                    {
                        await source.CopyToAsync(output);
                    }
                }
            }
        }

        /// <summary>
        /// Convert a given location to a string.
        /// </summary>
        /// <param name="location">An absolute URL or filepath or a relative filepath.</param>
        /// <param name="client">
        /// An HTTP client to use for requesting file contents from a web server.
        /// </param>
        /// <param name="token">A <see cref="CancellationToken"/> object.</param>
        /// <returns>A stream.</returns>
        protected async Task<Stream> GetStreamAsync(string location, HttpClient client, CancellationToken token)
        {
            token.ThrowIfCancellationRequested();

            if (location.StartsWith("http://") || location.StartsWith("https://"))
            {
                return await client.GetStreamAsync(location);
            }
            else
            {
                location = Path.Combine(PhysicalApplicationPath, location);

                return new FileStream(location, FileMode.Open, FileAccess.Read);
            }
        }
    }
}