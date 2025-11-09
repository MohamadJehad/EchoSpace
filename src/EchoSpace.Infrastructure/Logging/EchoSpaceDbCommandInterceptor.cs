using System.Data.Common;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using EchoSpace.Core.Interfaces.Services;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace EchoSpace.Infrastructure.Logging
{
    /// <summary>
    /// Intercepts all database commands for audit logging
    /// Use with caution - can generate large amounts of log data
    /// </summary>
    public class EchoSpaceDbCommandInterceptor : DbCommandInterceptor
    {
        private readonly IAuditLogService? _auditLogService;
        private readonly bool _isEnabled;

        public EchoSpaceDbCommandInterceptor(IAuditLogService? auditLogService = null, bool isEnabled = true)
        {
            _auditLogService = auditLogService;
            _isEnabled = isEnabled && auditLogService != null;
        }

        public override async ValueTask<InterceptionResult<DbDataReader>> ReaderExecutingAsync(
            DbCommand command,
            CommandEventData eventData,
            InterceptionResult<DbDataReader> result,
            CancellationToken cancellationToken = default)
        {
            if (_isEnabled)
            {
                await LogCommandAsync(SanitizeCommand(command.CommandText), "SELECT");
            }
            return await base.ReaderExecutingAsync(command, eventData, result, cancellationToken);
        }

        public override async ValueTask<InterceptionResult<int>> NonQueryExecutingAsync(
            DbCommand command,
            CommandEventData eventData,
            InterceptionResult<int> result,
            CancellationToken cancellationToken = default)
        {
            if (_isEnabled)
            {
                var commandType = GetCommandType(command.CommandText);
                await LogCommandAsync(SanitizeCommand(command.CommandText), commandType);
            }
            return await base.NonQueryExecutingAsync(command, eventData, result, cancellationToken);
        }

        public override async ValueTask<InterceptionResult<object>> ScalarExecutingAsync(
            DbCommand command,
            CommandEventData eventData,
            InterceptionResult<object> result,
            CancellationToken cancellationToken = default)
        {
            if (_isEnabled)
            {
                await LogCommandAsync(SanitizeCommand(command.CommandText), "SCALAR");
            }
            return await base.ScalarExecutingAsync(command, eventData, result, cancellationToken);
        }

        private async Task LogCommandAsync(string commandText, string actionType)
        {
            if (_auditLogService == null) return;

            // Truncate very long commands to avoid log bloat
            var truncatedCommand = commandText.Length > 500 
                ? commandText.Substring(0, 500) + "..." 
                : commandText;

            await _auditLogService.LogAsync(
                action: $"DB_{actionType}",
                entityType: "DatabaseCommand",
                entityId: truncatedCommand,
                result: "Executed"
            );
        }

        private string GetCommandType(string commandText)
        {
            var upperCommand = commandText.TrimStart().ToUpperInvariant();
            if (upperCommand.StartsWith("INSERT")) return "INSERT";
            if (upperCommand.StartsWith("UPDATE")) return "UPDATE";
            if (upperCommand.StartsWith("DELETE")) return "DELETE";
            if (upperCommand.StartsWith("SELECT")) return "SELECT";
            return "UNKNOWN";
        }

        /// <summary>
        /// Sanitize SQL command to remove sensitive data (passwords, tokens, etc.)
        /// </summary>
        private string SanitizeCommand(string commandText)
        {
            // Remove potential password fields
            var sanitized = Regex.Replace(
                commandText,
                @"(Password|PasswordHash|Token|Secret|Key)\s*=\s*'[^']*'",
                "$1='***REDACTED***'",
                RegexOptions.IgnoreCase);

            // Remove potential password parameters
            sanitized = Regex.Replace(
                sanitized,
                @"@(Password|PasswordHash|Token|Secret|Key)\s*=\s*'[^']*'",
                "@$1='***REDACTED***'",
                RegexOptions.IgnoreCase);

            return sanitized;
        }
    }
}

