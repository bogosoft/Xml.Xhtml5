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
    /// An DOM transformation strategy that asynchronously downloads files from remote servers,
    /// copies them to a local cache path and replaces references to remote files with their
    /// cached counterpart paths. References to remote files are expected to be in attributes only.
    /// </summary>
    public class RemoteFileCachingTransformer : IDomTransformer
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
        /// Create a new instance of the <see cref="RemoteFileCachingTransformer"/>.
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
        public RemoteFileCachingTransformer(string physicalCachePath, string virtualCachePath, string xpath)
        {
            PhysicalCachePath = physicalCachePath;
            VirtualCachePath = virtualCachePath.TrimEnd('/');
            XPath = xpath;
        }

        /// <summary>
        /// Create a new instance of the <see cref="RemoteFileCachingTransformer"/>.
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
        public RemoteFileCachingTransformer(
            string physicalCachePath,
            string virtualCachePath,
            IEnumerable<string> xpaths
            )
            : this(physicalCachePath, virtualCachePath, string.Join("|", xpaths))
        {
        }

        /// <summary>
        /// Apply the current DOM node transformation strategy to a given node.
        /// </summary>
        /// <param name="node">A DOM node to transform.</param>
        /// <param name="token">A <see cref="CancellationToken"/> object.</param>
        /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
        public async Task TransformAsync(XmlNode node, CancellationToken token)
        {
            var attributes = node.GetOwnerDocument()
                                 .SelectNodes(XPath)
                                 .Cast<XmlNode>()
                                 .Where(x => x.NodeType == XmlNodeType.Attribute)
                                 .Select(x => x as XmlAttribute);

            string filename, filepath, url;

            using (var client = new HttpClient())
            {
                foreach(var attribute in attributes)
                {
                    url = attribute.Value.ToLower();

                    if(!url.StartsWith("http://") && !url.StartsWith("https://"))
                    {
                        continue;
                    }

                    filename = Path.GetFileName(url);

                    attribute.Value = $"{VirtualCachePath}/{filename}";

                    filepath = Path.Combine(PhysicalCachePath, filename);

                    if (File.Exists(filepath))
                    {
                        continue;
                    }

                    using (var response = await client.GetAsync(url, token))
                    {
                        if (response.StatusCode != HttpStatusCode.OK)
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