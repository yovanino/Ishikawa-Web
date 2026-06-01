using IshikawaRca.Contracts.Common;
using IshikawaRca.Contracts.Rca;

namespace IshikawaRca.Application.Rca;

public interface IRcaIncidentService
{
    Task<ApiResult<RcaIncidentDto>> CreateAsync(CreateRcaIncidentRequest request, CancellationToken cancellationToken = default);

    Task<ApiResult<IReadOnlyList<RcaIncidentDto>>> ListAsync(string? sourceSystem = null, string? externalTaskId = null, string? status = null, CancellationToken cancellationToken = default);

    Task<ApiResult<RcaIncidentDto>> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    Task<ApiResult<IshikawaCanvasDto>> GetCanvasAsync(Guid incidentId, CancellationToken cancellationToken = default);

    Task<ApiResult<IshikawaCauseDto>> AddCauseAsync(Guid incidentId, AddIshikawaCauseRequest request, CancellationToken cancellationToken = default);

    Task<ApiResult<IReadOnlyList<CorrectiveActionDto>>> ListCorrectiveActionsAsync(Guid incidentId, CancellationToken cancellationToken = default);

    Task<ApiResult<CorrectiveActionDto>> AddCorrectiveActionAsync(Guid incidentId, AddCorrectiveActionRequest request, CancellationToken cancellationToken = default);

    Task<ApiResult<IReadOnlyList<RcaEvidenceDto>>> ListEvidenceAsync(Guid incidentId, CancellationToken cancellationToken = default);

    Task<ApiResult<RcaEvidenceDto>> AddEvidenceAsync(Guid incidentId, AddRcaEvidenceRequest request, CancellationToken cancellationToken = default);

    Task<ApiResult<RcaIntegrationSnapshotDto>> GetIntegrationSnapshotAsync(Guid incidentId, CancellationToken cancellationToken = default);

    Task<ApiResult<IReadOnlyList<RcaIntegrationSnapshotDto>>> ListIntegrationSnapshotsAsync(string? sourceSystem = null, string? externalTaskId = null, string? status = null, CancellationToken cancellationToken = default);

    Task<ApiResult<IReadOnlyList<RcaDomainEventDto>>> ListIntegrationEventsAsync(Guid? incidentId = null, DateTimeOffset? since = null, CancellationToken cancellationToken = default);
}
