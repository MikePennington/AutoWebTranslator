namespace AutoWebTranslator.Translators
{
    public interface ITranslationProvider
    {
        void Translate(TranslationRequest translation);

        string ProviderName { get; }
    }
}
