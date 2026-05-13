// EF Core DbContext with all entity sets and model configuration for the ELib database.
using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore;
using Server.Models;

namespace Server.Data;

public partial class ElibDbContext : DbContext
{
    public ElibDbContext(DbContextOptions<ElibDbContext> options)
        : base(options)
    {
    }

    public virtual DbSet<Author> Authors { get; set; }

    public virtual DbSet<Book> Books { get; set; }

    public virtual DbSet<BookAuthor> BookAuthors { get; set; }

    public virtual DbSet<BookGenre> BookGenres { get; set; }

    public virtual DbSet<Employee> Employees { get; set; }

    public virtual DbSet<Genre> Genres { get; set; }

    public virtual DbSet<Language> Languages { get; set; }

    public virtual DbSet<LevelOfLog> LevelOfLogs { get; set; }

    public virtual DbSet<LogActionConfig> LogActionConfigs { get; set; }

    public virtual DbSet<MediaFile> MediaFiles { get; set; }

    public virtual DbSet<Notification> Notifications { get; set; }

    public virtual DbSet<PasswordResetCode> PasswordResetCodes { get; set; }

    public virtual DbSet<Position> Positions { get; set; }

    public virtual DbSet<Publisher> Publishers { get; set; }

    public virtual DbSet<SystemLog> SystemLogs { get; set; }

    public virtual DbSet<TypesOfWork> TypesOfWorks { get; set; }

    public virtual DbSet<User> Users { get; set; }

    public virtual DbSet<UserBook> UserBooks { get; set; }

    public virtual DbSet<UserSession> UserSessions { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder
            .HasPostgresEnum("auth", "aal_level", new[] { "aal1", "aal2", "aal3" })
            .HasPostgresEnum("auth", "code_challenge_method", new[] { "s256", "plain" })
            .HasPostgresEnum("auth", "factor_status", new[] { "unverified", "verified" })
            .HasPostgresEnum("auth", "factor_type", new[] { "totp", "webauthn", "phone" })
            .HasPostgresEnum("auth", "oauth_authorization_status", new[] { "pending", "approved", "denied", "expired" })
            .HasPostgresEnum("auth", "oauth_client_type", new[] { "public", "confidential" })
            .HasPostgresEnum("auth", "oauth_registration_type", new[] { "dynamic", "manual" })
            .HasPostgresEnum("auth", "oauth_response_type", new[] { "code" })
            .HasPostgresEnum("auth", "one_time_token_type", new[] { "confirmation_token", "reauthentication_token", "recovery_token", "email_change_token_new", "email_change_token_current", "phone_change_token" })
            .HasPostgresEnum("realtime", "action", new[] { "INSERT", "UPDATE", "DELETE", "TRUNCATE", "ERROR" })
            .HasPostgresEnum("realtime", "equality_op", new[] { "eq", "neq", "lt", "lte", "gt", "gte", "in" })
            .HasPostgresEnum("storage", "buckettype", new[] { "STANDARD", "ANALYTICS", "VECTOR" })
            .HasPostgresExtension("extensions", "pg_stat_statements")
            .HasPostgresExtension("extensions", "pgcrypto")
            .HasPostgresExtension("extensions", "uuid-ossp")
            .HasPostgresExtension("graphql", "pg_graphql")
            .HasPostgresExtension("vault", "supabase_vault");

        modelBuilder.Entity<Author>(entity =>
        {
            entity.HasKey(e => e.AuthorId).HasName("AUTHORS_pkey");

            entity.ToTable("AUTHORS", tb => tb.HasComment("Авторы книг в библиотеке с отдельными полями имени"));

            entity.HasIndex(e => new { e.LastName, e.FirstName }, "idx_authors_name");

            entity.Property(e => e.AuthorId).HasColumnName("author_id");
            entity.Property(e => e.FirstName)
                .HasMaxLength(30)
                .HasColumnName("first_name");
            entity.Property(e => e.LastName)
                .HasMaxLength(30)
                .HasColumnName("last_name");
            entity.Property(e => e.YearOfBirth).HasColumnName("year_of_birth");
        });

        modelBuilder.Entity<Book>(entity =>
        {
            entity.HasKey(e => e.BookId).HasName("BOOKS_pkey");

            entity.ToTable("BOOKS", tb => tb.HasComment("Основная таблица электронных книг в библиотеке"));

            entity.HasIndex(e => e.Isbn, "BOOKS_isbn_key").IsUnique();

            entity.HasIndex(e => e.Title, "idx_books_title");

            entity.HasIndex(e => e.YearOfPublishing, "idx_books_year");

            entity.Property(e => e.BookId).HasColumnName("book_id");
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("now()")
                .HasColumnType("timestamp without time zone")
                .HasColumnName("created_at");
            entity.Property(e => e.Description).HasColumnName("description");
            entity.Property(e => e.EditionNumber).HasColumnName("edition_number");
            entity.Property(e => e.Isbn)
                .HasMaxLength(17)
                .HasColumnName("isbn");
            entity.Property(e => e.LanguageId).HasColumnName("language_id");
            entity.Property(e => e.PageCount).HasColumnName("page_count");
            entity.Property(e => e.PublisherId).HasColumnName("publisher_id");
            entity.Property(e => e.Title)
                .HasMaxLength(200)
                .HasColumnName("title");
            entity.Property(e => e.TypeOfWorkId).HasColumnName("type_of_work_id");
            entity.Property(e => e.UpdatedAt)
                .HasDefaultValueSql("now()")
                .HasColumnType("timestamp without time zone")
                .HasColumnName("updated_at");
            entity.Property(e => e.YearOfPublishing).HasColumnName("year_of_publishing");

            entity.HasOne(d => d.Language).WithMany(p => p.Books)
                .HasForeignKey(d => d.LanguageId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("BOOKS_language_id_fkey");

            entity.HasOne(d => d.Publisher).WithMany(p => p.Books)
                .HasForeignKey(d => d.PublisherId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("BOOKS_publisher_id_fkey");

            entity.HasOne(d => d.TypeOfWork).WithMany(p => p.Books)
                .HasForeignKey(d => d.TypeOfWorkId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("BOOKS_type_of_work_id_fkey");
        });

        modelBuilder.Entity<BookAuthor>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("BOOK_AUTHORS_pkey");

            entity.ToTable("BOOK_AUTHORS", tb => tb.HasComment("Связь книг с авторами (многие-ко-многим)"));

            entity.HasIndex(e => new { e.BookId, e.AuthorId }, "uq_book_author").IsUnique();

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.AuthorId).HasColumnName("author_id");
            entity.Property(e => e.BookId).HasColumnName("book_id");

            entity.HasOne(d => d.Author).WithMany(p => p.BookAuthors)
                .HasForeignKey(d => d.AuthorId)
                .OnDelete(DeleteBehavior.Restrict)
                .HasConstraintName("BOOK_AUTHORS_author_id_fkey");

            entity.HasOne(d => d.Book).WithMany(p => p.BookAuthors)
                .HasForeignKey(d => d.BookId)
                .OnDelete(DeleteBehavior.Cascade)
                .HasConstraintName("BOOK_AUTHORS_book_id_fkey");
        });

        modelBuilder.Entity<BookGenre>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("BOOK_GENRES_pkey");

            entity.ToTable("BOOK_GENRES", tb => tb.HasComment("Связь книг с жанрами (многие-ко-многим)"));

            entity.HasIndex(e => new { e.BookId, e.GenreId }, "uq_book_genre").IsUnique();

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.BookId).HasColumnName("book_id");
            entity.Property(e => e.GenreId).HasColumnName("genre_id");

            entity.HasOne(d => d.Book).WithMany(p => p.BookGenres)
                .HasForeignKey(d => d.BookId)
                .OnDelete(DeleteBehavior.Cascade)
                .HasConstraintName("BOOK_GENRES_book_id_fkey");

            entity.HasOne(d => d.Genre).WithMany(p => p.BookGenres)
                .HasForeignKey(d => d.GenreId)
                .OnDelete(DeleteBehavior.Restrict)
                .HasConstraintName("BOOK_GENRES_genre_id_fkey");
        });

        modelBuilder.Entity<Employee>(entity =>
        {
            entity.HasKey(e => e.EmployeeId).HasName("EMPLOYEES_pkey");

            entity.ToTable("EMPLOYEES", tb => tb.HasComment("Сотрудники библиотеки (администраторы)"));

            entity.HasIndex(e => e.UserId, "EMPLOYEES_user_id_key").IsUnique();

            entity.HasIndex(e => new { e.PositionId, e.UserId }, "idx_emlpoyees_search");

            entity.Property(e => e.EmployeeId).HasColumnName("employee_id");
            entity.Property(e => e.HireDate).HasColumnName("hire_date");
            entity.Property(e => e.PassportNumber)
                .HasMaxLength(50)
                .HasColumnName("passport_number");
            entity.Property(e => e.PassportSeries)
                .HasMaxLength(30)
                .HasColumnName("passport_series");
            entity.Property(e => e.PositionId).HasColumnName("position_id");
            entity.Property(e => e.TerminationDate).HasColumnName("termination_date");
            entity.Property(e => e.UserId).HasColumnName("user_id");
            entity.Property(e => e.WorkSchedule)
                .HasColumnType("json")
                .HasColumnName("work_schedule");

            entity.HasOne(d => d.Position).WithMany(p => p.Employees)
                .HasForeignKey(d => d.PositionId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("EMPLOYEES_position_id_fkey");

            entity.HasOne(d => d.User).WithOne(p => p.EmployeeNavigation)
                .HasForeignKey<Employee>(d => d.UserId)
                .OnDelete(DeleteBehavior.Cascade)
                .HasConstraintName("EMPLOYEES_user_id_fkey");
        });

        modelBuilder.Entity<Genre>(entity =>
        {
            entity.HasKey(e => e.GenreId).HasName("GENRES_pkey");

            entity.ToTable("GENRES", tb => tb.HasComment("Иерархическая структура жанров книг"));

            entity.HasIndex(e => e.GenreName, "GENRES_genre_name_key").IsUnique();

            entity.HasIndex(e => e.ParentGenreId, "idx_genres_parent");

            entity.Property(e => e.GenreId).HasColumnName("genre_id");
            entity.Property(e => e.GenreName)
                .HasMaxLength(50)
                .HasColumnName("genre_name");
            entity.Property(e => e.ParentGenreId).HasColumnName("parent_genre_id");

            entity.HasOne(d => d.ParentGenre).WithMany(p => p.InverseParentGenre)
                .HasForeignKey(d => d.ParentGenreId)
                .OnDelete(DeleteBehavior.SetNull)
                .HasConstraintName("GENRES_parent_genre_id_fkey");
        });

        modelBuilder.Entity<Language>(entity =>
        {
            entity.HasKey(e => e.LangId).HasName("LANGUAGES_pkey");

            entity.ToTable("LANGUAGES", tb => tb.HasComment("Языки книг"));

            entity.HasIndex(e => e.LangCode, "LANGUAGES_lang_code_key").IsUnique();

            entity.Property(e => e.LangId).HasColumnName("lang_id");
            entity.Property(e => e.LangCode)
                .HasMaxLength(5)
                .HasColumnName("lang_code");
            entity.Property(e => e.LangName)
                .HasMaxLength(50)
                .HasColumnName("lang_name");
        });

        modelBuilder.Entity<LevelOfLog>(entity =>
        {
            entity.HasKey(e => e.LevelId).HasName("LEVEL_OF_LOGS_pkey");

            entity.ToTable("LEVEL_OF_LOGS");

            entity.HasIndex(e => e.Color, "LEVEL_OF_LOGS_color_key").IsUnique();

            entity.HasIndex(e => e.LevelName, "LEVEL_OF_LOGS_level_name_key").IsUnique();

            entity.Property(e => e.LevelId).HasColumnName("level_id");
            entity.Property(e => e.Color)
                .HasMaxLength(9)
                .HasColumnName("color");
            entity.Property(e => e.LevelName)
                .HasMaxLength(20)
                .HasColumnName("level_name");
        });

        modelBuilder.Entity<LogActionConfig>(entity =>
        {
            entity.HasKey(e => e.ActionName).HasName("LOG_ACTION_CONFIGS_pkey");

            entity.ToTable("LOG_ACTION_CONFIGS");

            entity.Property(e => e.ActionName).HasMaxLength(100).HasColumnName("action_name");
            entity.Property(e => e.LevelId).HasColumnName("level_id");
            entity.Property(e => e.Description).HasColumnName("description");

            entity.HasOne(e => e.Level)
                .WithMany()
                .HasForeignKey(e => e.LevelId)
                .OnDelete(DeleteBehavior.Restrict)
                .HasConstraintName("LOG_ACTION_CONFIGS_level_id_fkey");
        });

        modelBuilder.Entity<MediaFile>(entity =>
        {
            entity.HasKey(e => e.MediaId).HasName("MEDIA_FILES_pkey");

            entity.ToTable("MEDIA_FILES", tb => tb.HasComment("Медиафайлы (обложки, электронные книги) в Яндекс.Облаке"));

            entity.HasIndex(e => new { e.EntityType, e.EntityId }, "idx_media_entity");

            entity.HasIndex(e => new { e.StorageType, e.StorageKey }, "idx_media_storage");

            entity.HasIndex(e => e.UploadedBy, "idx_media_uploader");

            entity.Property(e => e.MediaId).HasColumnName("media_id");
            entity.Property(e => e.EntityId).HasColumnName("entity_id");
            entity.Property(e => e.EntityType)
                .HasMaxLength(30)
                .HasColumnName("entity_type");
            entity.Property(e => e.FileName)
                .HasMaxLength(255)
                .HasColumnName("file_name");
            entity.Property(e => e.FilePath)
                .HasMaxLength(500)
                .HasColumnName("file_path");
            entity.Property(e => e.FileSize).HasColumnName("file_size");
            entity.Property(e => e.FileType)
                .HasMaxLength(20)
                .HasColumnName("file_type");
            entity.Property(e => e.Height).HasColumnName("height");
            entity.Property(e => e.IsPublic)
                .HasDefaultValue(true)
                .HasColumnName("is_public");
            entity.Property(e => e.MimeType)
                .HasMaxLength(100)
                .HasColumnName("mime_type");
            entity.Property(e => e.StorageKey)
                .HasMaxLength(255)
                .HasColumnName("storage_key");
            entity.Property(e => e.StorageType)
                .HasMaxLength(20)
                .HasDefaultValueSql("'yandex'::character varying")
                .HasColumnName("storage_type");
            entity.Property(e => e.UploadedAt)
                .HasDefaultValueSql("now()")
                .HasColumnType("timestamp without time zone")
                .HasColumnName("uploaded_at");
            entity.Property(e => e.UploadedBy).HasColumnName("uploaded_by");
            entity.Property(e => e.Width).HasColumnName("width");

            entity.HasOne(d => d.Entity).WithMany(p => p.MediaFiles)
                .HasForeignKey(d => d.EntityId)
                .OnDelete(DeleteBehavior.Cascade)
                .HasConstraintName("MEDIA_FILES_entity_id_fkey");

            entity.HasOne(d => d.UploadedByNavigation).WithMany(p => p.MediaFiles)
                .HasForeignKey(d => d.UploadedBy)
                .OnDelete(DeleteBehavior.SetNull)
                .HasConstraintName("MEDIA_FILES_uploaded_by_fkey");
        });

        modelBuilder.Entity<Notification>(entity =>
        {
            entity.HasKey(e => e.NotificationId).HasName("NOTIFICATIONS_pkey");

            entity.ToTable("NOTIFICATIONS", tb => tb.HasComment("Уведомления для пользователей через SendSay"));

            entity.HasIndex(e => e.Status, "idx_notifications_status_type");

            entity.HasIndex(e => new { e.UserId, e.SentAt }, "idx_notifications_user_sent");

            entity.Property(e => e.NotificationId).HasColumnName("notification_id");
            entity.Property(e => e.Content).HasColumnName("content");
            entity.Property(e => e.ReadAt)
                .HasColumnType("timestamp without time zone")
                .HasColumnName("read_at");
            entity.Property(e => e.SentAt)
                .HasColumnType("timestamp without time zone")
                .HasColumnName("sent_at");
            entity.Property(e => e.SentVia)
                .HasMaxLength(20)
                .HasDefaultValueSql("'sendsay'::character varying")
                .HasColumnName("sent_via");
            entity.Property(e => e.Status)
                .HasMaxLength(20)
                .HasDefaultValueSql("'pending'::character varying")
                .HasColumnName("status");
            entity.Property(e => e.Subject)
                .HasMaxLength(200)
                .HasColumnName("subject");
            entity.Property(e => e.UserId).HasColumnName("user_id");

            entity.HasOne(d => d.User).WithMany(p => p.Notifications)
                .HasForeignKey(d => d.UserId)
                .OnDelete(DeleteBehavior.Cascade)
                .HasConstraintName("NOTIFICATIONS_user_id_fkey");
        });

        modelBuilder.Entity<PasswordResetCode>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PASSWORD_RESET_CODES_pkey");

            entity.ToTable("PASSWORD_RESET_CODES");

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.Code)
                .HasMaxLength(6)
                .HasColumnName("code");
            entity.Property(e => e.ExpiresAt)
                .HasColumnType("timestamp without time zone")
                .HasColumnName("expires_at");
            entity.Property(e => e.Used)
                .HasDefaultValue(false)
                .HasColumnName("used");
            entity.Property(e => e.UserId).HasColumnName("user_id");

            entity.HasOne(d => d.User).WithMany(p => p.PasswordResetCodes)
                .HasForeignKey(d => d.UserId)
                .OnDelete(DeleteBehavior.Cascade)
                .HasConstraintName("PASSWORD_RESET_CODES_user_id_fkey");
        });

        modelBuilder.Entity<Position>(entity =>
        {
            entity.HasKey(e => e.PositionId).HasName("POSITIONS_pkey");

            entity.ToTable("POSITIONS", tb => tb.HasComment("Должности сотрудников с фиксированной зарплатой"));

            entity.HasIndex(e => e.Title, "POSITIONS_title_key").IsUnique();

            entity.Property(e => e.PositionId).HasColumnName("position_id");
            entity.Property(e => e.Permissions)
                .HasColumnType("json")
                .HasColumnName("permissions");
            entity.Property(e => e.Salary)
                .HasPrecision(10, 2)
                .HasColumnName("salary");
            entity.Property(e => e.Title)
                .HasMaxLength(30)
                .HasColumnName("title");
        });

        modelBuilder.Entity<Publisher>(entity =>
        {
            entity.HasKey(e => e.PublisherId).HasName("PUBLISHERS_pkey");

            entity.ToTable("PUBLISHERS", tb => tb.HasComment("Издательства книг"));

            entity.HasIndex(e => e.Name, "idx_publishers_name");

            entity.Property(e => e.PublisherId).HasColumnName("publisher_id");
            entity.Property(e => e.Name)
                .HasMaxLength(100)
                .HasColumnName("name");
        });

        modelBuilder.Entity<SystemLog>(entity =>
        {
            entity.HasKey(e => e.LogId).HasName("SYSTEM_LOGS_pkey");

            entity.ToTable("SYSTEM_LOGS", tb => tb.HasComment("Логирование действий в системе"));

            entity.HasIndex(e => e.Timestamp, "idx_logs_timestamp");

            entity.HasIndex(e => new { e.UserId, e.Action }, "idx_logs_user_action");

            entity.Property(e => e.LogId).HasColumnName("log_id");
            entity.Property(e => e.Action).HasColumnName("action");
            entity.Property(e => e.IpAddress)
                .HasMaxLength(50)
                .HasColumnName("ip_address");
            entity.Property(e => e.LevelId).HasColumnName("level_id");
            entity.Property(e => e.Timestamp)
                .HasDefaultValueSql("now()")
                .HasColumnType("timestamp without time zone")
                .HasColumnName("timestamp");
            entity.Property(e => e.UserId).HasColumnName("user_id");

            entity.HasOne(d => d.Level).WithMany(p => p.SystemLogs)
                .HasForeignKey(d => d.LevelId)
                .OnDelete(DeleteBehavior.Restrict)
                .HasConstraintName("SYSTEM_LOGS_level_id_fkey");

            entity.HasOne(d => d.User).WithMany(p => p.SystemLogs)
                .HasForeignKey(d => d.UserId)
                .OnDelete(DeleteBehavior.SetNull)
                .HasConstraintName("SYSTEM_LOGS_user_id_fkey");
        });

        modelBuilder.Entity<TypesOfWork>(entity =>
        {
            entity.HasKey(e => e.WorkId).HasName("TYPES_OF_WORK_pkey");

            entity.ToTable("TYPES_OF_WORK", tb => tb.HasComment("Типы произведений"));

            entity.HasIndex(e => e.TypeName, "TYPES_OF_WORK_type_name_key").IsUnique();

            entity.HasIndex(e => e.WorkId, "idx_type_of_work");

            entity.Property(e => e.WorkId).HasColumnName("work_id");
            entity.Property(e => e.TypeName)
                .HasMaxLength(50)
                .HasColumnName("type_name");
        });

        modelBuilder.Entity<User>(entity =>
        {
            entity.HasKey(e => e.UserId).HasName("USERS_pkey");

            entity.ToTable("USERS", tb => tb.HasComment("Пользователи системы с персональными данными и правами доступа"));

            entity.HasIndex(e => e.Email, "USERS_email_key").IsUnique();

            entity.HasIndex(e => e.Username, "USERS_username_key").IsUnique();

            entity.HasIndex(e => e.Email, "uq_user_email").IsUnique();

            entity.Property(e => e.UserId).HasColumnName("user_id");
            entity.Property(e => e.Email)
                .HasMaxLength(50)
                .HasColumnName("email");
            entity.Property(e => e.Employee).HasColumnName("employee");
            entity.Property(e => e.FirstName)
                .HasMaxLength(30)
                .HasColumnName("first_name");
            entity.Property(e => e.LastLogin)
                .HasColumnType("timestamp without time zone")
                .HasColumnName("last_login");
            entity.Property(e => e.LastName)
                .HasMaxLength(30)
                .HasColumnName("last_name");
            entity.Property(e => e.LastPasswordChange)
                .HasColumnType("timestamp without time zone")
                .HasColumnName("last_password_change");
            entity.Property(e => e.MiddleName)
                .HasMaxLength(30)
                .HasColumnName("middle_name");
            entity.Property(e => e.PasswordHash)
                .HasMaxLength(255)
                .HasColumnName("password_hash");
            entity.Property(e => e.Phone)
                .HasMaxLength(30)
                .HasColumnName("phone");
            entity.Property(e => e.RegistrationDate)
                .HasDefaultValueSql("now()")
                .HasColumnType("timestamp without time zone")
                .HasColumnName("registration_date");
            entity.Property(e => e.Username)
                .HasMaxLength(50)
                .HasColumnName("username");
            entity.Property(e => e.YearOfBirth).HasColumnName("year_of_birth");
        });

        modelBuilder.Entity<UserBook>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("USER_BOOKS_pkey");

            entity.ToTable("USER_BOOKS", tb => tb.HasComment("Связь пользователей с книгами (личная библиотека электронных книг)"));

            entity.HasIndex(e => new { e.UserId, e.Status }, "idx_user_books_status");

            entity.HasIndex(e => new { e.UserId, e.BookId }, "uq_user_book").IsUnique();

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.AddedDate)
                .HasDefaultValueSql("now()")
                .HasColumnType("timestamp without time zone")
                .HasColumnName("added_date");
            entity.Property(e => e.BookId).HasColumnName("book_id");
            entity.Property(e => e.FinishedReading).HasColumnName("finished_reading");
            entity.Property(e => e.LastReadPage).HasColumnName("last_read_page");
            entity.Property(e => e.StartedReading).HasColumnName("started_reading");
            entity.Property(e => e.Status)
                .HasMaxLength(20)
                .HasDefaultValueSql("'reading'::character varying")
                .HasColumnName("status");
            entity.Property(e => e.UserId).HasColumnName("user_id");

            entity.HasOne(d => d.Book).WithMany(p => p.UserBooks)
                .HasForeignKey(d => d.BookId)
                .OnDelete(DeleteBehavior.Cascade)
                .HasConstraintName("USER_BOOKS_book_id_fkey");

            entity.HasOne(d => d.User).WithMany(p => p.UserBooks)
                .HasForeignKey(d => d.UserId)
                .OnDelete(DeleteBehavior.Cascade)
                .HasConstraintName("USER_BOOKS_user_id_fkey");
        });

        modelBuilder.Entity<UserSession>(entity =>
        {
            entity.HasKey(e => e.SessionId).HasName("USER_SESSIONS_pkey");

            entity.ToTable("USER_SESSIONS", tb => tb.HasComment("Сессии пользователей для JWT аутентификации"));

            entity.HasIndex(e => e.ExpiresAt, "idx_sessions_expiry");

            entity.HasIndex(e => new { e.UserId, e.CreatedAt }, "idx_sessions_user_date");

            entity.Property(e => e.SessionId).HasColumnName("session_id");
            entity.Property(e => e.AccessToken).HasColumnName("access_token");
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("now()")
                .HasColumnType("timestamp without time zone")
                .HasColumnName("created_at");
            entity.Property(e => e.DeviceInfo)
                .HasColumnType("json")
                .HasColumnName("device_info");
            entity.Property(e => e.ExpiresAt)
                .HasColumnType("timestamp without time zone")
                .HasColumnName("expires_at");
            entity.Property(e => e.IpAddress)
                .HasMaxLength(50)
                .HasColumnName("ip_address");
            entity.Property(e => e.RefreshToken).HasColumnName("refresh_token");
            entity.Property(e => e.UserId).HasColumnName("user_id");

            entity.HasOne(d => d.User).WithMany(p => p.UserSessions)
                .HasForeignKey(d => d.UserId)
                .OnDelete(DeleteBehavior.Cascade)
                .HasConstraintName("USER_SESSIONS_user_id_fkey");
        });
        modelBuilder.HasSequence<int>("seq_schema_version", "graphql").IsCyclic();

        OnModelCreatingPartial(modelBuilder);
    }

    partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
}
