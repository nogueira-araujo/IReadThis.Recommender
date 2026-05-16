using System;
using Tensorflow;
using static Tensorflow.Binding;

namespace IReadThis.Recommender.Services.AI
{
    public static class ReaderTowerCoreBuilder
    {
        public static ReaderTowerModel BuildReaderTowerCore(Session session)
        {
            if (session == null)
                throw new ArgumentNullException(nameof(session), "Session cannot be null.");

            // 1. Usar o contexto de grafo da sessão injetada, não criar um novo
            session.graph.as_default();

            // ==========================================
            // 1. PORTAS DE ENTRADA (PLACEHOLDERS)
            // ==========================================

            // shape: [-1] indica que aceita N perfis ao mesmo tempo. 
            // Valores: 0 (N/A), 1 (M), 2 (F)
            var sexInput = tf.placeholder(tf.int32, shape: new[] { -1 }, name: "sex_id");

            // shape: [-1, 1] indica uma matriz coluna para o ano normalizado
            var yearInput = tf.placeholder(tf.float32, shape: new[] { -1, 1 }, name: "birth_year_norm");

            // ==========================================
            // 2. PROCESSAMENTO DO SEXO (EMBEDDING)
            // ==========================================

            // Matriz de pesos para 3 possibilidades. Definimos que o sexo será representado por 16 características.
            var sexWeights = tf.Variable(tf.random.truncated_normal(new[] { 3, 16 }), name: "sex_weights");
            var sexEmbedding = tf.nn.embedding_lookup((Tensor)sexWeights, sexInput);

            // ==========================================
            // 3. PROCESSAMENTO DO ANO DE NASCIMENTO (DENSE)
            // ==========================================

            // Multiplicamos a idade por 16 pesos para igualar à dimensão do sexo
            var yearWeights = tf.Variable(tf.random.normal(new[] { 1, 16 }), name: "year_weights");
            var yearBias = tf.Variable(tf.zeros(new[] { 16 }), name: "year_bias");
            var yearDense = tf.nn.relu(tf.add(tf.matmul(yearInput, yearWeights), yearBias));

            // ==========================================
            // 4. CONCATENAÇÃO DAS FEATURES
            // ==========================================

            // Juntamos o sexo (16) com o ano (16) = Tensor de 32 dimensões
            var concatenated = tf.concat(new[] { sexEmbedding, yearDense }, axis: 1);

            // ==========================================
            // 5. APRENDIZADO PROFUNDO (CAMADAS DENSAS)
            // ==========================================

            // Camada Intermediária
            var dense1Weights = tf.Variable(tf.random.normal(new[] { 32, 128 }), name: "reader_dense1_w");
            var dense1Bias = tf.Variable(tf.zeros(new[] { 128 }), name: "reader_dense1_b");
            var dense1 = tf.nn.relu(tf.add(tf.matmul(concatenated, dense1Weights), dense1Bias));

            // ==========================================
            // 6. SAÍDA DA TORRE DO LEITOR (768 DIMENSÕES)
            // ==========================================

            // CRÍTICO: O vetor final do leitor deve ter o EXATO mesmo tamanho do vetor do livro
            var finalWeights = tf.Variable(tf.random.normal(new[] { 128, 768 }), name: "reader_final_w");
            var finalBias = tf.Variable(tf.zeros(new[] { 768 }), name: "reader_final_b");

            var readerVectorOutput = tf.add(tf.matmul(dense1, finalWeights), finalBias, name: "reader_vector_output");

            // Retorna os ponteiros para que a Sessão os invoque
            return new ReaderTowerModel(sexInput, yearInput, readerVectorOutput);
        }
    }
}
