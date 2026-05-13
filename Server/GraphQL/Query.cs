// Root GraphQL Query type with cursor-based paginated book loading and filtering.
using HotChocolate;
using Microsoft.EntityFrameworkCore;
using Server.Data;
using Server.GraphQL.Inputs;
using Server.GraphQL.Types;
using Server.Models;
using Server.Services;

namespace Server.GraphQL
{
    public class Query
    {
        private static string EncodeCursor(int id) =>
            Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes(id.ToString()));

        private static int? DecodeCursor(string? cursor)
        {
            if (string.IsNullOrEmpty(cursor)) return null;
            try
            {
                var decoded = System.Text.Encoding.UTF8.GetString(Convert.FromBase64String(cursor));
                return int.TryParse(decoded, out var id) ? id : null;
            }
            catch { return null; }
        }

        [GraphQLName("load_books")]
        public async Task<BooksConnection> LoadBooks(
            [Service] ElibDbContext db,
            [Service] IStorageService? storage,
            PaginationInput? pagination = null,
            BookFilter? filter = null)
        {
            var afterId    = DecodeCursor(pagination?.After);
            var beforeId   = DecodeCursor(pagination?.Before);
            var isBackward = pagination?.Last.HasValue == true && beforeId.HasValue;
            var pageSize   = isBackward ? pagination!.Last!.Value : pagination?.First ?? 20;

            var booksQuery = db.Books
                .Include(b => b.MediaFiles)
                .Include(b => b.Publisher)
                .Include(b => b.Language)
                .Include(b => b.TypeOfWork)
                .Include(b => b.BookAuthors).ThenInclude(ba => ba.Author)
                .Include(b => b.BookGenres).ThenInclude(bg => bg.Genre)
                .AsQueryable();

            if (!string.IsNullOrEmpty(filter?.Title))
                booksQuery = booksQuery.Where(b => b.Title.Contains(filter.Title));

            if (filter?.YearOfPublishing != null)
                booksQuery = booksQuery.Where(b => b.YearOfPublishing == filter.YearOfPublishing);

            if (!string.IsNullOrEmpty(filter?.Language))
                booksQuery = booksQuery.Where(b =>
                    b.Language.LangName.Contains(filter.Language) ||
                    b.Language.LangCode == filter.Language);

            if (!string.IsNullOrEmpty(filter?.Publisher))
                booksQuery = booksQuery.Where(b => b.Publisher.Name.Contains(filter.Publisher));

            if (!string.IsNullOrEmpty(filter?.TypeOfWork))
                booksQuery = booksQuery.Where(b => b.TypeOfWork.TypeName.Contains(filter.TypeOfWork));

            if (!string.IsNullOrEmpty(filter?.Author))
                booksQuery = booksQuery.Where(b => b.BookAuthors.Any(ba =>
                    ba.Author.FirstName.Contains(filter.Author) ||
                    ba.Author.LastName.Contains(filter.Author)));

            if (!string.IsNullOrEmpty(filter?.Genre))
                booksQuery = booksQuery.Where(b => b.BookGenres.Any(bg =>
                    bg.Genre.GenreName.Contains(filter.Genre)));

            var totalCount = await booksQuery.CountAsync();

            List<Book> books;
            if (isBackward)
            {
                booksQuery = booksQuery.Where(b => b.BookId < beforeId!.Value);
                books = await booksQuery
                    .OrderByDescending(b => b.BookId)
                    .Take(pageSize + 1)
                    .ToListAsync();
                books.Reverse();
            }
            else
            {
                if (afterId.HasValue)
                    booksQuery = booksQuery.Where(b => b.BookId > afterId.Value);
                books = await booksQuery
                    .OrderBy(b => b.BookId)
                    .Take(pageSize + 1)
                    .ToListAsync();
            }

            var hasExtraPage = books.Count > pageSize;
            if (hasExtraPage)
                books.RemoveAt(isBackward ? 0 : books.Count - 1);

            var edges = books.Select(b => new BookEdge
            {
                Cursor = EncodeCursor(b.BookId),
                Node   = b
            }).ToList();

            return new BooksConnection
            {
                TotalCount = totalCount,
                PageSize   = pageSize,
                Nodes      = books,
                Edges      = edges,
                PageInfo   = new BookPageInfo
                {
                    StartCursor     = edges.FirstOrDefault()?.Cursor,
                    EndCursor       = edges.LastOrDefault()?.Cursor,
                    HasNextPage     = isBackward ? beforeId.HasValue : hasExtraPage,
                    HasPreviousPage = isBackward ? hasExtraPage       : afterId.HasValue
                }
            };
        }
    }

    public class BooksConnection
    {
        public List<BookEdge> Edges { get; set; } = new();
        public List<Book> Nodes { get; set; } = new();
        public int TotalCount { get; set; }
        public BookPageInfo PageInfo { get; set; } = new();
        public int PageSize { get; set; }
    }

    public class BookEdge
    {
        public string Cursor { get; set; } = string.Empty;
        public Book Node { get; set; } = new();
    }

    public class BookPageInfo
    {
        public string? StartCursor { get; set; }
        public string? EndCursor { get; set; }
        public bool HasNextPage { get; set; }
        public bool HasPreviousPage { get; set; }
    }

    public class BookFilter
    {
        public string? Author { get; set; }
        public string? Genre { get; set; }
        public int? YearOfPublishing { get; set; }
        public string? Publisher { get; set; }
        public string? Language { get; set; }
        public string? TypeOfWork { get; set; }
        public string? Title { get; set; }
    }
}
