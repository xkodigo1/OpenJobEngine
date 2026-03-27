using Microsoft.Extensions.Logging;
using OpenJobEngine.Application.Abstractions.Collections;
using OpenJobEngine.Application.Abstractions.Persistence;
using OpenJobEngine.Application.Abstractions.Services;
using OpenJobEngine.Application.Common;
using OpenJobEngine.Application.Matching;
using OpenJobEngine.Domain.Entities;
using OpenJobEngine.Domain.Enums;

namespace OpenJobEngine.Application.Collections;

public sealed class AlertDispatchService(
    IProfileAlertRepository profileAlertRepository,
    IAlertDeliveryRepository alertDeliveryRepository,
    IAlertWebhookPublisher alertWebhookPublisher,
    IMatchingService matchingService,
    IUnitOfWork unitOfWork,
    ILogger<AlertDispatchService> logger) : IAlertDispatchService
{
    public async Task<AlertDispatchRunDto> DispatchActiveAlertsAsync(CancellationToken cancellationToken)
    {
        var startedAtUtc = DateTimeOffset.UtcNow;
        var activeAlerts = await profileAlertRepository.ListActiveAsync(cancellationToken);

        var deliveredCount = 0;
        var recordedCount = 0;
        var failedCount = 0;
        var skippedCount = 0;
        var matchedJobs = 0;

        foreach (var alert in activeAlerts.OrderBy(x => x.CreatedAtUtc))
        {
            IReadOnlyCollection<JobMatchResultDto> matches;
            var threshold = alert.MinimumNewMatchScore
                ?? alert.MinimumMatchScore
                ?? matchingService.GetCurrentRules().Tolerances.NewHighPriorityThreshold;

            try
            {
                matches = await matchingService.GetAlertCandidatesAsync(
                    alert.CandidateProfileId,
                    threshold,
                    alert.OnlyNewJobs,
                    alert.LastCheckedAtUtc,
                    cancellationToken);
            }
            catch (Exception exception)
            {
                logger.LogError(exception, "Alert dispatch failed while evaluating alert {AlertId}", alert.Id);
                failedCount++;
                continue;
            }

            matchedJobs += matches.Count;

            foreach (var match in matches.OrderByDescending(x => x.MatchScore).Take(10))
            {
                var existingDelivery = await alertDeliveryRepository.GetByAlertAndJobAsync(alert.Id, match.Job.Id, cancellationToken);
                if (existingDelivery is { } terminalDelivery && terminalDelivery.IsTerminal())
                {
                    skippedCount++;
                    continue;
                }

                var delivery = existingDelivery ?? AlertDelivery.Create(
                    alert,
                    match.Job.Id,
                    match.MatchScore,
                    match.MatchBand,
                    match.RuleVersion);

                if (existingDelivery is null)
                {
                    await alertDeliveryRepository.AddAsync(delivery, cancellationToken);
                }

                if (alert.ChannelType == AlertChannelType.Passive)
                {
                    delivery.RecordPassive(DateTimeOffset.UtcNow);
                    await alertDeliveryRepository.UpdateAsync(delivery, cancellationToken);
                    recordedCount++;
                    continue;
                }

                var payload = new AlertWebhookPayloadDto(
                    "openjobengine.alert.match",
                    DateTimeOffset.UtcNow,
                    delivery.Id,
                    alert.Id,
                    alert.CandidateProfileId,
                    alert.Name,
                    alert.ChannelType.ToString(),
                    match.MatchScore,
                    match.MatchBand,
                    match.RuleVersion,
                    match.StrongMatches,
                    match.PartialMatches,
                    match.HardFailures,
                    match.Job,
                    alert.Target);

                var dispatchResult = await alertWebhookPublisher.SendAsync(payload, cancellationToken);
                if (dispatchResult.Success)
                {
                    delivery.MarkDelivered(DateTimeOffset.UtcNow, dispatchResult.StatusCode, dispatchResult.ResponseBody);
                    deliveredCount++;
                }
                else
                {
                    delivery.MarkFailed(DateTimeOffset.UtcNow, dispatchResult.StatusCode, dispatchResult.ResponseBody, dispatchResult.ErrorMessage);
                    failedCount++;
                }

                await alertDeliveryRepository.UpdateAsync(delivery, cancellationToken);
            }

            alert.MarkChecked(DateTimeOffset.UtcNow);
            await profileAlertRepository.UpdateAsync(alert, cancellationToken);
            await unitOfWork.SaveChangesAsync(cancellationToken);
        }

        return new AlertDispatchRunDto(
            startedAtUtc,
            DateTimeOffset.UtcNow,
            activeAlerts.Count,
            matchedJobs,
            deliveredCount,
            recordedCount,
            failedCount,
            skippedCount);
    }
}
