// GraphQL input types for creating and updating books, including author and media fields.
using HotChocolate;

namespace Server.GraphQL.Inputs;

[GraphQLName("NewAuthorInput")]
public class NewAuthorInput
{
    public string FirstName  { get; set; } = string.Empty;
    public string LastName   { get; set; } = string.Empty;
    public int    YearOfBirth { get; set; }
}

[GraphQLName("CreateBookInput")]
public class CreateBookInput
{
    public string? Isbn { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public int YearOfPublishing { get; set; }
    public int PageCount { get; set; }
    public int EditionNumber { get; set; }
    public int PublisherId { get; set; }
    public int TypeOfWorkId { get; set; }
    public int LanguageId { get; set; }
    public string? NewPublisherName  { get; set; }
    public string? NewLanguageName   { get; set; }
    public string? NewLanguageCode   { get; set; }
    public string? NewTypeOfWorkName { get; set; }
    public List<int>           AuthorIds     { get; set; } = new();
    public List<NewAuthorInput> NewAuthors   { get; set; } = new();
    public List<int>           GenreIds      { get; set; } = new();
    public List<string>        NewGenreNames { get; set; } = new();
    public string? CoverStorageKey   { get; set; }
    public string? CoverMimeType     { get; set; }
    public long?   CoverFileSize     { get; set; }
    public int?    CoverWidth        { get; set; }
    public int?    CoverHeight       { get; set; }
    public string? ContentStorageKey { get; set; }
    public string? ContentMimeType   { get; set; }
    public long?   ContentFileSize   { get; set; }
}

[GraphQLName("UpdateBookInput")]
public class UpdateBookInput
{
    public int BookId { get; set; }
    public string? Isbn { get; set; }
    public string? Title { get; set; }
    public string? Description { get; set; }
    public int? YearOfPublishing { get; set; }
    public int? PageCount { get; set; }
    public int? EditionNumber { get; set; }
    public int? PublisherId { get; set; }
    public int? TypeOfWorkId { get; set; }
    public int? LanguageId { get; set; }
    public List<int>?    AuthorIds     { get; set; }
    public List<int>?    GenreIds      { get; set; }
    public List<string>? NewGenreNames { get; set; }
    public string? CoverStorageKey    { get; set; }
    public string? CoverMimeType      { get; set; }
    public long?   CoverFileSize      { get; set; }
    public string? ContentStorageKey  { get; set; }
    public string? ContentMimeType    { get; set; }
    public long?   ContentFileSize    { get; set; }
}
