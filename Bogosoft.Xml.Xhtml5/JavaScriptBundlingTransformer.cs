using System;
using System.Collections.Generic;
using System.Xml;

namespace Bogosoft.Xml.Xhtml5
{
    /// <summary>
    /// An XHTML5 transformation strategy that bundles multiple JavaScript files into a single file.
    /// </summary>
    public class JavascriptBundlingTransformer : BundlingTransformerBase
    {
        string targettedContainerXPath;

        /// <summary>
        /// Get the name of the attribute on elements of the <see cref="TargettedElement"/> type
        /// which contains the actual reference to a resource to bundle.
        /// </summary>
        protected override string TargettedAttribute => "src";

        /// <summary>
        /// Get the XPath expression that will be used to gather all candidate elements for bundling.
        /// </summary>
        protected override string TargettedContainerXPath => targettedContainerXPath;

        /// <summary>
        /// Get the name of the element corresponding to a resource to bundle.
        /// </summary>
        protected override string TargettedElement => "script";

        /// <summary>
        /// Create a new instance of the <see cref="JavascriptBundlingTransformer"/> class.
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
        /// <param name="targettedContainerXPath">
        /// An XPath expression that targets the element containing all script element to bundle.
        /// </param>
        public JavascriptBundlingTransformer(
            Converter<string, string> physicalPathToRelativeUriMapper,
            Converter<string, string> relativeUriToPhysicalPathMapper,
            Converter<IEnumerable<string>, string> bundledFilepathMapper,
            string targettedContainerXPath
            )
            : base(physicalPathToRelativeUriMapper, relativeUriToPhysicalPathMapper, bundledFilepathMapper)
        {
            this.targettedContainerXPath = targettedContainerXPath;
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
            var script = container.OwnerDocument.CreateElement("script");

            container.AppendChild(script);

            script.SetAttribute("src", uri);
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
            return element.HasAttribute("src") && element.GetAttribute("src").EndsWith(".js");
        }
    }
}