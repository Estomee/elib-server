// EF Core model for the GENRES table with hierarchical parent-child genre structure.
using System;
using System.Collections.Generic;

namespace Server.Models;

public partial class Genre
{
    public int GenreId { get; set; }

    public string GenreName { get; set; } = null!;

    public int? ParentGenreId { get; set; }

    public virtual ICollection<BookGenre> BookGenres { get; set; } = new List<BookGenre>();

    public virtual ICollection<Genre> InverseParentGenre { get; set; } = new List<Genre>();

    public virtual Genre? ParentGenre { get; set; }
}
