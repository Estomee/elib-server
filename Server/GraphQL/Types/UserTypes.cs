// GraphQL DTO types for user data: user profile, sessions, user book library items, and library payloads.
namespace Server.GraphQL.Types;

public enum UserBookStatus
{
    Reading,
    Finished
}

public class DeviceInfoDto
{
    public string DeviceId { get; set; } = string.Empty;
    public string DeviceType { get; set; } = string.Empty;
    public string Os { get; set; } = string.Empty;
}

public class UserSessionDto
{
    public string SessionId { get; set; } = string.Empty;
    public DateTime? CreatedAt { get; set; }
    public DateTime ExpiresAt { get; set; }
    public string IpAddress { get; set; } = string.Empty;
}

public class UserDto
{
    public string UserId { get; set; } = string.Empty;
    public string Username { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string? MiddleName { get; set; }
    public string Phone { get; set; } = string.Empty;
    public int YearOfBirth { get; set; }
    public bool Employee { get; set; }
    public DateTime RegistrationDate { get; set; }
    public DateTime LastLogin { get; set; }
    public DateTime LastPasswordChange { get; set; }
    public List<UserSessionDto> Sessions { get; set; } = new();
    public int BooksCount { get; set; }
}

public class UserBookNestedBook
{
    public string BookId { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public int PageCount { get; set; }
    public int YearOfPublishing { get; set; }
    public MediaFileDto? BookCover { get; set; }
    public List<AuthorDto> Authors { get; set; } = new();
    public List<GenreDto> Genres { get; set; } = new();
    public PublisherDto? Publisher { get; set; }
    public LanguageDto? Language { get; set; }
}

public class AddBookToLibraryPayload
{
    public int Id { get; set; }
    public string Status { get; set; } = string.Empty;
    public bool IsNew { get; set; }
}

public class UserBookItemDto
{
    public string UserBookId { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public int LastReadPage { get; set; }
    public DateTime AddedAt { get; set; }
    public UserBookNestedBook Book { get; set; } = new();
}
