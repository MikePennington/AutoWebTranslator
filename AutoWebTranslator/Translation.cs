namespace AutoWebTranslator
{
    public class Translation
    {
        public Translation(string sourceText)
        {
            SourceText = sourceText;
        }

        public string SourceText { get; set; }

        public string TargetText { get; set; }

        public override string ToString()
        {
            return SourceText + " | " + TargetText;
        }

        public override bool Equals(object obj)
        {
            if (obj == null || GetType() != obj.GetType())
            {
                return false;
            }

            return SourceText.Equals(((Translation)obj).SourceText);
        }

        public override int GetHashCode()
        {
            return SourceText.GetHashCode();
        }
    }
}
