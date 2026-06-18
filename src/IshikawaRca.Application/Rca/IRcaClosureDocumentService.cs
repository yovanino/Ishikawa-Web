using IshikawaRca.Contracts.Common;
using IshikawaRca.Contracts.Rca;

namespace IshikawaRca.Application.Rca;

public interface IRcaClosureDocumentService
{
    Task<ApiResult<RcaClosureDocumentDto>> RegisterGeneratedAsync(
        Guid incidentId,
        RegisterRcaClosureDocumentRequest request,
        CancellationToken cancellationToken = default);

    Task<ApiResult<IReadOnlyList<RcaClosureDocumentDto>>> ListAsync(
        Guid incidentId,
        CancellationToken cancellationToken = default);

    Task<ApiResult<RcaClosureDocumentDto>> ApproveAsync(
        Guid incidentId,
        Guid documentId,
        ReviewRcaClosureDocumentRequest request,
        CancellationToken cancellationToken = default);

    Task<ApiResult<RcaClosureDocumentDto>> RejectAsync(
        Guid incidentId,
        Guid documentId,
        ReviewRcaClosureDocumentRequest request,
        CancellationToken cancellationToken = default);
}
