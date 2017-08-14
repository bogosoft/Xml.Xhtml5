using Bogosoft.Hashing;
using Bogosoft.Hashing.Cryptography;
using NUnit.Framework;
using Should;
using System;
using System.Collections.Generic;
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

        static string TempPath = Environment.GetEnvironmentVariable("TEMP");

        [TestCase]
        public async Task CssBundlingTransformerWorksAsExpected()
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

            var xformer = new CssBundlingTransformer(ToRelativeUri, ToPhysicalPath, ToBundledCssFilepath);

            formatter.Transformers.Add(xformer);

            test.Clear();

            test.Append($@"<!DOCTYPE html><html><head><link href=""");

            test.Append(ToRelativeUri(ToBundledCssFilepath(files)));

            test.Append(@""" rel=""stylesheet""/></head></html>");

            test.ToString().ShouldEqual(await formatter.ToStringAsync(document));
        }

        [TestCase]
        public async Task ImageBundlingTransformerWorksAsExpected()
        {
            var files = new[] { "one.jpeg", "two.png", "three.bmp", "four.gif" };

            foreach (var x in files.Select(ToAbsolutePath))
            {
                if (File.Exists(x))
                {
                    File.Delete(x);
                }

                using (var stream = new FileStream(x, FileMode.Create, FileAccess.Write))
                using (var writer = new StreamWriter(stream))
                {
                    await writer.WriteAsync("Not really binary image data.");
                }
            }

            var test = new StringBuilder("<!DOCTYPE html><html><body>");

            var document = new XmlDocument();

            var body = document.AppendElement("html").AppendElement("body");

            foreach (var x in files)
            {
                body.AppendElement("img").SetAttribute("src", x);

                test.Append($@"<img src=""{x}""/>");
            }

            test.Append("</body></html>");

            var formatter = new Xhtml5Formatter();

            test.ToString().ShouldEqual(await formatter.ToStringAsync(document));

            var xformer = new ImageBundlingTransformer(
                ToRelativeUri,
                ToPhysicalPath,
                ToBundledJsFilepath,
                ToId,
                FinalizeImageBundledDocument
                );

            formatter.Transformers.Add(xformer);

            test.Clear();

            test.Append("<!DOCTYPE html><html><body>");

            foreach(var x in files)
            {
                test.Append($@"<img data-id=""{ToId(x)}""/>");
            }

            var path = ToRelativeUri(ToBundledJsFilepath(files));

            test.Append($@"<script src=""{path}""></script>");

            test.Append(@"<script type=""text/javascript"">/* Rehydrate */</script></body></html>");

            var actual = await formatter.ToStringAsync(document);

            var expected = test.ToString();

            expected.ShouldEqual(actual);
        }

        [TestCase]
        public async Task JavaScriptBundlingTransformerCanBundleAtLeastTwoFiles()
        {
            var files = new[] { "one.js", "two.js" };

            foreach(var x in files.Select(ToAbsolutePath))
            {
                if (File.Exists(x))
                {
                    File.Delete(x);
                }

                using (var stream = new FileStream(x, FileMode.Create, FileAccess.Write))
                using (var writer = new StreamWriter(stream))
                {
                    await writer.WriteAsync("// I am a comment.\r\n");
                }
            }

            var test = new StringBuilder("<!DOCTYPE html><html><head>");

            var document = new XmlDocument();

            var head = document.AppendElement("html").AppendElement("head");

            foreach(var x in files)
            {
                head.AppendElement("script").SetAttribute("src", x);

                test.Append($@"<script src=""{x}""></script>");
            }

            test.Append("</head></html>");

            var formatter = new Xhtml5Formatter();

            test.ToString().ShouldEqual(await formatter.ToStringAsync(document));

            var xformer = new JavascriptBundlingTransformer(
                ToRelativeUri,
                ToPhysicalPath,
                ToBundledJsFilepath,
                "/html/head"
                );

            formatter.Transformers.Add(xformer);

            test.Clear();

            test.Append($@"<!DOCTYPE html><html><head><script src=""");

            test.Append(ToRelativeUri(ToBundledJsFilepath(files)));

            test.Append(@"""></script></head></html>");

            test.ToString().ShouldEqual(await formatter.ToStringAsync(document));
        }

        [TestCase]
        public async Task RemoteFileCachingTransformerWorksAsExpected()
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

            var xformer = new RemoteFileCachingTransformer(PhysicalCachePath, VirtualCachePath, "/html/head/link/@href");

            formatter.Transformers.Add(xformer);

            var transformed = await formatter.ToStringAsync(document);

            File.Exists(filepath).ShouldBeTrue();

            var uri = $"{VirtualCachePath}/{Path.GetFileName(Bootstrap)}";

            transformed.ShouldEqual($@"<!DOCTYPE html><html><head><link href=""{uri}""/></head></html>");
        }

        static void FinalizeImageBundledDocument(XmlDocument document)
        {
            var body = document.SelectNodes("/html/body").Cast<XmlElement>().FirstOrDefault();

            var script = document.CreateElement("script");

            body.AppendChild(script);

            script.SetAttribute("type", "text/javascript");

            script.AppendChild(document.CreateTextNode("/* Rehydrate */"));
        }

        static string ToAbsolutePath(string filename)
        {
            return Path.Combine(PhysicalCachePath, filename);
        }

        static string ToBundledCssFilepath(IEnumerable<string> uris)
        {
            return ToBundledFilepath(uris, "css");
        }

        static string ToBundledFilepath(IEnumerable<string> uris, string extension)
        {
            var hash = CryptoHashStrategy.MD5.Compute(uris).ToHexString();

            return Path.Combine(TempPath, $"{hash}.{extension}");
        }

        static string ToBundledJsFilepath(IEnumerable<string> uris)
        {
            return ToBundledFilepath(uris, "js");
        }

        static string ToId(string uri)
        {
            return CryptoHashStrategy.MD5.Compute(uri).ToHexString();
        }

        static string ToPhysicalPath(string uri)
        {
            return Path.Combine(Environment.GetEnvironmentVariable("TEMP"), uri);
        }

        static string ToRelativeUri(string filepath)
        {
            return $"content/cached/{Path.GetFileName(filepath)}";
        }
    }
}