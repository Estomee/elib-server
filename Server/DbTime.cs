// Utility providing a DateTimeKind.Unspecified timestamp for writing to PostgreSQL timestamp-without-timezone columns.
namespace Server;

internal static class DbTime
{
    public static DateTime Now => DateTime.SpecifyKind(DateTime.UtcNow, DateTimeKind.Unspecified);
}
