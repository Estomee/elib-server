// EF Core model for the LANGUAGES table storing book language codes and names.
using System;
using System.Collections.Generic;

namespace Server.Models;

public partial class Language
{
    public int LangId { get; set; }

    public string LangCode { get; set; } = null!;

    public string LangName { get; set; } = null!;

    public virtual ICollection<Book> Books { get; set; } = new List<Book>();
}
