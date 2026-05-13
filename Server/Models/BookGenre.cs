// EF Core model for the BOOK_GENRES join table linking books to genres (many-to-many).
using System;
using System.Collections.Generic;

namespace Server.Models;

public partial class BookGenre
{
    public int Id { get; set; }

    public int BookId { get; set; }

    public int GenreId { get; set; }

    public virtual Book Book { get; set; } = null!;

    public virtual Genre Genre { get; set; } = null!;
}
