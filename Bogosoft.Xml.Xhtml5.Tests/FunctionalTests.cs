using Bogosoft.Hashing;
using Bogosoft.Hashing.Cryptography;
using NUnit.Framework;
using Should;
using System;
using System.IO;
using System.Linq;
using System.Text;
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
        public async Task CssBundlingFilterWorksAsExpected()
        {
            var files = new[] { "one.css", "two.css", "three.css", "four.css" };

            foreach(var x in files.Select(ToAbsolutePath))
            {
                if (File.Exists(x))
                {
                    File.Delete(x);
                }

                using (var stream = new FileStream(x, FileMode.Create, FileAccess.Write))
                using (var writer = new StreamWriter(stream))
                {
                    await writer.WriteAsync("html{margin:0;}");
                }
            }

            var test = new StringBuilder("<!DOCTYPE html><html><head>");

            var document = new XmlDocument();

            var head = document.AppendElement("html").AppendElement("head");

            foreach(var x in files)
            {
                head.AppendElement("link").SetAttribute("href", x);

                test.Append($@"<link href=""{x}""/>");
            }

            test.Append("</head></html>");

            var formatter = new Xhtml5Formatter();

            test.ToString().ShouldEqual(await formatter.ToStringAsync(document));

            var algorithm = CryptoHashStrategy.MD5;

            var filter = new CssBundlingFilter(
                PhysicalCachePath,
                VirtualCachePath,
                PhysicalCachePath,
                algorithm
                );

            formatter.With(filter);

            test.Clear();

            test.Append($@"<!DOCTYPE html><html><head><link href=""{VirtualCachePath}/");

            test.Append(algorithm.Compute(files).ToHexString());

            test.Append(@".css"" rel=""stylesheet""/></head></html>");

            test.ToString().ShouldEqual(await formatter.ToStringAsync(document));
        }

        [TestCase]
        public async Task RemoteFileCachingFilterWorksAsExpected()
        {
            var filepath = Path.Combine(PhysicalCachePath, Path.GetFileName(Bootstrap));

            if (File.Exists(filepath))
            {
                File.Delete(filepath);
            }

            var document = new XmlDocument();

            var html = document.AppendElement("html");

            var head = html.AppendElement("head");

            var link = head.AppendElement("link");

            link.SetAttribute("href", Bootstrap);

            var formatter = new Xhtml5Formatter();

            var unfiltered = await formatter.ToStringAsync(document);

            File.Exists(filepath).ShouldBeFalse();

            unfiltered.ShouldEqual($@"<!DOCTYPE html><html><head><link href=""{Bootstrap}""/></head></html>");

            var filter = new RemoteFileCachingFilter(PhysicalCachePath, VirtualCachePath, "/html/head/link/@href");

            formatter.With(filter);

            var filtered = await formatter.ToStringAsync(document);

            File.Exists(filepath).ShouldBeTrue();

            var uri = $"{VirtualCachePath}/{Path.GetFileName(Bootstrap)}";

            filtered.ShouldEqual($@"<!DOCTYPE html><html><head><link href=""{uri}""/></head></html>");
        }

        static string ToAbsolutePath(string filename)
        {
            return Path.Combine(PhysicalCachePath, filename);
        }
    }
}