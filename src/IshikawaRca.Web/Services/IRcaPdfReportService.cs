using IshikawaRca.Web.Models.Rca;

namespace IshikawaRca.Web.Services;

public interface IRcaPdfReportService
{
    byte[] Build(RcaIncidentDetailsViewModel model, IReadOnlyDictionary<Guid, string> evidenceDownloadUrls);
}
