// EF Core model for the PUBLISHERS table storing book publisher names.
using System;
using System.Collections.Generic;

namespace Server.Models;

public partial class Publisher
{
    public int PublisherId { get; set; }

    public string Name { get; set; } = null!;

    public virtual ICollection<Book> Books { get; set; } = new List<Book>();
}
