using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Xml;

namespace Bogosoft.Xml.Xhtml5
{
    /// <summary>
    /// An implementation of the <see cref="IDomTransformer"/> contract which bundles images.
    /// </summary>
    public class ImageBundlingTransformer : IDomTransformer
    {
        /// <summary>
        /// Get or set the mapping strategy responsible for generating an absolute filepath
        /// against a collection of source URI's.
        /// </summary>
        protected Converter<IEnumerable<string>, string> BundledFilepathMapper;

        /// <summary>
        /// Get or set a document finalizer to invoke once bundling has finished.
        /// </summary>
        protected Action<XmlDocument> Finalizer;

        /// <summary>
        /// Get or set the mapping strategy responsible for generating an ID against a relative URI.
        /// </summary>
        protected Converter<string, string> IdMapper;

        /// <summary>
        /// Get or set the mapping strategy responsible for converting a physical path on the
        /// local filesystem to a relative URI.
        /// </summary>
        protected Converter<string, string> PhysicalPathToRelativeUriMapper;

        /// <summary>
        /// Get or set the mapping strategy responsible for converting a relative URI into a
        /// physical path on the local filesystem.
        /// </summary>
        protected Converter<string, string> RelativeUriToPhysicalPathMapper;

        /// <summary>
        /// Create a new instance of the <see cref="ImageBundlingTransformer"/> class.
        /// </summary>
        /// <param name="physicalPathToRelativeUriMapper">
        /// A strategy for mapping a physical path on the local filesystem to a relative URI.
        /// </param>
        /// <param name="relativeUriToPhysicalPathMapper">
        /// A strategy for mapping a relative URI to a physical path on the local filesystem.
        /// </param>
        /// <param name="bundledFilepathMapper">
        /// A strategy for mapping a relative URI collection to the absolute filepath
        /// of a resource to bundle.
        /// </param>
        /// <param name="idMapper">
        /// A strategy for mapping a relative URI to an ID.
        /// </param>
        /// <param name="finalizer">
        /// A strategy for finalizing an XHTML 5 document after bundling has completed.
        /// </param>
        public ImageBundlingTransformer(
            Converter<string, string> physicalPathToRelativeUriMapper,
            Converter<string, string> relativeUriToPhysicalPathMapper,
            Converter<IEnumerable<string>, string> bundledFilepathMapper,
            Converter<string, string> idMapper,
            Action<XmlDocument> finalizer
            )
        {
            BundledFilepathMapper = bundledFilepathMapper;
            Finalizer = finalizer;
            IdMapper = idMapper;
            PhysicalPathToRelativeUriMapper = physicalPathToRelativeUriMapper;
            RelativeUriToPhysicalPathMapper = relativeUriToPhysicalPathMapper;
        }

        /// <summary>
        /// Apply a DOM node transformation strategy to a given document.
        /// </summary>
        /// <param name="document">A DOM document to transform.</param>
        /// <param name="token">A <see cref="CancellationToken"/> object.</param>
        /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
        protected async Task TransformAsync(XmlDocument document, CancellationToken token)
        {
            var body = document.SelectNodes("/html/body").Cast<XmlElement>().FirstOrDefault();

            var nodes = document.SelectNodes("//img").Cast<XmlElement>().Where(x => x.HasAttribute("src"));

            var targets = new List<Image>();

            string filepath, uri;

            XmlElement last = null;

            foreach(var node in nodes)
            {
                uri = node.GetAttribute("src");

                filepath = RelativeUriToPhysicalPathMapper.Invoke(uri);

                if (!File.Exists(filepath))
                {
                    continue;
                }

                last = node;

                node.RemoveAttribute("src");

                var target = new Image
                {
                    Id = IdMapper.Invoke(uri),
                    PhysicalPath = RelativeUriToPhysicalPathMapper.Invoke(uri),
                    RelativeUri = uri
                };

                node.SetAttribute("data-id", target.Id);

                if(!targets.Select(x => x.RelativeUri).Contains(uri))
                {
                    targets.Add(target);
                }
            }

            if(targets.Count < 2)
            {
                if(targets.Count == 1)
                {
                    last.RemoveAttribute("data-id");

                    last.SetAttribute("src", targets[0].RelativeUri);
                }

                return;
            }

            var cachepath = BundledFilepathMapper.Invoke(targets.Select(x => x.RelativeUri));

            if (!File.Exists(cachepath))
            {
                using (var output = File.Open(cachepath, FileMode.Create, FileAccess.Write))
                using (var writer = new StreamWriter(output))
                {
                    await writer.WriteAsync("__bundledImages=[", token);

                    using (var enumerator = targets.GetEnumerator())
                    {
                        enumerator.MoveNext();

                        await writer.WriteAsync(enumerator.Current, token);

                        while (enumerator.MoveNext())
                        {
                            await writer.WriteAsync(",", token);
                            await writer.WriteAsync(enumerator.Current, token);
                        }
                    }

                    await writer.WriteAsync("];", token);
                }
            }

            var script = document.CreateElement("script");

            body.AppendChild(script);

            script.SetAttribute("src", PhysicalPathToRelativeUriMapper.Invoke(cachepath));

            Finalizer.Invoke(document);
        }

        /// <summary>
        /// Apply a DOM node transformation strategy to a given node.
        /// </summary>
        /// <param name="node">A DOM node to transform.</param>
        /// <param name="token">A <see cref="CancellationToken"/> object.</param>
        /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
        public Task TransformAsync(XmlNode node, CancellationToken token)
        {
            return TransformAsync(node is XmlDocument ? node as XmlDocument : node.OwnerDocument, token);
        }
    }
}