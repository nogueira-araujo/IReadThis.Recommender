using Tensorflow;

namespace IReadThis.Recommender.Services.AI
{
    /// <summary>
    /// Represents a model for processing book-related data using category and text inputs, and producing a final
    /// embedding tensor.
    /// </summary>
    /// <remarks>This model is typically used in machine learning scenarios where both categorical and textual
    /// features are combined to generate a unified embedding representation. The properties allow setting or retrieving
    /// the input tensors and the resulting embedding.</remarks>
    public class BookTowerModel
    {
        private Tensor? categoryInput;
        private Tensor? textInput;
        private Tensor? finalEmbedding;

        public Tensor? CategoryInput { get => categoryInput; set => categoryInput = value; }
        public Tensor? TextInput { get => textInput; set => textInput = value; }
        public Tensor? FinalEmbedding { get => finalEmbedding; set => finalEmbedding = value; }
        public BookTowerModel(Tensor? categoryInput, Tensor? textInput, Tensor? finalEmbedding) 
        {
            this.CategoryInput = categoryInput;
            this.TextInput = textInput;
            this.FinalEmbedding = finalEmbedding;
        }
    }
}
