using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;
using System.Web;
using Newtonsoft.Json.Linq;

namespace AutoWebTranslator.Translators
{
    public class GoogleTranslationProvider : ITranslationProvider
    {
        private static readonly Regex WhitespaceStart = new Regex(@"^[\s]+");
        private static readonly Regex WhitespaceEnd = new Regex(@"[\s]+$");
        private readonly string _apiKey;
        private const int MaxNumSegments = 128;
        private const int MaxUrlLength = 1800;

        public GoogleTranslationProvider(string apiKey)
        {
            _apiKey = apiKey;
        }

        public string ProviderName
        {
            get { return "Google"; }
        }

        public void Translate(TranslationRequest trans)
        {
            var baseUrl = "https://www.googleapis.com/language/translate/v2?key=" + _apiKey +
                "&source=" + trans.SourceLanguage + "&target=" + trans.TargetLanguage;

            var url = new StringBuilder(baseUrl);
            int lastTranslatedIndex = 0;
            for (int i = 0; i < trans.Translations.Count; i++)
            {
                AddTextToUrl(url, trans.Translations[i].SourceText);

                // All this craziness is here because we want to add as many segments to the URL as poassible
                // without going over the max length
                var nextUrl = new StringBuilder(url.ToString());
                if (i < trans.Translations.Count - 1)
                    AddTextToUrl(nextUrl, trans.Translations[i+1].SourceText);

                if ((i - lastTranslatedIndex) >= MaxNumSegments 
                    || i == (trans.Translations.Count - 1)
                    || (nextUrl.Length > MaxUrlLength))
                {
                    var translations = trans.Translations.GetRange(lastTranslatedIndex, i - lastTranslatedIndex + 1);
                    CallGoogle(url.ToString(), translations);
                    url = new StringBuilder(baseUrl);
                    lastTranslatedIndex = i + 1;
                    AddWhitespaceBackIn(translations);
                }
            }
        }

        private void CallGoogle(string url, List<Translation> translations)
        {
            try
            {
                var request = WebRequest.Create(url) as HttpWebRequest;
                if (request == null)
                    return;

                using (var response = request.GetResponse() as HttpWebResponse)
                {
                    if (response == null)
                        return;

                    if (response.StatusCode != HttpStatusCode.OK)
                        throw new Exception(string.Format("Server error (HTTP {0}: {1}).", response.StatusCode,
                                                          response.StatusDescription));

                    Stream responseStream = response.GetResponseStream();
                    if (responseStream == null)
                        return;

                    string json = new StreamReader(responseStream).ReadToEnd();
                    JObject o = JObject.Parse(json);
                    JArray jArray = ((JArray)o["data"]["translations"]);
                    for (int j = 0; j < jArray.Count; j++)
                    {
                        translations[j].TargetText = jArray[j]["translatedText"].ToString();
                    }
                }
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }

        /// <summary>
        /// Google translate removes leading and trailing whitespace. Let's add it back in.
        /// </summary>
        /// <param name="translations"></param>
        private void AddWhitespaceBackIn(List<Translation> translations)
        {
            foreach (var translation in translations)
            {
                if (translation.SourceText == null || translation.TargetText == null)
                    continue;
                
                var matchStart = WhitespaceStart.Match(translation.SourceText);
                if (matchStart.Success)
                    translation.TargetText = translation.TargetText
                        .Insert(0, translation.SourceText.Substring(0, matchStart.Length));
                else
                    continue;

                var matchEnd = WhitespaceEnd.Match(translation.SourceText);
                if (matchEnd.Success)
                    translation.TargetText = translation.TargetText +
                                             translation.SourceText.Substring(matchEnd.Index);
            }
        }

        private void AddTextToUrl(StringBuilder url, string text)
        {
            url.Append("&q=" + HttpUtility.UrlEncode(text));
        }
    }
}
