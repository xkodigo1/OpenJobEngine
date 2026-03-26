using OpenJobEngine.Application.Resume;

namespace OpenJobEngine.Application.Abstractions.Collections;

public interface IResumeImportService
{
    Task<ResumeImportPreviewDto?> ImportAsync(ResumeImportRequest request, CancellationToken cancellationToken);
}
