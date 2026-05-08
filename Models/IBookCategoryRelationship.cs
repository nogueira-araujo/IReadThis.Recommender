namespace IReadThis.Recommender.Models
{
    public interface IBookCategoryRelationship
    {
        public int BookId { get; set; }
        public int CategoryId { get; set; }
    }
}
