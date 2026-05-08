using System;
using System.Collections.Generic;
using Tensorflow;
using Tensorflow.NumPy; // Utilizado para formatar matrizes no TF.NET
using static Tensorflow.Binding;

namespace IReadThis.Recommender.Services.AI
{
    public class BookEmbeddingGenerator
    {
        // Variáveis de controle da Sessão e do Grafo
        private readonly Session _session;
        private readonly Tensor _categoryInput;
        private readonly Tensor _textInput;
        private readonly Tensor _finalEmbedding;

        // O construtor recebe as referências geradas pelo método BuildBookTowerCore
        public BookEmbeddingGenerator(Session session, Tensor categoryInput, Tensor textInput, Tensor finalEmbedding)
        {
            _session = session;
            _categoryInput = categoryInput;
            _textInput = textInput;
            _finalEmbedding = finalEmbedding;
        }
        public float[] GenerateTensorEmbedding(string title, string author, List<int> categoryIds)
        {
            // 1. PREPARAÇÃO DAS CATEGORIAS (O PADRÃO "PADDING")
            // Instanciamos um array fixo de 50 posições preenchido com zeros
            int[] paddedCategories = new int[2];

            // Copiamos os IDs reais do livro, respeitando o limite máximo
            for (int i = 0; i < Math.Min(categoryIds.Count, 50); i++)
            {
                paddedCategories[i] = categoryIds[i];
            }

            // 2. PREPARAÇÃO DO TEXTO (TOKENIZAÇÃO)
            // Combinamos Título e Autor em uma única string semântica e tokenizamos
            string rawText = $"{title} {author}";
            int[] textTokens = TokenizeText(rawText, maxTokens: 50);

            // 3. CONVERSÃO PARA TENSORES (NUMPY ARRAYS)
            // O shape [1, X] indica que estamos processando um "batch" de tamanho 1 (um único livro por vez)
            var categoryNdArray = np.array(paddedCategories).reshape(new Shape(1, 50));
            var textNdArray = np.array(textTokens).reshape(new Shape(1, 50));

            // 4. EXECUÇÃO NO GRAFO (INFERÊNCIA)
            // Criamos o dicionário de injeção ligando os dados aos Placeholders da rede
            var feedDict = new FeedItem[]
            {
                new FeedItem(_categoryInput, categoryNdArray),
                new FeedItem(_textInput, textNdArray)
            };

            // Rodamos a sessão informando qual tensor queremos como resultado final
            var outputTensor = _session.run(_finalEmbedding, feedDict);

            // 5. EXTRAÇÃO DO VETOR
            // O resultado é o nosso array denso com as características aprendidas (768 dimensões)
            float[] embeddingVector = outputTensor.ToArray<float>();

            return embeddingVector;
        }

        // Método auxiliar de Tokenização Determinística
        private int[] TokenizeText(string text, int maxTokens)
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
