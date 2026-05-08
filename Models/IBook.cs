namespace IReadThis.Recommender.Models
{
    /// <summary>
    /// Interface representing a book, with properties for author, title, publisher, release year, page count, and an ID.
    /// </summary>
    public interface IBook
    {
        string Author { get; set; }
        int BookId { get; set; }
        int? PageCount { get; set; }
        string? Publisher { get; set; }
        int? ReleaseYear { get; set; }
        string Title { get; set; }
    }
}