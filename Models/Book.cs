namespace IReadThis.Recommender.Models
{
    public class Book: IBook
    {
        public Book() { }
        public Book(int bookId, string title, string author, string? publisher, int? releaseYear, int? pageCount)
        {
            BookId = bookId;
            Title = title;
            Author = author;
            Publisher = publisher;
            ReleaseYear = releaseYear;
            PageCount = pageCount;
        }
        public string Author { get; set; }
        public int BookId { get; set; }
        public int? PageCount { get; set; }
        public string? Publisher { get; set; }
        public int? ReleaseYear { get; set; }
        public string Title { get; set; }
    }
}
