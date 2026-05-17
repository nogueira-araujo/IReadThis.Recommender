using Tensorflow;

namespace IReadThis.Recommender.Models
{
    /// <summary>
    /// Represents the input and output tensors for a reader tower model, typically used in machine learning scenarios
    /// to process reader-related features.
    /// </summary>
    /// <remarks>This model encapsulates the input features and the resulting output vector for a reader
    /// tower, which may be used in recommendation systems or similar applications. The properties are nullable to allow
    /// for scenarios where certain inputs or outputs may be optional or unavailable.</remarks>
    public class ReaderTowerModel
    {

        public Tensor? SexInput { get; set; }
        public Tensor? YearInput { get; set; }
        public Tensor? ReaderVectorOutput { get; set; }
        public ReaderTowerModel(Tensor? sexInput, Tensor? yearInput, Tensor? readerVectorOutput)
        {
            this.SexInput = sexInput;
            this.YearInput = yearInput;
            this.ReaderVectorOutput = readerVectorOutput;
        }
    }
}
