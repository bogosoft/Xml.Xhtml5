using NUnit.Framework;
using Should;
using System;
using System.IO;
using System.Threading.Tasks;
using System.Xml;

namespace Bogosoft.Xml.Xhtml5.Tests
{
    [TestFixture, Category("Functional")]
    public class FunctionalTests
    {
        const string Bootstrap = "https://maxcdn.bootstrapcdn.com/bootstrap/4.0.0-alpha.6/css/bootstrap.min.css";

        static string PhysicalCachePath => Environment.GetEnvironmentVariable("TEMP");

        const string VirtualCachePath = "content/cached";

        [TestCase]
        public async Task RemoteFileCachingFilterWorksAsExpected()
        {
            var document = new XmlDocument();

            var html = document.AppendElement("html");

            var head = html.AppendElement("head");

            var link = head.AppendElement("link");

            link.SetAttribute("href", Bootstrap);

            var formatter = new Xhtml5Formatter();

            var unfiltered = await formatter.ToStringAsync(document);

            unfiltered.ShouldEqual($@"<!DOCTYPE html><html><head><link href=""{Bootstrap}""/></head></html>");

            var filter = new RemoteFileCachingFilter(PhysicalCachePath, VirtualCachePath, "/html/head/link/@href");

            formatter.With(filter);

            var filtered = await formatter.ToStringAsync(document);

            var uri = $"{VirtualCachePath}/{Path.GetFileName(Bootstrap)}";

            filtered.ShouldEqual($@"<!DOCTYPE html><html><head><link href=""{uri}""/></head></html>");
        }
    }
}