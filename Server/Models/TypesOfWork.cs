// EF Core model for the TYPES_OF_WORK table storing work type labels such as novel or article.
using System;
using System.Collections.Generic;

namespace Server.Models;

public partial class TypesOfWork
{
    public int WorkId { get; set; }

    public string TypeName { get; set; } = null!;

    public virtual ICollection<Book> Books { get; set; } = new List<Book>();
}
