using System.Data.Common;
using System.Threading;
using System.Threading.Tasks;
using EchoSpace.Core.Interfaces.Services;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace EchoSpace.Infrastructure.Logging
{
    public class EchoSpaceDbCommandInterceptor : DbCommandInterceptor
    {
        private readonly IAuditLogService _auditLogService;

        public EchoSpaceDbCommandInterceptor(IAuditLogService auditLogService)
        {
            _auditLogService = auditLogService;
        }

        public override async ValueTask<InterceptionResult<DbDataReader>> ReaderExecutingAsync(
            DbCommand command,
            CommandEventData eventData,
            InterceptionResult<DbDataReader> result,
            CancellationToken cancellationToken = default)
        {
            await LogCommandAsync(command.CommandText, "ReaderExecutingAsync");
            return await base.ReaderExecutingAsync(command, eventData, result, cancellationToken);
        }

        public override async ValueTask<InterceptionResult<int>> NonQueryExecutingAsync(
            DbCommand command,
            CommandEventData eventData,
            InterceptionResult<int> result,
            CancellationToken cancellationToken = default)
        {
            await LogCommandAsync(command.CommandText, "NonQueryExecutingAsync");
            return await base.NonQueryExecutingAsync(command, eventData, result, cancellationToken);
        }

        public override async ValueTask<InterceptionResult<object>> ScalarExecutingAsync(
            DbCommand command,
            CommandEventData eventData,
            InterceptionResult<object> result,
            CancellationToken cancellationToken = default)
        {
            await LogCommandAsync(command.CommandText, "ScalarExecutingAsync");
            return await base.ScalarExecutingAsync(command, eventData, result, cancellationToken);
        }

        private async Task LogCommandAsync(string commandText, string actionType)
        {
            await _auditLogService.LogAsync(
                action: actionType,
                entityType: "DatabaseCommand",
                entityId: commandText,
                result: "success"
            );
        }
    }
}
