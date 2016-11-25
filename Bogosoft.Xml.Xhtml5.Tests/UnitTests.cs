using NUnit.Framework;
using Should;
using System.Threading.Tasks;
using System.Xml;

namespace Bogosoft.Xml.Tests
{
    [TestFixture, Category("Unit")]
    public class Xhtml5FormatterTests
    {
        [TestCase]
        public async Task AutomaticallyAddsHtml5Doctype()
        {
            var document = new XmlDocument();

            document.AppendChild(document.CreateElement("html"));

            var formatted = await new Xhtml5Formatter().ToStringAsync(document);

            formatted.ShouldEqual("<!DOCTYPE html><html/>");
        }

        [TestCase]
        public async Task ClosesTagsThatAreAllowedToSelfClose()
        {
            var document = new XmlDocument();

            document.AppendElement("html")
                    .AppendElement("head")
                    .AppendElement("meta")
                    .ParentNode.As<XmlElement>()
                    .AppendElement("script");

            var formatted = await new Xhtml5Formatter().ToStringAsync(document);

            formatted.ShouldEqual("<!DOCTYPE html><html><head><meta/><script></script></head></html>");
        }

        [TestCase]
        public async Task DoesNotCloseElementsThatShouldNotSelfClose()
        {
            var document = new XmlDocument();

            document.AppendElement("html").AppendElement("head").AppendElement("script");

            var formatted = await new Xhtml5Formatter().ToStringAsync(document);

            formatted.ShouldEqual("<!DOCTYPE html><html><head><script></script></head></html>");
        }

        [TestCase]
        public async Task FormatsCorrectly()
        {
            var document = new XmlDocument();

            var meta = document.AppendElement("html").AppendElement("head").AppendElement("meta");

            meta.SetAttribute("charset", "utf-8");

            var formatter = new Xhtml5Formatter { Indent = "    ", LBreak = "\r\n" };

            var formatted = await formatter.ToStringAsync(document);

            formatted.ShouldEqual(@"<!DOCTYPE html>
<html>
    <head>
        <meta charset=""utf-8""/>
    </head>
</html>");
        }
    }
}