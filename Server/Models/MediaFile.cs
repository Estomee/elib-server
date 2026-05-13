// EF Core model for the MEDIA_FILES table storing metadata for book covers and content files in Yandex S3.
using System;
using System.Collections.Generic;

namespace Server.Models;

public partial class MediaFile
{
    public int MediaId { get; set; }

    public string EntityType { get; set; } = null!;

    public int EntityId { get; set; }

    public string FileType { get; set; } = null!;

    public string FileName { get; set; } = null!;

    public string FilePath { get; set; } = null!;

    public long FileSize { get; set; }

    public string MimeType { get; set; } = null!;

    public string StorageType { get; set; } = null!;

    public string StorageKey { get; set; } = null!;

    public int Width { get; set; }

    public int Height { get; set; }

    public bool? IsPublic { get; set; }

    public int UploadedBy { get; set; }

    public DateTime UploadedAt { get; set; }

    public virtual Book Entity { get; set; } = null!;

    public virtual User UploadedByNavigation { get; set; } = null!;
}
