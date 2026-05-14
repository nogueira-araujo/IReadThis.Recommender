namespace IReadThis.Recommender.Services.AI
{
    public static class Tokenizer
    {
        private const int VocabularySize = 10000;

        /// <summary>
        /// Transforma texto em um array de IDs determinísticos.
        /// Método de extensão para centralizar a lógica usada em Towers e Generators.
        /// </summary>
        public static int[] TokenizeText(this string text, int maxTokens)
        {
            int[] tokens = new int[maxTokens];
            if (string.IsNullOrWhiteSpace(text)) return tokens;

            // Limpeza e separação uniforme
            string[] words = text.ToLowerInvariant()
                                 .Split(new[] { ' ', '.', ',', ':', ';', '-', '_' }, StringSplitOptions.RemoveEmptyEntries);

            for (int i = 0; i < Math.Min(words.Length, maxTokens); i++)
            {
                // Hashing determinístico para garantir consistência entre Treino e Inferência
                tokens[i] = Math.Abs(words[i].GetHashCode()) % VocabularySize;
            }

            return tokens;
        }

        public static int GetSexId(string? sex)
        {
            if (string.IsNullOrWhiteSpace(sex)) return 0; // Neutro
            if (sex.Equals("M", StringComparison.OrdinalIgnoreCase)) return 1;
            if (sex.Equals("F", StringComparison.OrdinalIgnoreCase)) return 2;
            return 0;
        }
    }
}
