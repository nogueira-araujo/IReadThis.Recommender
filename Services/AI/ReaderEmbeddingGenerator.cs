using System;
using Tensorflow;
using Tensorflow.NumPy;
using static Tensorflow.Binding;

namespace IReadThis.Recommender.Services.AI
{
    public class ReaderEmbeddingGenerator
    {
        private readonly Session _session;
        private readonly Tensor _sexInput;
        private readonly Tensor _yearInput;
        private readonly Tensor _readerVectorOutput;

        public Session Session { get { return _session; } }

        public ReaderEmbeddingGenerator(Session session, Tensor sexInput, Tensor yearInput, Tensor readerVectorOutput)
        {
            _session = session;
            _sexInput = sexInput;
            _yearInput = yearInput;
            _readerVectorOutput = readerVectorOutput;
        }

        public float[] GenerateTensorEmbedding(int birthYear, string sex)
        {
            // 1. PROCESSAMENTO DO SEXO (Opcional)
            // Conforme a Visão de Funcionalidades, se não informado, devemos desconsiderar (assumir valor neutro)
            int sexId = 0; // 0 = Neutro/Não Informado

            if (!string.IsNullOrWhiteSpace(sex))
            {
                if (sex.Equals("M", StringComparison.OrdinalIgnoreCase)) sexId = 1;
                else if (sex.Equals("F", StringComparison.OrdinalIgnoreCase)) sexId = 2;
            }

            // 2. PROCESSAMENTO DO ANO DE NASCIMENTO (Normalização)
            // Redes neurais aprendem melhor com valores pequenos. 
            // Normalizamos subtraindo uma base e dividindo por um fator de escala.
            float normalizedYear = (birthYear - 1900) / 100.0f;

            // 3. CONVERSÃO PARA TENSORES (NumPy Arrays com Shape explícito)
            // sexNdArray: Shape(1) representa um vetor 1D com 1 elemento (batch = 1)
            var sexNdArray = np.array(new int[] { sexId }).reshape(new Shape(1));

            // yearNdArray: Shape(1, 1) representa uma matriz 2D de 1 linha e 1 coluna
            var yearNdArray = np.array(new float[] { normalizedYear }).reshape(new Shape(1, 1));

            // 4. EXECUÇÃO NO GRAFO COMPUTACIONAL
            var feedDict = new FeedItem[]
            {
                new FeedItem(_sexInput, sexNdArray),
                new FeedItem(_yearInput, yearNdArray)
            };

            var outputTensor = _session.run(_readerVectorOutput, feedDict);

            // 5. EXTRAÇÃO DO VETOR FINAL
            // O resultado é o nosso array denso com as características geracionais e de gênero do leitor
            return outputTensor.ToArray<float>();
        }
    }
}