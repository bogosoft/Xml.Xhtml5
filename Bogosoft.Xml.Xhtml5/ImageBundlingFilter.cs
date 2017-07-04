using Bogosoft.Mapping;
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
    /// An implementation of the <see cref="IFilterXml"/> contract which bundles images.
    /// </summary>
    public class ImageBundlingFilter : IFilterXml
    {
        /// <summary>
        /// Get or set the mapping strategy responsible for generating an absolute filepath
        /// against a collection of source URI's.
        /// </summary>
        protected Mapper<IEnumerable<string>, string> BundledFilepathMapper;

        /// <summary>
        /// Get or set a document finalizer to invoke once bundling has finished.
        /// </summary>
        protected Action<XmlDocument> Finalizer;

        /// <summary>
        /// Get or set the mapping strategy responsible for generating an ID against a relative URI.
        /// </summary>
        protected Mapper<string, string> IdMapper;

        /// <summary>
        /// Get or set the mapping strategy responsible for converting a physical path on the
        /// local filesystem to a relative URI.
        /// </summary>
        protected Mapper<string, string> PhysicalPathToRelativeUriMapper;

        /// <summary>
        /// Get or set the mapping strategy responsible for converting a relative URI into a
        /// physical path on the local filesystem.
        /// </summary>
        protected Mapper<string, string> RelativeUriToPhysicalPathMapper;

        /// <summary>
        /// Create a new instance of the <see cref="ImageBundlingFilter"/> class.
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
        public ImageBundlingFilter(
            Mapper<string, string> physicalPathToRelativeUriMapper,
            Mapper<string, string> relativeUriToPhysicalPathMapper,
            Mapper<IEnumerable<string>, string> bundledFilepathMapper,
            Mapper<string, string> idMapper,
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
        /// Filter a given XML document.
        /// </summary>
        /// <param name="document">A document to filter.</param>
        /// <param name="token">A <see cref="CancellationToken"/> object.</param>
        /// <returns>
        /// A <see cref="Task"/> representing the asynchronous operation.
        /// </returns>
        public async Task FilterAsync(XmlDocument document, CancellationToken token)
        {
            var body = document.SelectNodes("/html/body").Cast<XmlElement>().FirstOrDefault();

            var nodes = document.SelectNodes("//img").Cast<XmlElement>().Where(x => x.HasAttribute("src"));

            var targets = new List<Image>();

            string filepath, uri;

            XmlElement last = null;

            foreach(var node in nodes)
            {
                uri = node.GetAttribute("src");

                filepath = RelativeUriToPhysicalPathMapper.Map(uri);

                if (!File.Exists(filepath))
                {
                    continue;
                }

                last = node;

                node.RemoveAttribute("src");

                var target = new Image
                {
                    Id = IdMapper.Map(uri),
                    PhysicalPath = RelativeUriToPhysicalPathMapper.Map(uri),
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

            var cachepath = BundledFilepathMapper.Map(targets.Select(x => x.RelativeUri));

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

            script.SetAttribute("src", PhysicalPathToRelativeUriMapper.Map(cachepath));

            Finalizer.Invoke(document);
        }
    }
}