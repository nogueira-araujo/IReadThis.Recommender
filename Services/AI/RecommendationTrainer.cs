using DynamicDtoCore;
using IReadThis.Recommender.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using Tensorflow;
using Tensorflow.NumPy;
using static Tensorflow.Binding;

namespace IReadThis.Recommender.Services.AI
{
    public class RecommendationTrainer
    {
        private readonly Tensor _trueRatingInput;
        private readonly Tensor _loss;
        private readonly Operation _trainOp;
        private readonly RecommendationTrainerData _model;

        public RecommendationTrainer(RecommendationTrainerData model)
        {
            if (model == null)
                throw new ArgumentNullException(nameof(model), "RecommendationTrainerData cannot be null.");

            _model = model;

            // Valida que a Session não é null
            if (_model.Session == null)
                throw new InvalidOperationException(
                    "RecommendationTrainerData.Session é null. A sessão do TensorFlow não foi inicializada corretamente. " +
                    "Verifique se Program.cs configurou corretamente a SessionManager e se o checkpoint foi carregado.");

            // Garante que as operações de treinamento sejam adicionadas ao grafo onde as variáveis residem
            try
            {
                _model.Session.graph.as_default();
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException(
                    "Falha ao definir o grafo padrão da sessão. Verifique se a Session está em estado válido.", ex);
            }

            // 1. Lógica de Cruzamento (Dot Product) para Predição
            var predictedRating = tf.reduce_sum(tf.multiply(_model.BookVector, _model.ReaderVector), axis: 1);

            // 2. Placeholder para Nota Real (0 a 4)
            _trueRatingInput = tf.placeholder(tf.float32, shape: new[] { -1 }, name: "true_rating");

            // 3. Função de Perda (MSE)
            _loss = tf.reduce_mean(tf.square(predictedRating - _trueRatingInput), name: "loss_op");

            // 4. Otimizador Adam
            var optimizer = tf.train.AdamOptimizer(learning_rate: 0.001f);

            try 
            {
                _trainOp = optimizer.minimize(_loss);
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException(
                    "Falha ao inicializar o otimizador. Certifique-se que as variáveis do grafo são treináveis e que a sessão está correta.", ex);
            }
        }

        public float TrainBatch(TrainBatchData data)
        {
            int batchSize = data.BirthYears.Length;

            // Normalização consistente com o Generator
            float[] normalizedYears = data.BirthYears.Select(y => (y - 1900) / 100.0f).ToArray();

            var feedDict = new FeedItem[]
            {
                new FeedItem(_model.SexInput, np.array(data.SexIds).reshape(new Shape(batchSize))),
                new FeedItem(_model.YearInput, np.array(normalizedYears).reshape(new Shape(batchSize, 1))),
                new FeedItem(_model.CategoryInput, np.array(data.PaddedCategories).reshape(new Shape(batchSize, 50))),
                new FeedItem(_model.TextInput, np.array(data.TextTokens).reshape(new Shape(batchSize, 50))),
                new FeedItem(_trueRatingInput, np.array(data.TrueRatings).reshape(new Shape(batchSize)))
            };

            var result = _model.Session.run(new object[] { _trainOp, _loss }, feedDict);
            return (float)result[1]; // Retorna o valor da Loss
        }
    }
}
