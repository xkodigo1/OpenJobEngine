namespace OpenJobEngine.Application.Collections;

public sealed record CollectionRunResultDto(
    DateTimeOffset StartedAtUtc,
    DateTimeOffset CompletedAtUtc,
    IReadOnlyCollection<CollectionRunSummaryDto> Sources);
