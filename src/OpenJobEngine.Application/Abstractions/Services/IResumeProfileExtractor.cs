using OpenJobEngine.Application.Resume;

namespace OpenJobEngine.Application.Abstractions.Services;

public interface IResumeProfileExtractor
{
    ResumeProfileExtractionResultDto Extract(string resumeText);
}
