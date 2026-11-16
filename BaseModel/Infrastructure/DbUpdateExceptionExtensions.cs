using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace BaseModel.Infrastructure;

public static class DbUpdateExceptionExtensions
{
    // SQLITE_CONSTRAINT_UNIQUE, see https://www.sqlite.org/rescode.html#constraint_unique
    private const int SqliteConstraintUnique = 2067;

    /// <summary>
    /// True when the save failed because a unique index rejected the row. Keeps the knowledge
    /// about provider error codes out of the service layer.
    /// </summary>
    public static bool IsUniqueConstraintViolation(this DbUpdateException exception)
        => exception.InnerException is SqliteException { SqliteExtendedErrorCode: SqliteConstraintUnique };
}
