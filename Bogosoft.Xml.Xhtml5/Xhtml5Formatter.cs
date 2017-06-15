using System;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Xml;

namespace Bogosoft.Xml.Xhtml5
{
    /// <summary>
    /// A specialized derived type of <see cref="IFormatXml"/> suited to correctly formatting
    /// the XML serialization of HTML 5 (XHTML 5).
    /// </summary>
    public class Xhtml5Formatter : StandardXmlFormatter
    {
        /// <summary>
        /// Get an array of element names that indicate that their respective elements
        /// should not self-close.
        /// </summary>
        protected readonly static String[] ShouldNotSelfClose = new String[]
        {
            "b",
            "div",
            "i",
            "p",
            "script",
            "span",
            "td",
            "textarea"
        };

        /// <summary>
        /// Format an <see cref="XmlDocument"/> to a <see cref="TextWriter"/>.
        /// This derived class automatically places the doctype of the XHTML 5 document.
        /// </summary>
        /// <param name="document">An <see cref="XmlDocument"/> to format.</param>
        /// <param name="writer">A target <see cref="TextWriter"/> to format to.</param>
        /// <param name="token">A <see cref="CancellationToken"/> object.</param>
        /// <returns>
        /// A <see cref="Task"/> representing the asynchronous operation.
        /// </returns>
        protected override async Task FormatDocumentAsync(
            XmlDocument document,
            TextWriter writer,
            CancellationToken token
            )
        {
            if(document.DocumentElement?.Name == "html")
            {
                await writer.WriteAsync("<!DOCTYPE html>", token);
            }

            await base.FormatDocumentAsync(document, writer, token);
        }

        /// <summary>
        /// Format an <see cref="XmlDocumentType"/> to a <see cref="TextWriter"/>.
        /// This class ignores actual <see cref="XmlDocumentType"/> objects for formatting purposes.
        /// </summary>
        /// <param name="doctype">An <see cref="XmlDocumentType"/> to format.</param>
        /// <param name="writer">A target <see cref="TextWriter"/> to format to.</param>
        /// <param name="token">A <see cref="CancellationToken"/> object.</param>
        /// <returns>
        /// A <see cref="Task"/> representing the asynchronous operation.
        /// </returns>
        protected override Task FormatDocumentTypeAsync(
            XmlDocumentType doctype,
            TextWriter writer,
            CancellationToken token
            )
        {
            return writer.WriteAsync(String.Empty, token);
        }

        /// <summary>
        /// Format an <see cref="XmlElement"/> to a <see cref="TextWriter"/>. 
        /// </summary>
        /// <param name="element">An <see cref="XmlElement"/> to format.</param>
        /// <param name="writer">A target <see cref="TextWriter"/> to format to.</param>
        /// <param name="indent">An <see cref="String"/> representing the current indentation.</param>
        /// <param name="token">A <see cref="CancellationToken"/> object.</param>
        /// <returns>
        /// A <see cref="Task"/> representing the asynchronous operation.
        /// </returns>
        protected override async Task FormatElementAsync(
            XmlElement element,
            TextWriter writer,
            String indent,
            CancellationToken token
            )
        {
            if(!element.HasChildNodes && ShouldNotSelfClose.Contains(element.Name))
            {
                await writer.WriteAsync(LBreak + indent + "<" + element.Name, token);

                foreach(XmlAttribute a in element.Attributes)
                {
                    await FormatAttributeAsync(a, writer, indent, token);
                }

                await writer.WriteAsync("></" + element.Name + ">", token);
            }
            else
            {
                await base.FormatElementAsync(element, writer, indent, token);
            }
        }

        /// <summary>
        /// Format an <see cref="XmlDeclaration"/> to a <see cref="TextWriter"/>.  
        /// This class ignores actual <see cref="XmlDeclaration"/> objects for formatting purposes. 
        /// </summary>
        /// <param name="declaration">An <see cref="XmlDeclaration"/> to format.</param>
        /// <param name="writer">A target <see cref="TextWriter"/> to format to.</param>
        /// <param name="token">A <see cref="CancellationToken"/> object.</param>
        /// <returns>
        /// A <see cref="Task"/> representing the asynchronous operation.
        /// </returns>
        protected override Task FormatXmlDeclarationAsync(
            XmlDeclaration declaration,
            TextWriter writer,
            CancellationToken token
            )
        {
            return writer.WriteAsync(String.Empty, token);
        }
    }
}