// GraphQL object type configuration for Book with custom field resolvers and shared catalog DTO classes.
using HotChocolate;
using HotChocolate.Resolvers;
using HotChocolate.Types;
using Server.Models;
using Server.Services;

namespace Server.GraphQL.Types;

public class BookObjectType : ObjectType<Book>
{
    protected override void Configure(IObjectTypeDescriptor<Book> descriptor)
    {
        descriptor.Ignore(b => b.PublisherId);
        descriptor.Ignore(b => b.TypeOfWorkId);
        descriptor.Ignore(b => b.LanguageId);
        descriptor.Ignore(b => b.Publisher);
        descriptor.Ignore(b => b.Language);
        descriptor.Ignore(b => b.TypeOfWork);
        descriptor.Ignore(b => b.BookAuthors);
        descriptor.Ignore(b => b.BookGenres);
        descriptor.Ignore(b => b.UserBooks);
        descriptor.Ignore(b => b.MediaFiles);

        descriptor.Field("book_cover")
            .Type<ObjectType<MediaFileDto>>()
            .Resolve(async ctx =>
            {
                var book    = ctx.Parent<Book>();
                var storage = ctx.Service<IStorageService>();
                var cover   = book.MediaFiles?.FirstOrDefault(m => m.FileType == "cover");
                if (cover == null) return (object?)null;
                var url = await storage.GetFile(cover.StorageKey);
                return (object?)new MediaFileDto
                {
                    MediaId     = cover.MediaId,
                    EntityType  = cover.EntityType,
                    FileType    = cover.FileType,
                    FileName    = cover.FileName,
                    FilePath    = url,
                    FileSize    = cover.FileSize,
                    MimeType    = cover.MimeType,
                    StorageType = cover.StorageType,
                    StorageKey  = cover.StorageKey,
                    Width       = cover.Width,
                    Height      = cover.Height,
                    IsPublic    = cover.IsPublic ?? false,
                    UploadedBy  = cover.UploadedBy,
                    UploadedAt  = cover.UploadedAt
                };
            });

        descriptor.Field("book_content")
            .Type<ObjectType<MediaFileDto>>()
            .Resolve(ctx =>
            {
                var book    = ctx.Parent<Book>();
                var content = book.MediaFiles?.FirstOrDefault(m => m.FileType == "content");
                if (content == null) return (object?)null;
                return (object?)new MediaFileDto
                {
                    MediaId     = content.MediaId,
                    EntityType  = content.EntityType,
                    FileType    = content.FileType,
                    FileName    = content.FileName,
                    FilePath    = content.FilePath,
                    FileSize    = content.FileSize,
                    MimeType    = content.MimeType,
                    StorageType = content.StorageType,
                    StorageKey  = content.StorageKey,
                    Width       = content.Width,
                    Height      = content.Height,
                    IsPublic    = content.IsPublic ?? false,
                    UploadedBy  = content.UploadedBy,
                    UploadedAt  = content.UploadedAt
                };
            });
    }
}

[ExtendObjectType("Book")]
public class BookTypeExtensions
{
    [GraphQLName("authors")]
    public IEnumerable<AuthorDto> GetAuthors([Parent] Book book)
        => book.BookAuthors.Select(ba => new AuthorDto
        {
            AuthorId    = ba.Author.AuthorId.ToString(),
            FirstName   = ba.Author.FirstName,
            LastName    = ba.Author.LastName,
            YearOfBirth = ba.Author.YearOfBirth
        });

    [GraphQLName("genres")]
    public IEnumerable<GenreDto> GetGenres([Parent] Book book)
        => book.BookGenres.Select(bg => new GenreDto
        {
            GenreId      = bg.Genre.GenreId.ToString(),
            GenreName    = bg.Genre.GenreName,
            ParentGenreId = bg.Genre.ParentGenreId
        });

    [GraphQLName("publisher")]
    public PublisherDto? GetPublisher([Parent] Book book)
        => book.Publisher == null ? null : new PublisherDto
        {
            PublisherId = book.Publisher.PublisherId.ToString(),
            Name        = book.Publisher.Name
        };

    [GraphQLName("language")]
    public LanguageDto? GetLanguage([Parent] Book book)
        => book.Language == null ? null : new LanguageDto
        {
            LangId   = book.Language.LangId.ToString(),
            LangCode = book.Language.LangCode,
            LangName = book.Language.LangName
        };

    [GraphQLName("type_of_work")]
    public TypeOfWorkDto? GetTypeOfWork([Parent] Book book)
        => book.TypeOfWork == null ? null : new TypeOfWorkDto
        {
            WorkId   = book.TypeOfWork.WorkId.ToString(),
            TypeName = book.TypeOfWork.TypeName
        };
}

public interface IBookDto
{
    string BookId { get; set; }
    string? Isbn { get; set; }
    string Title { get; set; }
    string Description { get; set; }
    int YearOfPublishing { get; set; }
    int PageCount { get; set; }
    int EditionNumber { get; set; }
    DateTime CreatedAt { get; set; }
    DateTime UpdatedAt { get; set; }
    PublisherDto Publisher { get; set; }
    MediaFileDto? BookCover { get; set; }
    List<MediaFileDto> BookFile { get; set; }
    TypeOfWorkDto TypeOfWork { get; set; }
    LanguageDto Language { get; set; }
    List<AuthorDto> Authors { get; set; }
    List<GenreDto> Genres { get; set; }
}

public class MediaFileDto
{
    public int MediaId { get; set; }
    public string EntityType { get; set; } = string.Empty;
    public string FileType { get; set; } = string.Empty;
    public string FileName { get; set; } = string.Empty;
    public string FilePath { get; set; } = string.Empty;
    public long FileSize { get; set; }
    public string MimeType { get; set; } = string.Empty;
    public string StorageType { get; set; } = string.Empty;
    public string StorageKey { get; set; } = string.Empty;
    public int Width { get; set; }
    public int Height { get; set; }
    public bool IsPublic { get; set; }
    public int UploadedBy { get; set; }
    public DateTime UploadedAt { get; set; }
}

public class AuthorDto
{
    public string AuthorId { get; set; } = string.Empty;
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public int YearOfBirth { get; set; }
}

public class GenreDto
{
    public string GenreId { get; set; } = string.Empty;
    public string GenreName { get; set; } = string.Empty;
    public int? ParentGenreId { get; set; }
}

public class PublisherDto
{
    public string PublisherId { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
}

public class TypeOfWorkDto
{
    public string WorkId { get; set; } = string.Empty;
    public string TypeName { get; set; } = string.Empty;
}

public class LanguageDto
{
    public string LangId { get; set; } = string.Empty;
    public string LangCode { get; set; } = string.Empty;
    public string LangName { get; set; } = string.Empty;
}
