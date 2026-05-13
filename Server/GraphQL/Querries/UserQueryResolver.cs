// GraphQL query resolver for fetching authenticated user profile, paginated user lists, and user book libraries.
using System.Security.Claims;
using HotChocolate;
using HotChocolate.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Server.Data;
using Server.GraphQL.Inputs;
using Server.GraphQL.Types;
using Server.Models;
using Server.Services;

namespace Server.GraphQL.Querries;

[ExtendObjectType<Query>]
public class UserQueryResolver
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

    [GraphQLName("me")]
    [Authorize]
    public async Task<UserDto?> GetMe(
        [Service] ElibDbContext db,
        [Service] IHttpContextAccessor http,
        CancellationToken cancellationToken)
    {
        var userId = GetUserId(http);
        if (userId == null) return null;

        var user = await db.Users
            .Include(u => u.UserSessions)
            .FirstOrDefaultAsync(u => u.UserId == userId, cancellationToken);

        if (user == null)
            throw new GraphQLException(
                ErrorBuilder.New().SetMessage("Сессия недействительна. Войдите снова.")
                    .SetCode("SESSION_INVALID").Build());

        return AuthService.MapUser(user);
    }

    [GraphQLName("users")]
    [Authorize]
    public async Task<UsersConnection> GetUsers(
        [Service] ElibDbContext db,
        PaginationInput? pagination = null,
        UsersFilterInput? filter = null,
        CancellationToken cancellationToken = default)
    {
        var afterId    = DecodeCursor(pagination?.After);
        var beforeId   = DecodeCursor(pagination?.Before);
        var isBackward = pagination?.Last.HasValue == true && beforeId.HasValue;
        var pageSize   = isBackward ? pagination!.Last!.Value : pagination?.First ?? 20;

        var query = db.Users.AsQueryable();

        if (!string.IsNullOrEmpty(filter?.Search))
            query = query.Where(u =>
                u.Email.Contains(filter.Search) ||
                u.Username.Contains(filter.Search) ||
                u.FirstName.Contains(filter.Search) ||
                u.LastName.Contains(filter.Search));

        if (filter?.EmployeeOnly == true)
            query = query.Where(u => u.Employee);

        var totalCount = await query.CountAsync(cancellationToken);

        List<User> users;
        if (isBackward)
        {
            query = query.Where(u => u.UserId < beforeId!.Value);
            users = await query.OrderByDescending(u => u.UserId).Take(pageSize + 1).ToListAsync(cancellationToken);
            users.Reverse();
        }
        else
        {
            if (afterId.HasValue) query = query.Where(u => u.UserId > afterId.Value);
            users = await query.OrderBy(u => u.UserId).Take(pageSize + 1).ToListAsync(cancellationToken);
        }

        var hasExtra = users.Count > pageSize;
        if (hasExtra) users.RemoveAt(isBackward ? 0 : users.Count - 1);

        var edges = users.Select(u => new UserEdge
        {
            Cursor = EncodeCursor(u.UserId),
            Node   = AuthService.MapUser(u)
        }).ToList();

        return new UsersConnection
        {
            TotalCount = totalCount,
            PageSize   = pageSize,
            Nodes      = edges.Select(e => e.Node).ToList(),
            Edges      = edges,
            PageInfo   = new UserPageInfo
            {
                StartCursor     = edges.FirstOrDefault()?.Cursor,
                EndCursor       = edges.LastOrDefault()?.Cursor,
                HasNextPage     = isBackward ? beforeId.HasValue : hasExtra,
                HasPreviousPage = isBackward ? hasExtra : afterId.HasValue
            }
        };
    }

    [GraphQLName("user_books")]
    [Authorize]
    public async Task<UserBooksConnection> GetUserBooks(
        int userId,
        [Service] ElibDbContext db,
        [Service] IStorageService storage,
        PaginationInput? pagination = null,
        string? status = null,
        CancellationToken cancellationToken = default)
    {
        var afterId    = DecodeCursor(pagination?.After);
        var beforeId   = DecodeCursor(pagination?.Before);
        var isBackward = pagination?.Last.HasValue == true && beforeId.HasValue;
        var pageSize   = isBackward ? pagination!.Last!.Value : pagination?.First ?? 20;

        var query = db.UserBooks
            .Include(ub => ub.Book)
                .ThenInclude(b => b.Publisher)
            .Include(ub => ub.Book)
                .ThenInclude(b => b.Language)
            .Include(ub => ub.Book)
                .ThenInclude(b => b.TypeOfWork)
            .Include(ub => ub.Book)
                .ThenInclude(b => b.BookAuthors).ThenInclude(ba => ba.Author)
            .Include(ub => ub.Book)
                .ThenInclude(b => b.BookGenres).ThenInclude(bg => bg.Genre)
            .Include(ub => ub.Book)
                .ThenInclude(b => b.MediaFiles)
            .Include(ub => ub.User)
            .Where(ub => ub.UserId == userId)
            .AsQueryable();

        if (!string.IsNullOrEmpty(status))
            query = query.Where(ub => ub.Status == status);

        var totalCount = await query.CountAsync(cancellationToken);

        List<UserBook> items;
        if (isBackward)
        {
            query = query.Where(ub => ub.Id < beforeId!.Value);
            items = await query.OrderByDescending(ub => ub.Id).Take(pageSize + 1).ToListAsync(cancellationToken);
            items.Reverse();
        }
        else
        {
            if (afterId.HasValue) query = query.Where(ub => ub.Id > afterId.Value);
            items = await query.OrderBy(ub => ub.Id).Take(pageSize + 1).ToListAsync(cancellationToken);
        }

        var hasExtra = items.Count > pageSize;
        if (hasExtra) items.RemoveAt(isBackward ? 0 : items.Count - 1);

        var nodes = await Task.WhenAll(items.Select(ub => MapUserBookAsync(ub, storage)));

        var edges = nodes.Select(node => new UserBookEdge
        {
            Cursor = EncodeCursor(int.Parse(node.UserBookId)),
            Node   = node
        }).ToList();

        return new UserBooksConnection
        {
            TotalCount = totalCount,
            PageSize   = pageSize,
            Nodes      = nodes.ToList(),
            Edges      = edges,
            PageInfo   = new UserBookPageInfo
            {
                StartCursor     = edges.FirstOrDefault()?.Cursor,
                EndCursor       = edges.LastOrDefault()?.Cursor,
                HasNextPage     = isBackward ? beforeId.HasValue : hasExtra,
                HasPreviousPage = isBackward ? hasExtra : afterId.HasValue
            }
        };
    }

    private static int? GetUserId(IHttpContextAccessor http)
    {
        var claim = http.HttpContext?.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        return claim != null && int.TryParse(claim, out var id) ? id : null;
    }

    private static async Task<UserBookItemDto> MapUserBookAsync(UserBook ub, IStorageService storage)
    {
        var coverFile = ub.Book.MediaFiles?.FirstOrDefault(m => m.FileType == "cover");
        MediaFileDto? bookCover = null;
        if (coverFile != null)
        {
            string? url = null;
            try { url = await storage.GetFile(coverFile.StorageKey); } catch { }
            if (!string.IsNullOrEmpty(url))
                bookCover = new MediaFileDto { FilePath = url, StorageKey = coverFile.StorageKey };
        }

        return new UserBookItemDto
        {
            UserBookId   = ub.Id.ToString(),
            Status       = ub.Status ?? "reading",
            LastReadPage = ub.LastReadPage,
            AddedAt      = ub.AddedDate,
            Book         = new UserBookNestedBook
            {
                BookId           = ub.BookId.ToString(),
                Title            = ub.Book.Title,
                Description      = ub.Book.Description,
                PageCount        = ub.Book.PageCount,
                YearOfPublishing = ub.Book.YearOfPublishing,
                BookCover        = bookCover,
                Authors = ub.Book.BookAuthors.Select(ba => new AuthorDto
                {
                    AuthorId    = ba.AuthorId.ToString(),
                    FirstName   = ba.Author.FirstName,
                    LastName    = ba.Author.LastName,
                    YearOfBirth = ba.Author.YearOfBirth
                }).ToList(),
                Genres = ub.Book.BookGenres.Select(bg => new GenreDto
                {
                    GenreId       = bg.GenreId.ToString(),
                    GenreName     = bg.Genre.GenreName,
                    ParentGenreId = bg.Genre.ParentGenreId
                }).ToList(),
                Publisher = ub.Book.Publisher != null
                    ? new PublisherDto { PublisherId = ub.Book.PublisherId.ToString(), Name = ub.Book.Publisher.Name }
                    : null,
                Language = ub.Book.Language != null
                    ? new LanguageDto { LangId = ub.Book.LanguageId.ToString(), LangCode = ub.Book.Language.LangCode, LangName = ub.Book.Language.LangName }
                    : null,
            }
        };
    }
}

public class UsersConnection
{
    public List<UserEdge> Edges { get; set; } = new();
    public List<UserDto> Nodes { get; set; } = new();
    public int TotalCount { get; set; }
    public UserPageInfo PageInfo { get; set; } = new();
    public int PageSize { get; set; }
}

public class UserEdge
{
    public string Cursor { get; set; } = string.Empty;
    public UserDto Node { get; set; } = new();
}

public class UserPageInfo
{
    public string? StartCursor { get; set; }
    public string? EndCursor { get; set; }
    public bool HasNextPage { get; set; }
    public bool HasPreviousPage { get; set; }
}

public class UserBooksConnection
{
    public List<UserBookEdge> Edges { get; set; } = new();
    public List<UserBookItemDto> Nodes { get; set; } = new();
    public int TotalCount { get; set; }
    public UserBookPageInfo PageInfo { get; set; } = new();
    public int PageSize { get; set; }
}

public class UserBookEdge
{
    public string Cursor { get; set; } = string.Empty;
    public UserBookItemDto Node { get; set; } = new();
}

public class UserBookPageInfo
{
    public string? StartCursor { get; set; }
    public string? EndCursor { get; set; }
    public bool HasNextPage { get; set; }
    public bool HasPreviousPage { get; set; }
}
