// GraphQL mutation resolver for creating, updating, and deleting books with associated media and metadata.
using System.Security.Claims;
using HotChocolate;
using HotChocolate.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Server.Data;
using Server.GraphQL.Inputs;
using Server.Models;
using Server.Services;

namespace Server.GraphQL.Mutations;

[ExtendObjectType<Mutation>]
public class BookMutationResolver
{
    [GraphQLName("create_book")]
    [Authorize]
    public async Task<Book> CreateBook(
        CreateBookInput input,
        [Service] ElibDbContext db,
        [Service] ILogService log,
        [Service] IHttpContextAccessor http,
        CancellationToken cancellationToken)
    {
        var uploaderId = GetUserId(http) ?? throw new GraphQLException(
            ErrorBuilder.New().SetMessage("Требуется аутентификация.").SetCode("UNAUTHORIZED").Build());

        var ip = http.HttpContext?.Connection.RemoteIpAddress?.ToString();

        await using var tx = await db.Database.BeginTransactionAsync(cancellationToken);

        var now = DbTime.Now;

        var publisherId = input.PublisherId;
        if (publisherId == 0 && !string.IsNullOrWhiteSpace(input.NewPublisherName))
        {
            var trimmed   = input.NewPublisherName.Trim();
            var publisher = await db.Publishers
                .FirstOrDefaultAsync(p => p.Name.ToLower() == trimmed.ToLower(), cancellationToken);
            if (publisher == null)
            {
                publisher = new Publisher { Name = trimmed };
                db.Publishers.Add(publisher);
                await db.SaveChangesAsync(cancellationToken);
            }
            publisherId = publisher.PublisherId;
        }

        var languageId = input.LanguageId;
        if (languageId == 0 && !string.IsNullOrWhiteSpace(input.NewLanguageName))
        {
            var langName = input.NewLanguageName.Trim();
            var langCode = (input.NewLanguageCode ?? "").Trim();
            var language = await db.Languages
                .FirstOrDefaultAsync(l => l.LangName.ToLower() == langName.ToLower(), cancellationToken);
            if (language == null)
            {
                language = new Language { LangName = langName, LangCode = langCode };
                db.Languages.Add(language);
                await db.SaveChangesAsync(cancellationToken);
            }
            languageId = language.LangId;
        }

        var typeOfWorkId = input.TypeOfWorkId;
        if (typeOfWorkId == 0 && !string.IsNullOrWhiteSpace(input.NewTypeOfWorkName))
        {
            var typeName  = input.NewTypeOfWorkName.Trim();
            var typeOfWork = await db.TypesOfWorks
                .FirstOrDefaultAsync(t => t.TypeName.ToLower() == typeName.ToLower(), cancellationToken);
            if (typeOfWork == null)
            {
                typeOfWork = new TypesOfWork { TypeName = typeName };
                db.TypesOfWorks.Add(typeOfWork);
                await db.SaveChangesAsync(cancellationToken);
            }
            typeOfWorkId = typeOfWork.WorkId;
        }

        if (!string.IsNullOrWhiteSpace(input.Isbn))
        {
            var isbnTrimmed = input.Isbn.Trim();
            var exists = await db.Books.AnyAsync(b => b.Isbn == isbnTrimmed, cancellationToken);
            if (exists)
                throw new GraphQLException(
                    ErrorBuilder.New()
                        .SetMessage($"Книга с ISBN «{isbnTrimmed}» уже существует в библиотеке.")
                        .SetCode("ISBN_DUPLICATE")
                        .Build());
        }

        var book = new Book
        {
            Isbn              = string.IsNullOrWhiteSpace(input.Isbn) ? null : input.Isbn.Trim(),
            Title             = input.Title,
            Description       = input.Description,
            YearOfPublishing  = input.YearOfPublishing,
            PageCount         = input.PageCount,
            EditionNumber     = input.EditionNumber,
            PublisherId       = publisherId,
            TypeOfWorkId      = typeOfWorkId,
            LanguageId        = languageId,
            CreatedAt         = now,
            UpdatedAt         = now
        };

        db.Books.Add(book);
        await db.SaveChangesAsync(cancellationToken);

        foreach (var authorId in input.AuthorIds.Distinct())
            db.BookAuthors.Add(new BookAuthor { BookId = book.BookId, AuthorId = authorId });

        foreach (var na in input.NewAuthors)
        {
            var fn = na.FirstName.Trim();
            var ln = na.LastName.Trim();
            var author = await db.Authors
                .FirstOrDefaultAsync(a => a.FirstName.ToLower() == fn.ToLower()
                                       && a.LastName.ToLower()  == ln.ToLower(), cancellationToken);
            if (author == null)
            {
                author = new Author { FirstName = fn, LastName = ln, YearOfBirth = na.YearOfBirth };
                db.Authors.Add(author);
                await db.SaveChangesAsync(cancellationToken);
            }
            db.BookAuthors.Add(new BookAuthor { BookId = book.BookId, AuthorId = author.AuthorId });
        }

        foreach (var genreId in input.GenreIds.Distinct())
            db.BookGenres.Add(new BookGenre { BookId = book.BookId, GenreId = genreId });

        if (input.NewGenreNames?.Count > 0)
            await AddNewGenresAsync(db, book.BookId, input.NewGenreNames, input.GenreIds, cancellationToken);

        if (!string.IsNullOrEmpty(input.ContentStorageKey))
            db.MediaFiles.Add(new MediaFile
            {
                EntityType  = "book",
                EntityId    = book.BookId,
                FileType    = "content",
                FileName    = System.IO.Path.GetFileName(input.ContentStorageKey),
                FilePath    = input.ContentStorageKey,
                FileSize    = input.ContentFileSize ?? 0,
                MimeType    = input.ContentMimeType ?? "application/octet-stream",
                StorageType = "yandex_s3",
                StorageKey  = input.ContentStorageKey,
                Width       = 0,
                Height      = 0,
                IsPublic    = false,
                UploadedBy  = uploaderId,
                UploadedAt  = now
            });

        if (!string.IsNullOrEmpty(input.CoverStorageKey))
            db.MediaFiles.Add(new MediaFile
            {
                EntityType  = "book",
                EntityId    = book.BookId,
                FileType    = "cover",
                FileName    = System.IO.Path.GetFileName(input.CoverStorageKey),
                FilePath    = input.CoverStorageKey,
                FileSize    = input.CoverFileSize ?? 0,
                MimeType    = input.CoverMimeType ?? "image/jpeg",
                StorageType = "yandex_s3",
                StorageKey  = input.CoverStorageKey,
                Width       = input.CoverWidth  ?? 0,
                Height      = input.CoverHeight ?? 0,
                IsPublic    = true,
                UploadedBy  = uploaderId,
                UploadedAt  = now
            });

        await db.SaveChangesAsync(cancellationToken);
        await tx.CommitAsync(cancellationToken);

        var result = await db.Books
            .Include(b => b.Publisher)
            .Include(b => b.Language)
            .Include(b => b.TypeOfWork)
            .Include(b => b.BookAuthors).ThenInclude(ba => ba.Author)
            .Include(b => b.BookGenres).ThenInclude(bg => bg.Genre)
            .Include(b => b.MediaFiles)
            .FirstAsync(b => b.BookId == book.BookId, cancellationToken);

        await log.LogAsync(uploaderId, "create_book", ip, cancellationToken);

        return result;
    }

    [GraphQLName("update_book")]
    [Authorize]
    public async Task<Book> UpdateBook(
        UpdateBookInput input,
        [Service] ElibDbContext db,
        [Service] IStorageService storage,
        [Service] ILogService log,
        [Service] IHttpContextAccessor http,
        CancellationToken cancellationToken)
    {
        var userId = GetUserId(http);
        var ip     = http.HttpContext?.Connection.RemoteIpAddress?.ToString();

        var book = await db.Books
            .Include(b => b.BookAuthors)
            .Include(b => b.BookGenres)
            .Include(b => b.MediaFiles)
            .FirstOrDefaultAsync(b => b.BookId == input.BookId, cancellationToken);

        if (book == null)
            throw new GraphQLException(
                ErrorBuilder.New().SetMessage("Книга не найдена.").SetCode("BOOK_NOT_FOUND").Build());

        await using var tx = await db.Database.BeginTransactionAsync(cancellationToken);

        if (input.Isbn != null)             book.Isbn             = input.Isbn;
        if (input.Title != null)            book.Title            = input.Title;
        if (input.Description != null)      book.Description      = input.Description;
        if (input.YearOfPublishing != null) book.YearOfPublishing = input.YearOfPublishing.Value;
        if (input.PageCount != null)        book.PageCount        = input.PageCount.Value;
        if (input.EditionNumber != null)    book.EditionNumber    = input.EditionNumber.Value;
        if (input.PublisherId != null)      book.PublisherId      = input.PublisherId.Value;
        if (input.TypeOfWorkId != null)     book.TypeOfWorkId     = input.TypeOfWorkId.Value;
        if (input.LanguageId != null)       book.LanguageId       = input.LanguageId.Value;
        book.UpdatedAt = DbTime.Now;

        if (input.AuthorIds != null)
        {
            db.BookAuthors.RemoveRange(book.BookAuthors);
            foreach (var id in input.AuthorIds.Distinct())
                db.BookAuthors.Add(new BookAuthor { BookId = book.BookId, AuthorId = id });
        }

        if (input.GenreIds != null)
        {
            db.BookGenres.RemoveRange(book.BookGenres);
            foreach (var id in input.GenreIds.Distinct())
                db.BookGenres.Add(new BookGenre { BookId = book.BookId, GenreId = id });
        }

        if (input.NewGenreNames?.Count > 0)
            await AddNewGenresAsync(db, book.BookId, input.NewGenreNames, input.GenreIds ?? new(), cancellationToken);

        string? oldCoverKey = null;
        if (!string.IsNullOrEmpty(input.CoverStorageKey))
        {
            var existingCover = await db.MediaFiles
                .FirstOrDefaultAsync(m => m.EntityId == book.BookId
                                       && m.EntityType == "book"
                                       && m.FileType   == "cover", cancellationToken);
            if (existingCover != null)
            {
                oldCoverKey              = existingCover.StorageKey;
                existingCover.StorageKey = input.CoverStorageKey;
                existingCover.FilePath   = input.CoverStorageKey;
                existingCover.FileName   = System.IO.Path.GetFileName(input.CoverStorageKey);
                existingCover.MimeType   = input.CoverMimeType ?? "image/jpeg";
                existingCover.FileSize   = input.CoverFileSize ?? 0;
                existingCover.IsPublic   = true;
            }
            else
            {
                db.MediaFiles.Add(new MediaFile
                {
                    EntityType  = "book",
                    EntityId    = book.BookId,
                    FileType    = "cover",
                    FileName    = System.IO.Path.GetFileName(input.CoverStorageKey),
                    FilePath    = input.CoverStorageKey,
                    StorageKey  = input.CoverStorageKey,
                    FileSize    = input.CoverFileSize ?? 0,
                    MimeType    = input.CoverMimeType ?? "image/jpeg",
                    StorageType = "yandex_s3",
                    Width       = 0,
                    Height      = 0,
                    IsPublic    = true,
                    UploadedBy  = userId ?? 0,
                    UploadedAt  = DbTime.Now
                });
            }
        }

        string? oldContentKey = null;
        if (!string.IsNullOrEmpty(input.ContentStorageKey))
        {
            var existingContent = await db.MediaFiles
                .FirstOrDefaultAsync(m => m.EntityId == book.BookId
                                       && m.EntityType == "book"
                                       && m.FileType   == "content", cancellationToken);
            if (existingContent != null)
            {
                oldContentKey              = existingContent.StorageKey;
                existingContent.StorageKey = input.ContentStorageKey;
                existingContent.FilePath   = input.ContentStorageKey;
                existingContent.FileName   = System.IO.Path.GetFileName(input.ContentStorageKey);
                existingContent.MimeType   = input.ContentMimeType ?? "application/octet-stream";
                existingContent.FileSize   = input.ContentFileSize ?? 0;
            }
            else
            {
                db.MediaFiles.Add(new MediaFile
                {
                    EntityType  = "book",
                    EntityId    = book.BookId,
                    FileType    = "content",
                    FileName    = System.IO.Path.GetFileName(input.ContentStorageKey),
                    FilePath    = input.ContentStorageKey,
                    StorageKey  = input.ContentStorageKey,
                    FileSize    = input.ContentFileSize ?? 0,
                    MimeType    = input.ContentMimeType ?? "application/octet-stream",
                    StorageType = "yandex_s3",
                    Width       = 0,
                    Height      = 0,
                    IsPublic    = false,
                    UploadedBy  = userId ?? 0,
                    UploadedAt  = DbTime.Now
                });
            }
        }

        await db.SaveChangesAsync(cancellationToken);
        await tx.CommitAsync(cancellationToken);

        if (oldCoverKey != null && oldCoverKey != input.CoverStorageKey)
        {
            try { await storage.DeleteFile(oldCoverKey); }
            catch { await log.LogAsync(userId, "s3_delete_cover_failed", ip, cancellationToken); }
        }

        if (oldContentKey != null && oldContentKey != input.ContentStorageKey)
        {
            try { await storage.DeleteFile(oldContentKey); }
            catch { await log.LogAsync(userId, "s3_delete_content_failed", ip, cancellationToken); }
        }

        var result = await db.Books
            .Include(b => b.Publisher)
            .Include(b => b.Language)
            .Include(b => b.TypeOfWork)
            .Include(b => b.BookAuthors).ThenInclude(ba => ba.Author)
            .Include(b => b.BookGenres).ThenInclude(bg => bg.Genre)
            .Include(b => b.MediaFiles)
            .FirstAsync(b => b.BookId == book.BookId, cancellationToken);

        if (userId.HasValue)
            await log.LogAsync(userId, "update_book", ip, cancellationToken);

        return result;
    }

    [GraphQLName("delete_book")]
    [Authorize]
    public async Task<bool> DeleteBook(
        int bookId,
        [Service] ElibDbContext db,
        [Service] IStorageService storage,
        [Service] ILogService log,
        [Service] IHttpContextAccessor http,
        CancellationToken cancellationToken)
    {
        var userId = GetUserId(http);
        var ip     = http.HttpContext?.Connection.RemoteIpAddress?.ToString();

        var book = await db.Books
            .Include(b => b.MediaFiles)
            .FirstOrDefaultAsync(b => b.BookId == bookId, cancellationToken);
        if (book == null) return false;

        var storageKeys = book.MediaFiles
            .Where(m => !string.IsNullOrEmpty(m.StorageKey))
            .Select(m => m.StorageKey!)
            .ToList();

        db.Books.Remove(book);
        await db.SaveChangesAsync(cancellationToken);

        foreach (var key in storageKeys)
        {
            try { await storage.DeleteFile(key); }
            catch { await log.LogAsync(userId, "s3_delete_book_failed", ip, cancellationToken); }
        }

        if (userId.HasValue)
            await log.LogAsync(userId, "delete_book", ip, cancellationToken);

        return true;
    }

    private static async Task AddNewGenresAsync(
        ElibDbContext db, int bookId, List<string> names, List<int> existingIds, CancellationToken ct)
    {
        foreach (var name in names)
        {
            var trimmed = name.Trim();
            if (string.IsNullOrEmpty(trimmed)) continue;

            var genre = await db.Genres
                .FirstOrDefaultAsync(g => g.GenreName.ToLower() == trimmed.ToLower(), ct);
            if (genre == null)
            {
                genre = new Genre { GenreName = trimmed, ParentGenreId = null };
                db.Genres.Add(genre);
                await db.SaveChangesAsync(ct);
            }
            if (!existingIds.Contains(genre.GenreId))
                db.BookGenres.Add(new BookGenre { BookId = bookId, GenreId = genre.GenreId });
        }
    }

    private static int? GetUserId(IHttpContextAccessor http)
    {
        var claim = http.HttpContext?.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        return claim != null && int.TryParse(claim, out var id) ? id : null;
    }
}
