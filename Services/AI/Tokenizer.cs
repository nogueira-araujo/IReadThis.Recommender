namespace IReadThis.Recommender.Services.AI
{
    public static class Tokenizer
    {
        // Método auxiliar de Tokenização Determinística
        public static int[] TokenizeText(this string text, int maxTokens)
        {
            int[] tokens = new int[maxTokens];
            if (string.IsNullOrWhiteSpace(text)) return tokens;

            // Limpeza básica e separação por espaços ou pontuação
            string[] words = text.ToLowerInvariant()
                                 .Split(new[] { ' ', '.', ',', ':', ';' }, StringSplitOptions.RemoveEmptyEntries);

            for (int i = 0; i < Math.Min(words.Length, maxTokens); i++)
            {
                // Função de hash simples para mapear a palavra a um ID do nosso vocabulário de texto (que definimos como 10000)
                // Em um cenário de NLP avançado, aqui usaríamos bibliotecas como o BertTokenizer
                tokens[i] = Math.Abs(words[i].GetHashCode()) % 10000;
            }

            return tokens; // Os espaços não preenchidos pelas palavras continuarão como zero (padding text)
        }
    }
}
