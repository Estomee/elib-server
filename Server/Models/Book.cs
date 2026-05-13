// EF Core model for the BOOKS table representing the main electronic book catalog.
using System;
using System.Collections.Generic;

namespace Server.Models;

public partial class Book
{
    public int BookId { get; set; }

    public string? Isbn { get; set; }

    public string Title { get; set; } = null!;

    public string Description { get; set; } = null!;

    public int YearOfPublishing { get; set; }

    public int PublisherId { get; set; }

    public int TypeOfWorkId { get; set; }

    public int LanguageId { get; set; }

    public int PageCount { get; set; }

    public int EditionNumber { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime UpdatedAt { get; set; }

    public virtual ICollection<BookAuthor> BookAuthors { get; set; } = new List<BookAuthor>();

    public virtual ICollection<BookGenre> BookGenres { get; set; } = new List<BookGenre>();

    public virtual Language Language { get; set; } = null!;

    public virtual ICollection<MediaFile> MediaFiles { get; set; } = new List<MediaFile>();

    public virtual Publisher Publisher { get; set; } = null!;

    public virtual TypesOfWork TypeOfWork { get; set; } = null!;

    public virtual ICollection<UserBook> UserBooks { get; set; } = new List<UserBook>();
}
