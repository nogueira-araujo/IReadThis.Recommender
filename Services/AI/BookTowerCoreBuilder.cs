using System;
using Tensorflow;
using static Tensorflow.Binding;

namespace IReadThis.Recommender.Services.AI
{
    public static class BookTowerCoreBuilder
    {
        public static BookTowerModel BuildBookTowerCore()
        {
            // 1. Inicialização do Grafo Computacional [1, 2]
            var graph = tf.Graph().as_default();

            // 2. Entradas (Placeholders)
            // Substituem o keras.Input. O -1 indica que o tamanho do lote (batch_size) é dinâmico.
            var categoryInput = tf.placeholder(tf.int32, shape: new[] { -1, 100 }, name: "category_ids");
            var textInput = tf.placeholder(tf.int32, shape: new[] { -1, 50 }, name: "text_tokens");

            // 3. Matrizes de Pesos para Embeddings (Variables) [2]
            // Sem o Keras, você é responsável por inicializar as matrizes matemáticas na memória.
            var categoryEmbeddingWeights = tf.Variable(tf.random.truncated_normal(new[] { 101, 32 }), name: "cat_weights");
            var textEmbeddingWeights = tf.Variable(tf.random.truncated_normal(new[] { 10000, 64 }), name: "text_weights");

            // 4. Operações de Embedding Lookup e Transformações [2]
            // Busca o vetor correspondente ao ID da categoria na matriz de pesos
            var categoryEmbeddings = tf.nn.embedding_lookup((Tensor)categoryEmbeddingWeights, categoryInput);
            var textEmbeddings = tf.nn.embedding_lookup((Tensor)textEmbeddingWeights, textInput);

            // Substituindo o GlobalAveragePooling1D: Tiramos a média no eixo 1 das categorias
            var categoryPooling = tf.reduce_mean(categoryEmbeddings, axis: 1);

            // Substituindo o Flatten: Remodelamos o tensor 3D de texto para 2D (50 tokens * 64 dimensões = 3200)
            var textFlatten = tf.reshape(textEmbeddings, new[] { -1, 3200 });

            // 5. Operação de Concatenação [3]
            var concatenated = tf.concat(new[] { categoryPooling, textFlatten }, axis: 1);

            // 6. Camada Densa Intermediária (Multiplicação de Matriz + Viés + Ativação) [2]
            // Entrada total: 32 (categorias) + 3200 (texto) = 3232 características.
            var weights1 = tf.Variable(tf.random.normal(new[] { 3232, 256 }), name: "dense1_weights");
            var bias1 = tf.Variable(tf.zeros(new[] { 256 }), name: "dense1_bias");

            // Lógica matemática: Relu( (Entrada * Pesos) + Viés )
            var dense1 = tf.nn.relu(tf.add(tf.matmul(concatenated, weights1), bias1));

            // 7. Camada Densa Final (Vetor que vai para o SQL Server)
            var weights2 = tf.Variable(tf.random.normal(new[] { 256, 768 }), name: "dense2_weights");
            var bias2 = tf.Variable(tf.zeros(new[] { 768 }), name: "dense2_bias");

            // O tensor resultante será o nosso Embedding final
            var finalEmbedding = tf.add(tf.matmul(dense1, weights2), bias2, name: "book_vector_output");

            // Nota: Para executar ou treinar esse grafo, é preciso abrir uma tf.Session() 
            // ou ativar o Eager Mode, e utilizar tf.train e tf.gradients para atualizar as Variables [2].
            return new BookTowerModel(categoryInput, textInput, finalEmbedding);
        }
    }
}
