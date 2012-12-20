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
        private static readonly List<string> ExcludeTags = new List<string> { "script", "style", "link" };
        
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
            if (_sourceLanguage == _targetLanguage)
                return html;

            var doc = new HtmlDocument();
            doc.LoadHtml(html);
            HtmlNodeCollection textNodes = doc.DocumentNode.SelectNodes("//text()");

            var trans = new TranslationRequest(_sourceLanguage, _targetLanguage);

            foreach (var textNode in textNodes)
            {
                if (!TranslateNode(textNode))
                    continue;

                trans.Translations.Add(new Translation(textNode.InnerHtml));
            }

            // De-dupe list to minimize calls
            trans.Translations = trans.Translations.Distinct().ToList();

            _translationProvider.Translate(trans);

            foreach (var textNode in textNodes)
            {
                if (!TranslateNode(textNode))
                    continue;

                Translation t = trans.Translations.FirstOrDefault(x => x.SourceText == textNode.InnerHtml);
                if(t != null)
                    textNode.InnerHtml = t.TargetText;
            }

            return doc.DocumentNode.InnerHtml;
        }

        private bool TranslateNode(HtmlNode textNode)
        {
            string text = textNode.InnerHtml;
            if (string.IsNullOrWhiteSpace(text))
                return false;
            if (ExcludeTags.Contains(textNode.ParentNode.Name.ToLower()))
                return false;
            return true;
        }
    }
}
