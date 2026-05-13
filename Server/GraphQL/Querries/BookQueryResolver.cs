// GraphQL query resolver for fetching individual books and generating presigned cover/content URLs.
using HotChocolate;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Server.Data;
using Server.GraphQL.Types;
using Server.Models;
using Server.Services;

namespace Server.GraphQL.Querries;

[ExtendObjectType<Query>]
public class BookQueryResolver
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

    [GraphQLName("book")]
    public async Task<Book?> GetBook(
        int id,
        [Service] ElibDbContext db,
        CancellationToken cancellationToken)
    {
        return await db.Books
            .Include(b => b.MediaFiles)
            .Include(b => b.Publisher)
            .Include(b => b.Language)
            .Include(b => b.TypeOfWork)
            .Include(b => b.BookAuthors).ThenInclude(ba => ba.Author)
            .Include(b => b.BookGenres).ThenInclude(bg => bg.Genre)
            .FirstOrDefaultAsync(b => b.BookId == id, cancellationToken);
    }

    [GraphQLName("book_file_url")]
    public async Task<string?> GetBookFileUrl(
        int bookId,
        [Service] ElibDbContext db,
        [Service] IHttpContextAccessor httpContext,
        CancellationToken cancellationToken)
    {
        var file = await db.MediaFiles
            .FirstOrDefaultAsync(m => m.EntityId == bookId && m.FileType == "content", cancellationToken);
        if (file == null) return null;

        var req = httpContext.HttpContext?.Request;
        if (req == null) return null;

        var ext = System.IO.Path.GetExtension(file.StorageKey).TrimStart('.').ToLowerInvariant();
        if (string.IsNullOrEmpty(ext)) ext = "pdf";

        // Return a server-proxied URL so the client never contacts Yandex Cloud directly.
        return $"{req.Scheme}://{req.Host}/api/books/{bookId}/content?type={ext}";
    }

    [GraphQLName("book_cover_url")]
    public async Task<string?> GetBookCoverUrl(
        int bookId,
        [Service] ElibDbContext db,
        [Service] IStorageService storage,
        CancellationToken cancellationToken)
    {
        var file = await db.MediaFiles
            .FirstOrDefaultAsync(m => m.EntityId == bookId
                                   && m.EntityType == "book"
                                   && m.FileType   == "cover", cancellationToken);
        if (file == null) return null;

        try { return await storage.GetFile(file.StorageKey); }
        catch { return null; }
    }
}
