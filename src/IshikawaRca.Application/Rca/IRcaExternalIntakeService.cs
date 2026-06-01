using IshikawaRca.Contracts.Common;
using IshikawaRca.Contracts.Rca;

namespace IshikawaRca.Application.Rca;

public interface IRcaExternalIntakeService
{
    Task<ApiResult<CreatedExternalIntakeDto>> CreateAsync(Guid incidentId, CreateExternalIntakeRequest request, CancellationToken cancellationToken = default);

    Task<ApiResult<IReadOnlyList<RcaExternalIntakeDto>>> ListByIncidentAsync(Guid incidentId, CancellationToken cancellationToken = default);

    Task<ApiResult<RcaExternalIntakeDto>> GetByTokenAsync(string token, CancellationToken cancellationToken = default);

    Task<ApiResult<RcaExternalIntakeDto>> SubmitAsync(string token, SubmitExternalIntakeRequest request, CancellationToken cancellationToken = default);

    Task<ApiResult<RcaExternalIntakeDto>> ReviewAsync(Guid intakeId, ReviewExternalIntakeRequest request, CancellationToken cancellationToken = default);

    Task<ApiResult<RcaExternalIntakeDto>> RevokeAsync(Guid intakeId, CancellationToken cancellationToken = default);
}
