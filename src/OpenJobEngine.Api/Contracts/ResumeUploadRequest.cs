using Microsoft.AspNetCore.Mvc;

namespace OpenJobEngine.Api.Contracts;

public sealed class ResumeUploadRequest
{
    [FromForm(Name = "file")]
    public IFormFile File { get; set; } = null!;

    [FromForm(Name = "applyToProfile")]
    public bool ApplyToProfile { get; set; }
}
