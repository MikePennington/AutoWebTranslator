using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using AutoWebTranslator.Translators;
using HtmlAgilityPack;

namespace AutoWebTranslator
{
    public class TranslationFilter : MemoryStream
    {
        private static readonly List<string> ExcludeTags = new List<string> { "script", "style" };
        
        private readonly Stream _outputStream;
        private readonly ITranslationProvider _translationProvider;
        private readonly StringBuilder _responseHtml;
        private readonly string _sourceLanguage;
        private readonly string _targetLanguage;

        public TranslationFilter(Stream output, ITranslationProvider translationProvider,
            string sourceLanguage, string targetLanguage)
        {
            _outputStream = output;
            _translationProvider = translationProvider;
            _sourceLanguage = sourceLanguage.ToLower();
            _targetLanguage = targetLanguage.ToLower();
            _responseHtml = new StringBuilder();
        }

        public override void Write(byte[] buffer, int offset, int count)
        {
            // No need to do any filtering if languages are the same
            if (_sourceLanguage == _targetLanguage)
            {
                _outputStream.Write(buffer, offset, count);
                return;
            }

            var html = Encoding.UTF8.GetString(buffer);
            _responseHtml.Append(html);

            // Wait for the closing </html> tag
            var eof = new Regex("</html>", RegexOptions.IgnoreCase);
            if (eof.IsMatch(html))
            {
                string translatedHtml = TranslateAll(_responseHtml.ToString());
                _outputStream.Write(Encoding.UTF8.GetBytes(translatedHtml), offset, Encoding.UTF8.GetByteCount(translatedHtml));
            }
        }

        private string Translate(string html)
        {
            if (_sourceLanguage == _targetLanguage)
                return html;

            var doc = new HtmlDocument();
            doc.LoadHtml(html);
            HtmlNodeCollection textNodes = doc.DocumentNode.SelectNodes("//text()");

            foreach (var textNode in textNodes)
            {
                string text = textNode.InnerHtml;
                if (string.IsNullOrWhiteSpace(text))
                    continue;
                if (ExcludeTags.Contains(textNode.ParentNode.Name.ToLower()))
                    continue;

                var trans = new TranslationRequest(_sourceLanguage, _targetLanguage);
                trans.Translations.Add(new Translation(textNode.InnerHtml));
                _translationProvider.Translate(trans);
                textNode.InnerHtml = trans.Translations[0].TargetText;
            }

            return doc.DocumentNode.InnerHtml;
        }

        private string TranslateAll(string html)
        {
            html = TranslateNodes(html, "//text()");
            html = TranslateNodes(html, "//input[@type='submit']");
            return html;
        }

        private string TranslateNodes(string html, string xpath)
        {
            if (_sourceLanguage == _targetLanguage)
                return html;

            var doc = new HtmlDocument();
            doc.LoadHtml(html);
            HtmlNodeCollection nodes = doc.DocumentNode.SelectNodes(xpath);
            if (nodes == null)
                return html;

            var translationRequest = new TranslationRequest(_sourceLanguage, _targetLanguage);

            foreach (var node in nodes)
            {
                if (!ShouldTranslateNode(node))
                    continue;

                translationRequest.Translations.Add(new Translation(FindTextToTranslate(node)));
            }

            // De-dupe list to minimize calls
            translationRequest.Translations = translationRequest.Translations.Distinct().ToList();

            _translationProvider.Translate(translationRequest);

            foreach (var node in nodes)
            {
                if (!ShouldTranslateNode(node))
                    continue;

                Translation t = translationRequest.Translations.FirstOrDefault(x => x.SourceText == FindTextToTranslate(node));
                if (t != null)
                    WriteTranslatedText(node, t.TargetText);
            }

            return doc.DocumentNode.InnerHtml;
        }

        private bool ShouldTranslateNode(HtmlNode node)
        {
            if (node.NodeType == HtmlNodeType.Text)
            {
                string text = node.InnerHtml;
                if (string.IsNullOrWhiteSpace(text))
                    return false;
                if (ExcludeTags.Contains(node.ParentNode.Name.ToLower()))
                    return false;
                return true;
            }
            else if (node.NodeType == HtmlNodeType.Element)
            {
                if(node.Name.ToLower() == "input")
                {
                    var value = node.GetAttributeValue("value", null);
                    return !string.IsNullOrWhiteSpace(value);
                }
                return false;
            }
            return false;
        }

        public string FindTextToTranslate(HtmlNode node)
        {
            if (node.NodeType == HtmlNodeType.Text)
            {
                string text = node.InnerHtml;
                if (string.IsNullOrWhiteSpace(text))
                    return null;
                if (ExcludeTags.Contains(node.ParentNode.Name.ToLower()))
                    return null;
                return text;
            }
            else if (node.NodeType == HtmlNodeType.Element)
            {
                if (node.Name.ToLower() == "input")
                {
                    var value = node.GetAttributeValue("value", null);
                    return string.IsNullOrWhiteSpace(value) ? null : value;
                }
                return null;
            }
            return null;
        }

        private void WriteTranslatedText(HtmlNode node, string text)
        {
            if (node.NodeType == HtmlNodeType.Text)
            {
                node.InnerHtml = text;
            }
            else if (node.NodeType == HtmlNodeType.Element && node.Name.ToLower() == "input")
            {
                node.SetAttributeValue("value", text);
            }
        }
    }
}
