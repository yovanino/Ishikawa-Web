using IshikawaRca.Application.Rca;
using IshikawaRca.Contracts.Common;
using IshikawaRca.Contracts.Rca;
using IshikawaRca.Web.Security;
using IshikawaRca.Web.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace IshikawaRca.Web.Controllers.Api;

[ApiController]
[Authorize]
[Route("api/v1/rca/incidents/{id:guid}/documents/closure")]
public class RcaDocumentsController : ControllerBase
{
    private readonly IRcaClosureDocumentService _closureDocumentService;
    private readonly IClosureDocumentStorage _closureDocumentStorage;
    private readonly ICurrentRcaUserContext _currentUserContext;

    public RcaDocumentsController(
        IRcaClosureDocumentService closureDocumentService,
        IClosureDocumentStorage closureDocumentStorage,
        ICurrentRcaUserContext currentUserContext)
    {
        _closureDocumentService = closureDocumentService;
        _closureDocumentStorage = closureDocumentStorage;
        _currentUserContext = currentUserContext;
    }

    [HttpGet]
    [ProducesResponseType(typeof(ApiResult<IReadOnlyList<RcaClosureDocumentDto>>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResult<IReadOnlyList<RcaClosureDocumentDto>>), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ApiResult<IReadOnlyList<RcaClosureDocumentDto>>>> List(Guid id, CancellationToken cancellationToken)
    {
        var result = await _closureDocumentService.ListAsync(id, cancellationToken);
        return ToActionResult(result);
    }

    [HttpGet("{documentId:guid}/download")]
    [ProducesResponseType(typeof(FileResult), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Download(Guid id, Guid documentId, CancellationToken cancellationToken)
    {
        var result = await _closureDocumentService.ListAsync(id, cancellationToken);
        if (!result.Success)
        {
            return NotFound();
        }

        var document = result.Data?.FirstOrDefault(x => x.Id == documentId);
        if (document is null)
        {
            return NotFound();
        }

        try
        {
            var file = _closureDocumentStorage.Resolve(
                document.StorageKey,
                document.FileName,
                document.ContentType);

            return PhysicalFile(file.PhysicalPath, file.ContentType, file.FileName);
        }
        catch (Exception ex) when (ex is InvalidOperationException or FileNotFoundException)
        {
            return NotFound();
        }
    }

    [HttpPost("{documentId:guid}/approve")]
    [Authorize(Roles = RcaRoleNames.QualityGovernance)]
    [ProducesResponseType(typeof(ApiResult<RcaClosureDocumentDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResult<RcaClosureDocumentDto>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResult<RcaClosureDocumentDto>), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ApiResult<RcaClosureDocumentDto>>> Approve(Guid id, Guid documentId, ReviewRcaClosureDocumentRequest request, CancellationToken cancellationToken)
    {
        request.ReviewedByUserId = _currentUserContext.UserId;
        var result = await _closureDocumentService.ApproveAsync(id, documentId, request, cancellationToken);
        return ToActionResult(result);
    }

    [HttpPost("{documentId:guid}/reject")]
    [Authorize(Roles = RcaRoleNames.QualityGovernance)]
    [ProducesResponseType(typeof(ApiResult<RcaClosureDocumentDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResult<RcaClosureDocumentDto>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResult<RcaClosureDocumentDto>), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ApiResult<RcaClosureDocumentDto>>> Reject(Guid id, Guid documentId, ReviewRcaClosureDocumentRequest request, CancellationToken cancellationToken)
    {
        request.ReviewedByUserId = _currentUserContext.UserId;
        var result = await _closureDocumentService.RejectAsync(id, documentId, request, cancellationToken);
        return ToActionResult(result);
    }

    private ActionResult<ApiResult<T>> ToActionResult<T>(ApiResult<T> result)
    {
        if (result.Success)
        {
            return Ok(result);
        }

        if (result.Errors.Any(x => x.Code.Contains("NOT_FOUND", StringComparison.OrdinalIgnoreCase)))
        {
            return NotFound(result);
        }

        return BadRequest(result);
    }
}
