using System;
using System.IO;
using System.Text;
using System.Collections.Generic;
using System.Linq;
using AutoWebTranslator;
using AutoWebTranslator.Translators;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using Should;

namespace Tests.Unit
{
    [TestClass]
    public class TranslationFilterTests
    {
        private Mock<ITranslationProvider> _translationProvider;

        [TestInitialize]
        public void TestInitialize()
        {
            _translationProvider = new Mock<ITranslationProvider>();
            _translationProvider.Setup(x => x.Translate(It.IsAny<TranslationRequest>())).Callback(
                (TranslationRequest t) => t.Translations.ForEach(x => x.TargetText = "translated"));
        }

        [TestMethod]
        public void TextShouldBeTranslated()
        {
            const string html = "<html><body>test</body></html>";
            const string expected = "<html><body>translated</body></html>";
            TestTranslation(html, expected);
        }

        [TestMethod]
        public void TextInsideDivShouldBeTranslated()
        {
            const string html = "<html><body><div>test</div></body></html>";
            const string expected = "<html><body><div>translated</div></body></html>";
            TestTranslation(html, expected);
        }

        [TestMethod]
        public void TextLinkShouldBeTranslated()
        {
            const string html = "<html><body><a href=\"http://www.google.com\">test</a></body></html>";
            const string expected = "<html><body><a href=\"http://www.google.com\">translated</a></body></html>";
            TestTranslation(html, expected);
        }

        [TestMethod]
        public void TextForSubmitButtonShouldBeTranslated()
        {
            const string html = "<html><body><input type=\"submit\" value=\"submit it\" /></body></html>";
            const string expected = "<html><body><input type=\"submit\" value=\"translated\"></body></html>";
            TestTranslation(html, expected);
        }

        [TestMethod]
        public void TextForTextFieldShouldNotBeTranslated()
        {
            const string html = "<html><body><input type=\"text\" value=\"submit it\" /></body></html>";
            TestTranslation(html, html);
        }

        [TestMethod]
        public void ScriptShouldNotBeTranslated()
        {
            const string html = "<html><body><script>var test = 1;</script></body></html>";
            TestTranslation(html, html);
        }

        [TestMethod]
        public void StyleShouldNotBeTranslated()
        {
            const string html = "<html><body><style>body { color: black; }</style></body></html>";
            TestTranslation(html, html);
        }

        private void TestTranslation(string html, string expected)
        {
            var stream = new MemoryStream();
            var translationFilter = new TranslationFilter(stream, _translationProvider.Object, "en", "de");

            var bytes = Encoding.Default.GetBytes(html);
            translationFilter.Write(bytes, 0, bytes.Length);

            var translated = Encoding.Default.GetString(stream.ToArray());
            translated.ShouldEqual(expected);
        }
    }
}
