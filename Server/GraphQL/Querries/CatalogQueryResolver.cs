// GraphQL query resolver exposing reference catalog lists: authors, genres, languages, publishers, and work types.
using HotChocolate;
using Microsoft.EntityFrameworkCore;
using Server.Data;
using Server.GraphQL.Types;

namespace Server.GraphQL.Querries;

[ExtendObjectType<Query>]
public class CatalogQueryResolver
{
    [GraphQLName("authors")]
    public async Task<List<AuthorDto>> GetAuthors(
        [Service] ElibDbContext db,
        CancellationToken cancellationToken)
    {
        var authors = await db.Authors
            .OrderBy(a => a.LastName).ThenBy(a => a.FirstName)
            .ToListAsync(cancellationToken);

        return authors.Select(a => new AuthorDto
        {
            AuthorId    = a.AuthorId.ToString(),
            FirstName   = a.FirstName,
            LastName    = a.LastName,
            YearOfBirth = a.YearOfBirth
        }).ToList();
    }

    [GraphQLName("genres")]
    public async Task<List<GenreDto>> GetGenres(
        [Service] ElibDbContext db,
        CancellationToken cancellationToken)
    {
        var genres = await db.Genres
            .OrderBy(g => g.GenreName)
            .ToListAsync(cancellationToken);

        return genres.Select(g => new GenreDto
        {
            GenreId       = g.GenreId.ToString(),
            GenreName     = g.GenreName,
            ParentGenreId = g.ParentGenreId
        }).ToList();
    }

    [GraphQLName("languages")]
    public async Task<List<LanguageDto>> GetLanguages(
        [Service] ElibDbContext db,
        CancellationToken cancellationToken)
    {
        var languages = await db.Languages
            .OrderBy(l => l.LangName)
            .ToListAsync(cancellationToken);

        return languages.Select(l => new LanguageDto
        {
            LangId   = l.LangId.ToString(),
            LangCode = l.LangCode,
            LangName = l.LangName
        }).ToList();
    }

    [GraphQLName("publishers")]
    public async Task<List<PublisherDto>> GetPublishers(
        [Service] ElibDbContext db,
        CancellationToken cancellationToken)
    {
        var publishers = await db.Publishers
            .OrderBy(p => p.Name)
            .ToListAsync(cancellationToken);

        return publishers.Select(p => new PublisherDto
        {
            PublisherId = p.PublisherId.ToString(),
            Name        = p.Name
        }).ToList();
    }

    [GraphQLName("types_of_work")]
    public async Task<List<TypeOfWorkDto>> GetTypesOfWork(
        [Service] ElibDbContext db,
        CancellationToken cancellationToken)
    {
        var types = await db.TypesOfWorks
            .OrderBy(t => t.TypeName)
            .ToListAsync(cancellationToken);

        return types.Select(t => new TypeOfWorkDto
        {
            WorkId   = t.WorkId.ToString(),
            TypeName = t.TypeName
        }).ToList();
    }
}
