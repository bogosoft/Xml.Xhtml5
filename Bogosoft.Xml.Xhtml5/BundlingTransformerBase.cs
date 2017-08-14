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
    /// An implementation of the <see cref="IDomTransformer"/> contract which bundles the contents of similar
    /// files together, removes their references in the associated XHTML document, and adds a single reference
    /// to the bundled file. The bundled file is cached locally
    /// </summary>
    public abstract class BundlingTransformerBase : IDomTransformer
    {
        /// <summary>
        /// Get or set the mapping strategy responsible for generating an absolute filepath
        /// against a collection of source URI's.
        /// </summary>
        protected Mapper<IEnumerable<string>, string> BundledFilepathMapper;

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
        /// Get the name of the attribute on elements of the <see cref="TargettedElement"/> type
        /// which contains the actual reference to a resource to bundle.
        /// </summary>
        protected abstract string TargettedAttribute { get; }

        /// <summary>
        /// Get the XPath expression that will be used to gather all candidate elements for bundling.
        /// </summary>
        protected abstract string TargettedContainerXPath { get; }

        /// <summary>
        /// Get the name of the element corresponding to a resource to bundle.
        /// </summary>
        protected abstract string TargettedElement { get; }

        /// <summary>
        /// Create a new instance of the <see cref="BundlingTransformerBase"/> class.
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
        protected BundlingTransformerBase(
            Mapper<string, string> physicalPathToRelativeUriMapper,
            Mapper<string, string> relativeUriToPhysicalPathMapper,
            Mapper<IEnumerable<string>, string> bundledFilepathMapper
            )
        {
            BundledFilepathMapper = bundledFilepathMapper;
            PhysicalPathToRelativeUriMapper = physicalPathToRelativeUriMapper;
            RelativeUriToPhysicalPathMapper = relativeUriToPhysicalPathMapper;
        }

        /// <summary>
        /// Append an element representing the bundled resource to a given container.
        /// </summary>
        /// <param name="container">
        /// The containing element which the new element will be appended to as a child.
        /// </param>
        /// <param name="uri">
        /// A value corresponding to the relative URI of the bundled resource.
        /// </param>
        protected abstract void AppendReplacementElement(XmlElement container, string uri);

        /// <summary>
        /// Filter a given XML document.
        /// </summary>
        /// <param name="node">A node to filter.</param>
        /// <param name="token">A <see cref="CancellationToken"/> object.</param>
        /// <returns>
        /// A <see cref="Task"/> representing the asynchronous operation.
        /// </returns>
        public async Task TransformAsync(XmlNode node, CancellationToken token)
        {
            var container = node.GetOwnerDocument()
                                .SelectNodes(TargettedContainerXPath)
                                .Cast<XmlElement>()
                                .FirstOrDefault();

            var targets = container.SelectNodes(TargettedElement)
                                   .Cast<XmlNode>()
                                   .Where(x => x.NodeType == XmlNodeType.Element)
                                   .Select(x => x as XmlElement)
                                   .Where(Qualified);

            var locations = new List<string>();

            string uri;

            XmlNode last = null;

            foreach(var target in targets.Where(x => x.HasAttribute(TargettedAttribute)))
            {
                if (target.HasAttribute("data-bundle"))
                {
                    target.RemoveAttribute("data-bundle");

                    if (target.GetAttribute("data-bundle").Equals("false", StringComparison.OrdinalIgnoreCase))
                    {
                        continue;
                    }
                }

                uri = target.GetAttribute(TargettedAttribute);

                locations.Add(uri);

                last = container.RemoveChild(target);
            }

            if(locations.Count < 2)
            {
                if(locations.Count == 1)
                {
                    container.AppendChild(last);
                }

                return;
            }

            var filepath = BundledFilepathMapper.Invoke(locations);

            AppendReplacementElement(container, PhysicalPathToRelativeUriMapper.Invoke(filepath));

            if (File.Exists(filepath))
            {
                return;
            }

            string srcpath;

            using (var output = new FileStream(filepath, FileMode.Create, FileAccess.Write))
            {
                foreach(var x in locations)
                {
                    srcpath = RelativeUriToPhysicalPathMapper.Invoke(x);

                    using (var source = new FileStream(srcpath, FileMode.Open, FileAccess.Read))
                    {
                        await source.CopyToAsync(output, token);
                    }
                }
            }
        }

        /// <summary>
        /// Qualify whether a given element is to be included in the bundle.
        /// </summary>
        /// <param name="element">An element to qualify for inclusion.</param>
        /// <returns>
        /// A value indicating whether or not the given element qualifies for bundling.
        /// </returns>
        protected abstract bool Qualified(XmlElement element);
    }
}