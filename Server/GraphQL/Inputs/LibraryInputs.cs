// GraphQL input types for user library operations and shared pagination/filter inputs.
using HotChocolate;

namespace Server.GraphQL.Inputs;

[GraphQLName("AddBookToLibraryInput")]
public class AddBookToLibraryInput
{
    public int BookId { get; set; }
}

[GraphQLName("RemoveBookFromLibraryInput")]
public class RemoveBookFromLibraryInput
{
    public int BookId { get; set; }
}

[GraphQLName("ReadingProgressInput")]
public class UpdateReadingProgressInput
{
    public int UserBookId { get; set; }
    public int LastReadPage { get; set; }
    public string? Status { get; set; }
}

[GraphQLName("PaginationInput")]
public class PaginationInput
{
    public int? First { get; set; } = 20;
    public string? After { get; set; }
    public string? Before { get; set; }
    public int? Last { get; set; }
}

[GraphQLName("LogPagination")]
public class LogPaginationInput
{
    public int? First { get; set; } = 50;
    public string? After { get; set; }
    public string? Before { get; set; }
    public int? Last { get; set; }
}

[GraphQLName("LogFilter")]
public class LogFilterInput
{
    public string? Action { get; set; }
    public string? LevelName { get; set; }
    public string? UserEmail { get; set; }
    public DateTime? From { get; set; }
    public DateTime? To { get; set; }
}
