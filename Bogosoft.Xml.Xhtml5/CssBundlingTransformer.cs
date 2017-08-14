using System.Collections.Generic;
using System;
using System.Xml;

namespace Bogosoft.Xml.Xhtml5
{
    /// <summary>
    /// An XHTML5 transformation strategy that bundles multiple CSS files into a single file.
    /// </summary>
    public class CssBundlingTransformer : BundlingTransformerBase
    {
        /// <summary>
        /// Get the name of the attribute on elements of the <see cref="TargettedElement"/> type
        /// which contains the actual reference to a resource to bundle.
        /// </summary>
        protected override string TargettedAttribute => "href";

        /// <summary>
        /// Get the XPath expression that will be used to gather all candidate elements for bundling.
        /// </summary>
        protected override string TargettedContainerXPath => "/html/head";

        /// <summary>
        /// Get the name of the element corresponding to a resource to bundle.
        /// </summary>
        protected override string TargettedElement => "link";

        /// <summary>
        /// Create a new instance of the <see cref="CssBundlingTransformer"/> class.
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
        public CssBundlingTransformer(
            Converter<string, string> physicalPathToRelativeUriMapper,
            Converter<string, string> relativeUriToPhysicalPathMapper,
            Converter<IEnumerable<string>, string> bundledFilepathMapper
            )
            : base(physicalPathToRelativeUriMapper, relativeUriToPhysicalPathMapper, bundledFilepathMapper)
        {
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
        protected override void AppendReplacementElement(XmlElement container, string uri)
        {
            var link = container.OwnerDocument.CreateElement("link");

            container.AppendChild(link);

            link.SetAttribute("href", uri);
            link.SetAttribute("rel", "stylesheet");
        }

        /// <summary>
        /// Qualify whether a given element is to be included in the bundle.
        /// </summary>
        /// <param name="element">An element to qualify for inclusion.</param>
        /// <returns>
        /// A value indicating whether or not the given element qualifies for bundling.
        /// </returns>
        protected override bool Qualified(XmlElement element)
        {
            return element.HasAttribute("href") && element.GetAttribute("href").EndsWith(".css");
        }
    }
}