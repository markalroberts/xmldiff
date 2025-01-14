using Microsoft.XmlDiffPatch;
using System;
using System.Diagnostics;
using System.IO;
using System.Xml;
using Xunit;

namespace UnitTests
{
    public class UnitTest1
    {
        [Fact]
        public void SimpleAddition()
        {
            XmlDocument doc1 = new XmlDocument();
            doc1.LoadXml("<a></a>");
            XmlDocument doc2 = new XmlDocument();
            doc2.LoadXml("<a><b/></a>");

            XmlDiff diff = new XmlDiff();

            var sw = new StringWriter();
            var settings = new XmlWriterSettings() { OmitXmlDeclaration = true, Indent = false };
            using (var writer = XmlWriter.Create(sw, settings))
            {
                Assert.False(diff.Compare(doc1, doc2, writer));                
            }

            XmlDocument result = new XmlDocument();
            result.LoadXml(sw.ToString());

            var inner = ToComparibleString(result);
            var expected = "<xd:xmldiff version=\"1.0\" options=\"None\" fragments=\"no\" xmlns:xd=\"http://schemas.microsoft.com/xmltools/2002/xmldiff\"><xd:node match=\"1\"><xd:add><b /></xd:add></xd:node></xd:xmldiff>";
            Assert.Equal(inner, expected);
        }

        [Fact]
        public void SimpleFragment()
        {
            using (var col = new TempFileCollection())
            {
                var file1 = col.CreateTempFile("<a></a><b></b>");
                var file2 = col.CreateTempFile("<a></a><b></b><c></c>");

                XmlDiff diff = new XmlDiff();

                var sw = new StringWriter();
                var settings = new XmlWriterSettings() { OmitXmlDeclaration = true, Indent = false };
                using (var writer = XmlWriter.Create(sw, settings))
                {
                    Assert.False(diff.Compare(file1, file2, true, writer));
                }

                XmlDocument result = new XmlDocument();
                result.LoadXml(sw.ToString());

                var inner = ToComparibleString(result);
                var expected = "<xd:xmldiff version=\"1.0\" options=\"None\" fragments=\"yes\" xmlns:xd=\"http://schemas.microsoft.com/xmltools/2002/xmldiff\"><xd:node match=\"2\" /><xd:add><c /></xd:add></xd:xmldiff>";
                Assert.Equal(inner, expected);
            }
        }

        [Fact]
        public void SideBySideDiffView()
        {
            using (var col = new TempFileCollection())
            {
                var xmlSource = col.CreateTempFile("<root><a></a><b><temp/></b></root>");
                var xmlChanged = col.CreateTempFile("<root><a id='1'></a><b></b><c></c></root>");

                var view = new XmlDiffView();
                var options = XmlDiffOptions.IgnoreChildOrder | XmlDiffOptions.IgnoreWhitespace;
                var results = view.DifferencesSideBySideAsHtml(xmlSource, xmlChanged, false, options);

                var diff = results.ReadToEnd();
                Assert.Contains("<span class=\"remove\">&lt;temp</span>", diff);
                Assert.Contains("<span class=\"add\">id=\"1\"</span>", diff);
                Assert.Contains("<span class=\"add\">&lt;c</span>", diff);
            }
        }

        private string ToComparibleString(XmlDocument doc)
        {
            // avoid comparing the hash.
            var s = doc.OuterXml;
            int pos = s.IndexOf("srcDocHash");
            int end = s.IndexOf("\"", pos + 12) + 2;
            s = s.Substring(0, pos) + s.Substring(end);
            return s;
        }
    }
}
