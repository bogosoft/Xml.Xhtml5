using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using System.Xml;

namespace Bogosoft.Xml.Xhtml5
{
    /// <summary>
    /// An XML document filter strategy that asynchronously downloads files from remote servers,
    /// copies them to a local cache path and replaces references to remote files with their
    /// cached counterpart paths. References to remote files are expected to be in attributes only.
    /// </summary>
    public class RemoteFileCachingFilter : IFilterXml
    {
        /// <summary>
        /// Get or set the absolute physical (local) path to a directory where locally
        /// cached files are to be stored.
        /// </summary>
        protected string PhysicalCachePath;

        /// <summary>
        /// Get or set the virtual directory which will serve requests for locally cached files.
        /// </summary>
        protected string VirtualCachePath;

        /// <summary>
        /// Get or set the XPath expression that will be used to select attributes to filter.
        /// </summary>
        protected string XPath;

        /// <summary>
        /// Create a new instance of the <see cref="RemoteFileCachingFilter"/>.
        /// </summary>
        /// <param name="physicalCachePath">
        /// A value corresponding to the absolute physical (local) path to a directory where locally
        /// cached files are to be stored.
        /// </param>
        /// <param name="virtualCachePath">
        /// A value corresponding to the virtual directory which will serve requests for locally cached files.
        /// </param>
        /// <param name="xpath">
        /// An XPath expression that will be used to select attributes to filter.
        /// </param>
        public RemoteFileCachingFilter(string physicalCachePath, string virtualCachePath, string xpath)
        {
            PhysicalCachePath = physicalCachePath;
            VirtualCachePath = virtualCachePath.TrimEnd('/');
            XPath = xpath;
        }

        /// <summary>
        /// Create a new instance of the <see cref="RemoteFileCachingFilter"/>.
        /// </summary>
        /// <param name="physicalCachePath">
        /// A value corresponding to the absolute physical (local) path to a directory where locally
        /// cached files are to be stored.
        /// </param>
        /// <param name="virtualCachePath">
        /// A value corresponding to the virtual directory which will serve requests for locally cached files.
        /// </param>
        /// <param name="xpaths">
        /// A sequence of XPath expressions that will be used to select attributes to filter.
        /// </param>
        public RemoteFileCachingFilter(
            string physicalCachePath,
            string virtualCachePath,
            IEnumerable<string> xpaths
            )
            : this(physicalCachePath, virtualCachePath, string.Join("|", xpaths))
        {
        }

        /// <summary>
        /// Filter a given XML document.
        /// </summary>
        /// <param name="document">An XML document to filter.</param>
        /// <param name="token">A <see cref="CancellationToken"/> object.</param>
        /// <returns>
        /// A <see cref="Task"/> representing the asynchronous operation.
        /// </returns>
        public async Task FilterAsync(XmlDocument document, CancellationToken token)
        {
            var attributes = document.SelectNodes(XPath)
                                     .Cast<XmlNode>()
                                     .Where(x => x.NodeType == XmlNodeType.Attribute)
                                     .Select(x => x as XmlAttribute);

            string url;

            using (var client = new HttpClient())
            {
                foreach(var attribute in attributes)
                {
                    url = attribute.Value.ToLower();

                    if(!url.StartsWith("http://") && !url.StartsWith("https://"))
                    {
                        continue;
                    }

                    using (var response = await client.GetAsync(url, token))
                    {
                        if (response.StatusCode != HttpStatusCode.OK)
                        {
                            continue;
                        }

                        var filename = Path.GetFileName(url);

                        attribute.Value = $"{VirtualCachePath}/{filename}";

                        var filepath = Path.Combine(PhysicalCachePath, filename);

                        if (File.Exists(filepath))
                        {
                            continue;
                        }

                        using (var destination = new FileStream(filepath, FileMode.Create, FileAccess.Write))
                        {
                            await response.Content.CopyToAsync(destination, token);
                        }
                    }
                }
            }
        }
    }
}