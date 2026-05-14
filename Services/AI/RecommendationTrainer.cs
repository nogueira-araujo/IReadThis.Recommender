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
            _model = model;

            // 1. Lógica de Cruzamento (Dot Product) para Predição
            var predictedRating = tf.reduce_sum(tf.multiply(_model.BookVector, _model.ReaderVector), axis: 1);

            // 2. Placeholder para Nota Real (0 a 4)
            _trueRatingInput = tf.placeholder(tf.float32, shape: new[] { -1 }, name: "true_rating");

            // 3. Função de Perda (MSE)
            _loss = tf.reduce_mean(tf.square(predictedRating - _trueRatingInput));

            // 4. Otimizador Adam
            var optimizer = tf.train.AdamOptimizer(learning_rate: 0.001f);
            _trainOp = optimizer.minimize(_loss);
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
