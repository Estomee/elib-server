// GraphQL mutation resolver for managing a user's personal book library and reading progress.
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

namespace Server.GraphQL.Mutations;

[ExtendObjectType<Mutation>]
public class UserLibraryMutationResolver
{
    [GraphQLName("add_book_to_library")]
    [Authorize]
    public async Task<AddBookToLibraryPayload> AddBookToLibrary(
        AddBookToLibraryInput input,
        [Service] ElibDbContext db,
        [Service] ILogService log,
        [Service] IHttpContextAccessor http,
        CancellationToken cancellationToken)
    {
        var userId = RequireUserId(http);
        var ip     = http.HttpContext?.Connection.RemoteIpAddress?.ToString();

        var existing = await db.UserBooks
            .FirstOrDefaultAsync(ub => ub.UserId == userId && ub.BookId == input.BookId, cancellationToken);

        if (existing != null)
            return new AddBookToLibraryPayload { Id = existing.Id, Status = existing.Status ?? "reading", IsNew = false };

        var bookExists = await db.Books.AnyAsync(b => b.BookId == input.BookId, cancellationToken);
        if (!bookExists)
            throw new GraphQLException(
                ErrorBuilder.New().SetMessage("Книга не найдена.").SetCode("BOOK_NOT_FOUND").Build());

        var now = DateOnly.FromDateTime(DateTime.UtcNow);
        var userBook = new UserBook
        {
            UserId          = userId,
            BookId          = input.BookId,
            Status          = "reading",
            AddedDate       = DbTime.Now,
            StartedReading  = now,
            FinishedReading = DateOnly.MinValue,
            LastReadPage    = 0
        };

        db.UserBooks.Add(userBook);
        await db.SaveChangesAsync(cancellationToken);

        await log.LogAsync(userId, "add_book_to_library", ip, cancellationToken);

        return new AddBookToLibraryPayload { Id = userBook.Id, Status = userBook.Status, IsNew = true };
    }

    [GraphQLName("remove_book_from_library")]
    [Authorize]
    public async Task<bool> RemoveBookFromLibrary(
        int userBookId,
        [Service] ElibDbContext db,
        [Service] ILogService log,
        [Service] IHttpContextAccessor http,
        CancellationToken cancellationToken)
    {
        var userId = RequireUserId(http);
        var ip     = http.HttpContext?.Connection.RemoteIpAddress?.ToString();

        var userBook = await db.UserBooks
            .FirstOrDefaultAsync(ub => ub.Id == userBookId && ub.UserId == userId, cancellationToken);

        if (userBook == null) return false;

        db.UserBooks.Remove(userBook);
        await db.SaveChangesAsync(cancellationToken);

        await log.LogAsync(userId, "remove_book_from_library", ip, cancellationToken);

        return true;
    }

    [GraphQLName("update_reading_progress")]
    [Authorize]
    public async Task<UserBook> UpdateReadingProgress(
        UpdateReadingProgressInput input,
        [Service] ElibDbContext db,
        [Service] ILogService log,
        [Service] IHttpContextAccessor http,
        CancellationToken cancellationToken)
    {
        var userId = RequireUserId(http);
        var ip     = http.HttpContext?.Connection.RemoteIpAddress?.ToString();

        var userBook = await db.UserBooks
            .FirstOrDefaultAsync(ub => ub.Id == input.UserBookId && ub.UserId == userId, cancellationToken);

        if (userBook == null)
            throw new GraphQLException(
                ErrorBuilder.New().SetMessage("Книга не найдена в библиотеке пользователя.").SetCode("USER_BOOK_NOT_FOUND").Build());

        userBook.LastReadPage = input.LastReadPage;

        if (!string.IsNullOrEmpty(input.Status))
        {
            userBook.Status = input.Status;
            if (input.Status == "finished")
                userBook.FinishedReading = DateOnly.FromDateTime(DateTime.UtcNow);
        }

        await db.SaveChangesAsync(cancellationToken);

        await log.LogAsync(userId, "update_reading_progress", ip, cancellationToken);

        return userBook;
    }

    private static int RequireUserId(IHttpContextAccessor http)
    {
        var claim = http.HttpContext?.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (claim == null || !int.TryParse(claim, out var id))
            throw new GraphQLException(
                ErrorBuilder.New().SetMessage("Требуется аутентификация.").SetCode("UNAUTHORIZED").Build());
        return id;
    }
}
