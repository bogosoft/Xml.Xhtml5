﻿using Bogosoft.Hashing;
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

            var filter = new CssBundlingFilter(ToRelativeUri, ToPhysicalPath, ToBundledCssFilepath);

            formatter.With(filter);

            test.Clear();

            test.Append($@"<!DOCTYPE html><html><head><link href=""");

            test.Append(ToRelativeUri(ToBundledCssFilepath(files)));

            test.Append(@""" rel=""stylesheet""/></head></html>");

            test.ToString().ShouldEqual(await formatter.ToStringAsync(document));
        }

        [TestCase]
        public async Task JavaScriptBundlingFilterCanBundleAtLeastTwoFiles()
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

            var filter = new JavascriptBundlingFilter(
                ToRelativeUri,
                ToPhysicalPath,
                ToBundledJsFilepath,
                "/html/head"
                );

            formatter.With(filter);

            test.Clear();

            test.Append($@"<!DOCTYPE html><html><head><script src=""");

            test.Append(ToRelativeUri(ToBundledJsFilepath(files)));

            test.Append(@"""></script></head></html>");
            var expected = test.ToString();
            var actual = await formatter.ToStringAsync(document);
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

        static string ToPhysicalPath(string uri)
        {
            return Path.Combine(Environment.GetEnvironmentVariable("TEMP"), uri);
        }

        static string ToRelativeUri(string filepath)
        {
            return $"{VirtualCachePath}/{Path.GetFileName(filepath)}";
        }
    }
}