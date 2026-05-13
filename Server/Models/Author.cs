// EF Core model for the AUTHORS table representing book authors.
using System;
using System.Collections.Generic;

namespace Server.Models;

public partial class Author
{
    public int AuthorId { get; set; }

    public string FirstName { get; set; } = null!;

    public string LastName { get; set; } = null!;

    public int YearOfBirth { get; set; }

    public virtual ICollection<BookAuthor> BookAuthors { get; set; } = new List<BookAuthor>();
}
