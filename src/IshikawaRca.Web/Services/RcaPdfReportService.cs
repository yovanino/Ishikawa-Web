using System.Globalization;
using System.Text;
using IshikawaRca.Contracts.Rca;
using IshikawaRca.Web.Models.Rca;

namespace IshikawaRca.Web.Services;

public class RcaPdfReportService : IRcaPdfReportService
{
    public byte[] Build(RcaIncidentDetailsViewModel model, IReadOnlyDictionary<Guid, string> evidenceDownloadUrls)
    {
        var document = new SimplePdfDocument();
        var incident = model.Incident;
        var rootCause = model.Canvas.Causes.FirstOrDefault(x => x.IsRootCause);
        var openActions = model.CorrectiveActions.Count(x =>
            !string.Equals(x.Status, "Completed", StringComparison.OrdinalIgnoreCase) &&
            !string.Equals(x.Status, "Cancelled", StringComparison.OrdinalIgnoreCase));
        var validatedEvidence = model.Evidence.Count(x =>
            string.Equals(x.ValidationStatus, "Validated", StringComparison.OrdinalIgnoreCase));

        document.AddTitle("RCA Ishikawa Report");
        document.AddSubtitle(incident.Title);
        document.AddKeyValue("Incident ID", incident.Id.ToString());
        document.AddKeyValue("Generated at", DateTimeOffset.UtcNow.ToString("yyyy-MM-dd HH:mm 'UTC'", CultureInfo.InvariantCulture));
        document.AddKeyValue("Status", incident.Status);
        document.AddKeyValue("Severity", incident.Severity);
        document.AddKeyValue("Claim actor", $"{incident.ClaimActorType} / {incident.ClaimOwnerName ?? "-"}");
        document.AddKeyValue("Source", incident.SourceSystem);
        document.AddKeyValue("Machine / Line / WO", $"{incident.MachineCode ?? "-"} / {incident.LineCode ?? "-"} / {incident.WorkOrderCode ?? "-"}");
        document.AddKeyValue("Occurred", FormatDate(incident.OccurredAt));
        document.AddParagraph(incident.ProblemDescription ?? "No problem description recorded.");

        document.AddSection("Executive Summary");
        document.AddKeyValue("Wizard step", incident.WizardStep);
        document.AddKeyValue("Wizard completion", $"{model.WizardProgress.CompletionPercent}%");
        document.AddKeyValue("Root cause", rootCause?.Title ?? "Pending");
        document.AddKeyValue("Evidence", $"{model.Evidence.Count} total / {validatedEvidence} validated");
        document.AddKeyValue("Actions", $"{model.CorrectiveActions.Count} total / {openActions} open");
        document.AddKeyValue("Escalated to 8D", incident.EscalatedTo8D ? "Yes" : "No");

        document.AddSection("Closure");
        if (incident.ClosedAt.HasValue)
        {
            document.AddKeyValue("Closed at", FormatDate(incident.ClosedAt.Value));
            document.AddKeyValue("Closed by", incident.ClosedByUserId ?? "-");
            document.AddParagraph(incident.ClosureSummary ?? "No closure summary recorded.");
        }
        else
        {
            document.AddParagraph("RCA is not formally closed yet.");
        }

        document.AddSection("Fact Line");
        if (model.Facts.Count == 0)
        {
            document.AddParagraph("No investigation facts recorded.");
        }
        else
        {
            foreach (var fact in model.Facts.OrderBy(x => x.OccurredAt).ThenBy(x => x.CreatedAt))
            {
                var cause = model.Canvas.Causes.FirstOrDefault(x => x.Id == fact.CauseId);
                var evidence = model.Evidence.FirstOrDefault(x => x.Id == fact.EvidenceId);
                var action = model.CorrectiveActions.FirstOrDefault(x => x.Id == fact.CorrectiveActionId);
                var intake = model.ExternalIntakes.FirstOrDefault(x => x.Id == fact.ExternalIntakeId);
                document.AddBullet($"{FormatDate(fact.OccurredAt)} - {fact.Title}");
                document.AddKeyValue("Type / Source", $"{fact.FactType} / {fact.Source}");
                document.AddKeyValue("Cause", cause?.Title ?? "-");
                document.AddKeyValue("Evidence", evidence?.Title ?? "-");
                document.AddKeyValue("Action", action?.Title ?? "-");
                document.AddKeyValue("External intake", intake is null ? "-" : $"{intake.ActorType} / {intake.ActorName ?? intake.ContactEmail ?? "-"}");
                document.AddKeyValue("Captured by", fact.CapturedByUserId ?? "-");

                if (!string.IsNullOrWhiteSpace(fact.SourceDetail))
                {
                    document.AddKeyValue("Source detail", fact.SourceDetail);
                }

                if (!string.IsNullOrWhiteSpace(fact.Description))
                {
                    document.AddParagraph(fact.Description);
                }
            }
        }

        document.AddSection("Root Cause And Causes");
        if (model.Canvas.Causes.Count == 0)
        {
            document.AddParagraph("No causes recorded.");
        }
        else
        {
            foreach (var cause in model.Canvas.Causes
                         .OrderByDescending(x => x.IsRootCause)
                         .ThenByDescending(x => x.ImpactScore + x.ProbabilityScore + x.FrequencyScore)
                         .ThenBy(x => x.Title))
            {
                document.AddBullet($"{(cause.IsRootCause ? "[ROOT] " : string.Empty)}{cause.Title}");
                document.AddKeyValue("Scores", $"P{cause.ProbabilityScore} I{cause.ImpactScore} F{cause.FrequencyScore}");
                if (!string.IsNullOrWhiteSpace(cause.Description))
                {
                    document.AddParagraph(cause.Description);
                }

                if (!string.IsNullOrWhiteSpace(cause.EvidenceSummary))
                {
                    document.AddParagraph($"Evidence summary: {cause.EvidenceSummary}");
                }
            }
        }

        document.AddSection("Corrective Actions");
        if (model.CorrectiveActions.Count == 0)
        {
            document.AddParagraph("No corrective actions recorded.");
        }
        else
        {
            foreach (var action in model.CorrectiveActions.OrderBy(x => x.Status).ThenBy(x => x.DueDate))
            {
                var cause = model.Canvas.Causes.FirstOrDefault(x => x.Id == action.CauseId);
                document.AddBullet($"{action.Title} [{action.Status}]");
                document.AddKeyValue("Cause", cause?.Title ?? "-");
                document.AddKeyValue("Owner / Due", $"{action.AssignedToUserId ?? "-"} / {(action.DueDate.HasValue ? FormatDate(action.DueDate.Value) : "-")}");
                if (!string.IsNullOrWhiteSpace(action.Description))
                {
                    document.AddParagraph(action.Description);
                }

                if (!string.IsNullOrWhiteSpace(action.ValidationNotes))
                {
                    document.AddParagraph($"Validation: {action.ValidationNotes}");
                }
            }
        }

        document.AddSection("Evidence Manifest");
        if (model.Evidence.Count == 0)
        {
            document.AddParagraph("No evidence recorded.");
        }
        else
        {
            foreach (var evidence in model.Evidence.OrderByDescending(x => x.CapturedAt))
            {
                var cause = model.Canvas.Causes.FirstOrDefault(x => x.Id == evidence.CauseId);
                document.AddBullet($"{evidence.Title} [{evidence.ValidationStatus}]");
                document.AddKeyValue("Cause", cause?.Title ?? "-");
                document.AddKeyValue("Type / Source", $"{evidence.EvidenceType} / {evidence.Source}");
                document.AddKeyValue("Source detail", evidence.SourceDetail ?? "-");
                document.AddKeyValue("Captured", $"{FormatDate(evidence.CapturedAt)} by {evidence.CapturedByUserId ?? "-"}");
                document.AddKeyValue("Tags", evidence.Tags ?? "-");
                document.AddKeyValue("Validation", $"{evidence.ValidationStatus} by {evidence.ValidatedByUserId ?? "-"} at {(evidence.ValidatedAt.HasValue ? FormatDate(evidence.ValidatedAt.Value) : "-")}");

                if (!string.IsNullOrWhiteSpace(evidence.Summary))
                {
                    document.AddParagraph(evidence.Summary);
                }

                if (!string.IsNullOrWhiteSpace(evidence.ValidationNotes))
                {
                    document.AddParagraph($"Validation notes: {evidence.ValidationNotes}");
                }

                if (!string.IsNullOrWhiteSpace(evidence.AttachmentStorageKey))
                {
                    document.AddKeyValue("Attachment", $"{evidence.AttachmentFileName ?? "file"} / {FormatFileSize(evidence.AttachmentSizeBytes)} / {evidence.AttachmentContentType ?? "binary"}");
                    document.AddKeyValue("SHA-256", evidence.AttachmentSha256 ?? "-");
                    if (evidenceDownloadUrls.TryGetValue(evidence.Id, out var downloadUrl))
                    {
                        document.AddParagraph($"Download: {downloadUrl}");
                    }
                }

                if (!string.IsNullOrWhiteSpace(evidence.ReferenceUri))
                {
                    document.AddParagraph($"Reference: {evidence.ReferenceUri}");
                }
            }
        }

        document.AddSection("External Intake");
        if (model.ExternalIntakes.Count == 0)
        {
            document.AddParagraph("No external intake links recorded.");
        }
        else
        {
            foreach (var intake in model.ExternalIntakes.OrderByDescending(x => x.CreatedAt))
            {
                document.AddBullet($"{intake.ActorType} / {intake.ActorName ?? "-"} [{intake.Status}]");
                document.AddKeyValue("Contact", $"{intake.ContactName ?? "-"} / {intake.ContactEmail ?? "-"}");
                document.AddKeyValue("Expires", FormatDate(intake.ExpiresAt));
                if (!string.IsNullOrWhiteSpace(intake.EvidenceSummary))
                {
                    document.AddParagraph($"Evidence: {intake.EvidenceSummary}");
                }
            }
        }

        document.AddFooter();

        return document.Build();
    }

    private static string FormatDate(DateTimeOffset value)
    {
        return value.LocalDateTime.ToString("yyyy-MM-dd HH:mm", CultureInfo.InvariantCulture);
    }

    private static string FormatFileSize(long? bytes)
    {
        if (!bytes.HasValue)
        {
            return "-";
        }

        var value = bytes.Value;
        if (value >= 1024L * 1024L)
        {
            return $"{value / 1024d / 1024d:0.##} MB";
        }

        if (value >= 1024L)
        {
            return $"{value / 1024d:0.##} KB";
        }

        return $"{value} B";
    }

    private sealed record PdfTextLine(string Text, int FontSize, bool Bold);

    private sealed class SimplePdfDocument
    {
        private const double PageWidth = 612;
        private const double PageHeight = 792;
        private const double Margin = 54;
        private const double BottomMargin = 54;
        private const double DefaultLineHeight = 14;

        private readonly List<List<PdfTextLine>> _pages = [[]];
        private double _remaining = PageHeight - Margin - BottomMargin;

        public void AddTitle(string text)
        {
            AddLine(text, 18, true, 22);
            AddLine(new string('-', 72), 9, false, 12);
        }

        public void AddSubtitle(string text)
        {
            AddWrapped(text, 14, true);
            AddBlank(8);
        }

        public void AddSection(string text)
        {
            AddBlank(8);
            AddLine(text, 13, true, 18);
        }

        public void AddKeyValue(string key, string value)
        {
            AddWrapped($"{key}: {value}", 10, false);
        }

        public void AddBullet(string text)
        {
            AddWrapped($"- {text}", 11, true);
        }

        public void AddParagraph(string text)
        {
            AddWrapped(text, 10, false);
            AddBlank(4);
        }

        public void AddFooter()
        {
            for (var i = 0; i < _pages.Count; i++)
            {
                _pages[i].Add(new PdfTextLine($"Page {i + 1} of {_pages.Count}", 8, false));
            }
        }

        public byte[] Build()
        {
            var objects = new List<string>
            {
                "<< /Type /Catalog /Pages 2 0 R >>",
                string.Empty,
                "<< /Type /Font /Subtype /Type1 /BaseFont /Helvetica >>",
                "<< /Type /Font /Subtype /Type1 /BaseFont /Helvetica-Bold >>"
            };

            var pageObjectNumbers = new List<int>();
            var contentObjectNumbers = new List<int>();

            foreach (var page in _pages)
            {
                var content = BuildPageContent(page);
                var contentObjectNumber = objects.Count + 1;
                objects.Add($"<< /Length {Encoding.ASCII.GetByteCount(content)} >>\nstream\n{content}endstream");
                contentObjectNumbers.Add(contentObjectNumber);

                var pageObjectNumber = objects.Count + 1;
                objects.Add($"<< /Type /Page /Parent 2 0 R /MediaBox [0 0 {PageWidth} {PageHeight}] /Resources << /Font << /F1 3 0 R /F2 4 0 R >> >> /Contents {contentObjectNumber} 0 R >>");
                pageObjectNumbers.Add(pageObjectNumber);
            }

            objects[1] = $"<< /Type /Pages /Kids [{string.Join(' ', pageObjectNumbers.Select(x => $"{x} 0 R"))}] /Count {pageObjectNumbers.Count} >>";

            return WritePdf(objects);
        }

        private void AddWrapped(string text, int fontSize, bool bold)
        {
            foreach (var line in Wrap(Sanitize(text), fontSize))
            {
                AddLine(line, fontSize, bold, DefaultLineHeight);
            }
        }

        private void AddLine(string text, int fontSize, bool bold, double height)
        {
            EnsureSpace(height);
            _pages[^1].Add(new PdfTextLine(Sanitize(text), fontSize, bold));
            _remaining -= height;
        }

        private void AddBlank(double height)
        {
            EnsureSpace(height);
            _pages[^1].Add(new PdfTextLine(string.Empty, 10, false));
            _remaining -= height;
        }

        private void EnsureSpace(double height)
        {
            if (_remaining >= height)
            {
                return;
            }

            _pages.Add([]);
            _remaining = PageHeight - Margin - BottomMargin;
        }

        private static IEnumerable<string> Wrap(string text, int fontSize)
        {
            var maxChars = Math.Max(32, (int)((PageWidth - Margin * 2) / (fontSize * 0.52)));
            var words = text.Split(' ', StringSplitOptions.RemoveEmptyEntries);
            if (words.Length == 0)
            {
                yield return string.Empty;
                yield break;
            }

            var line = new StringBuilder();
            foreach (var word in words)
            {
                if (line.Length > 0 && line.Length + word.Length + 1 > maxChars)
                {
                    yield return line.ToString();
                    line.Clear();
                }

                if (line.Length > 0)
                {
                    line.Append(' ');
                }

                line.Append(word.Length > maxChars ? word[..maxChars] : word);
            }

            if (line.Length > 0)
            {
                yield return line.ToString();
            }
        }

        private static string BuildPageContent(IReadOnlyList<PdfTextLine> lines)
        {
            var content = new StringBuilder();
            var y = PageHeight - Margin;

            foreach (var line in lines)
            {
                var font = line.Bold ? "F2" : "F1";
                content.Append(CultureInfo.InvariantCulture, $"BT /{font} {line.FontSize} Tf {Margin:0.##} {y:0.##} Td ({EscapePdf(line.Text)}) Tj ET\n");
                y -= line.FontSize + 4;
            }

            return content.ToString();
        }

        private static byte[] WritePdf(IReadOnlyList<string> objects)
        {
            using var stream = new MemoryStream();
            var offsets = new List<long> { 0 };

            WriteAscii(stream, "%PDF-1.4\n");

            for (var i = 0; i < objects.Count; i++)
            {
                offsets.Add(stream.Position);
                WriteAscii(stream, $"{i + 1} 0 obj\n{objects[i]}\nendobj\n");
            }

            var xrefOffset = stream.Position;
            WriteAscii(stream, $"xref\n0 {objects.Count + 1}\n");
            WriteAscii(stream, "0000000000 65535 f \n");

            foreach (var offset in offsets.Skip(1))
            {
                WriteAscii(stream, $"{offset:0000000000} 00000 n \n");
            }

            WriteAscii(stream, $"trailer\n<< /Size {objects.Count + 1} /Root 1 0 R >>\nstartxref\n{xrefOffset}\n%%EOF");

            return stream.ToArray();
        }

        private static void WriteAscii(Stream stream, string value)
        {
            var bytes = Encoding.ASCII.GetBytes(value);
            stream.Write(bytes, 0, bytes.Length);
        }

        private static string EscapePdf(string value)
        {
            return value
                .Replace("\\", "\\\\", StringComparison.Ordinal)
                .Replace("(", "\\(", StringComparison.Ordinal)
                .Replace(")", "\\)", StringComparison.Ordinal);
        }

        private static string Sanitize(string? value)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                return "-";
            }

            var normalized = value.Normalize(NormalizationForm.FormD);
            var builder = new StringBuilder(normalized.Length);
            foreach (var character in normalized)
            {
                var category = CharUnicodeInfo.GetUnicodeCategory(character);
                if (category == UnicodeCategory.NonSpacingMark)
                {
                    continue;
                }

                builder.Append(character is >= ' ' and <= '~' ? character : ' ');
            }

            return builder.ToString().Normalize(NormalizationForm.FormC).Trim();
        }
    }
}
