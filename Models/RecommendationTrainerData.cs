using Tensorflow;

namespace IReadThis.Recommender.Models
{
    /// <summary>
    /// Classe que encapsula os tensores de entrada e a sessão do TensorFlow para o processo de treinamento do modelo de recomendação.
    /// </summary>
    public sealed class RecommendationTrainerData
    {
        // Este modelo é responsável por criar as camadas de Embedding e processar os dados de entrada
        // Ele não contém a lógica de treinamento, apenas a definição da arquitetura da rede neural
        public Tensor SexInput { get; private set; }
        public Tensor YearInput { get; private set; }
        public Tensor ReaderVector { get; private set; }
        public Tensor CategoryInput { get; private set; }
        public Tensor TextInput { get; private set; }
        public Tensor BookVector { get; private set; }
        public Session Session { get; private set; }

        /// <summary>
        /// Initializes a new instance of the RecommendationTrainerData class using the specified session.
        /// </summary>
        /// <param name="session">The session context to associate with this instance. Cannot be null.</param>
        public RecommendationTrainerData(Session session)
        {
            if (session == null)
                throw new ArgumentNullException(nameof(session), "Session cannot be null.");
            this.Session = session;
        }
        public RecommendationTrainerData(Session session, Tensor sexInput, Tensor yearInput, Tensor readerVector, Tensor categoryInput, Tensor textInput, Tensor bookVector) : this(session)
        {
            SexInput = sexInput;
            YearInput = yearInput;
            ReaderVector = readerVector;
            CategoryInput = categoryInput;
            TextInput = textInput;
            BookVector = bookVector;
        }

        public RecommendationTrainerData(Session session, ReaderTowerModel readerModel, BookTowerModel bookModel) :
            this(session, readerModel.SexInput, readerModel.YearInput, readerModel.ReaderVectorOutput, bookModel.CategoryInput, bookModel.TextInput, bookModel.FinalEmbedding)
        { }
    }
}
