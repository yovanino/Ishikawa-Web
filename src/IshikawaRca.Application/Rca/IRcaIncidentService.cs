using IshikawaRca.Contracts.Common;
using IshikawaRca.Contracts.Rca;

namespace IshikawaRca.Application.Rca;

public interface IRcaIncidentService
{
    Task<ApiResult<RcaIncidentDto>> CreateAsync(CreateRcaIncidentRequest request, CancellationToken cancellationToken = default);

    Task<ApiResult<IReadOnlyList<RcaIncidentDto>>> ListAsync(string? sourceSystem = null, string? externalTaskId = null, string? status = null, CancellationToken cancellationToken = default);

    Task<ApiResult<RcaIncidentDto>> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
}
