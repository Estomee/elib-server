// EF Core model for the BOOK_AUTHORS join table linking books to authors (many-to-many).
using System;
using System.Collections.Generic;

namespace Server.Models;

public partial class BookAuthor
{
    public int Id { get; set; }

    public int BookId { get; set; }

    public int AuthorId { get; set; }

    public virtual Author Author { get; set; } = null!;

    public virtual Book Book { get; set; } = null!;
}
