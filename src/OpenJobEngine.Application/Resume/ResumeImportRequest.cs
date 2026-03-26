namespace OpenJobEngine.Application.Resume;

public sealed record ResumeImportRequest(
    Guid ProfileId,
    string FileName,
    string ContentType,
    byte[] Content,
    bool ApplyToProfile);
