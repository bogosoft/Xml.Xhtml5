using System;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Xml;

namespace Bogosoft.Xml
{
    /// <summary>
    /// A specialized derived type of <see cref="XmlFormatter"/> suited to correctly formatting
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
            "textarea"
        };

        /// <summary>
        /// Format an <see cref="XmlDocument"/> to a <see cref="TextWriter"/>.
        /// This derived class automatically places the doctype of the XHTML 5 document.
        /// </summary>
        /// <param name="document">An <see cref="XmlDocument"/> to format.</param>
        /// <param name="writer">A target <see cref="TextWriter"/> to format to.</param>
        /// <param name="indent">An <see cref="String"/> representing the current indentation.</param>
        /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
        protected override async Task FormatDocumentAsync(
            XmlDocument document,
            TextWriter writer,
            CancellationToken token
            )
        {
            await writer.WriteAsync("<!DOCTYPE html>");

            await base.FormatDocumentAsync(document, writer, token);
        }

        /// <summary>
        /// Format an <see cref="XmlDocumentType"/> to a <see cref="TextWriter"/>.
        /// This class ignores actual <see cref="XmlDocumentType"/> objects for formatting purposes.
        /// </summary>
        /// <param name="doctype">An <see cref="XmlDocumentType"/> to format.</param>
        /// <param name="writer">A target <see cref="TextWriter"/> to format to.</param>
        /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
        protected override Task FormatDocumentTypeAsync(
            XmlDocumentType doctype,
            TextWriter writer,
            CancellationToken token
            )
        {
            token.ThrowIfCancellationRequested();

            return writer.WriteAsync(String.Empty);
        }

        /// <summary>
        /// Format an <see cref="XmlElement"/> to a <see cref="TextWriter"/>. 
        /// </summary>
        /// <param name="element">An <see cref="XmlElement"/> to format.</param>
        /// <param name="writer">A target <see cref="TextWriter"/> to format to.</param>
        /// <param name="indent">An <see cref="String"/> representing the current indentation.</param>
        /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
        protected override async Task FormatElementAsync(
            XmlElement element,
            TextWriter writer,
            String indent,
            CancellationToken token
            )
        {
            if(!element.HasChildNodes && ShouldNotSelfClose.Contains(element.Name))
            {
                await writer.WriteAsync("<" + element.Name);

                foreach(XmlAttribute a in element.Attributes)
                {
                    await this.FormatAttributeAsync(a, writer, indent, token);
                }

                await writer.WriteAsync("></" + element.Name + ">");
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
        /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
        protected override Task FormatXmlDeclarationAsync(
            XmlDeclaration declaration,
            TextWriter writer,
            CancellationToken token
            )
        {
            token.ThrowIfCancellationRequested();

            return writer.WriteAsync(String.Empty);
        }
    }
}