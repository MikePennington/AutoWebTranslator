using System.Collections.Generic;

namespace AutoWebTranslator
{
    public class TranslationRequest
    {
        public TranslationRequest(string sourceLanguage, string targetLanguage)
        {
            SourceLanguage = sourceLanguage;
            TargetLanguage = targetLanguage;
            Translations = new List<Translation>();
        }

        public string SourceLanguage { get; set; }

        public string TargetLanguage { get; set; }

        public List<Translation> Translations { get; set; }
    }
}
