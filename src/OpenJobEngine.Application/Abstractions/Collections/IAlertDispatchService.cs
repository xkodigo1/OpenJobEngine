using OpenJobEngine.Application.Common;

namespace OpenJobEngine.Application.Abstractions.Collections;

public interface IAlertDispatchService
{
    Task<AlertDispatchRunDto> DispatchActiveAlertsAsync(CancellationToken cancellationToken);
}
