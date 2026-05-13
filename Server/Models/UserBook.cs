// EF Core model for the USER_BOOKS table linking users to books in their personal reading library.
using System;
using System.Collections.Generic;

namespace Server.Models;

public partial class UserBook
{
    public int Id { get; set; }

    public int UserId { get; set; }

    public int BookId { get; set; }

    public string? Status { get; set; }

    public DateTime AddedDate { get; set; }

    public DateOnly StartedReading { get; set; }

    public DateOnly FinishedReading { get; set; }

    public int LastReadPage { get; set; }

    public virtual Book Book { get; set; } = null!;

    public virtual User User { get; set; } = null!;
}
