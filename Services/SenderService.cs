using System;
using System.Collections.Generic;
using RCL.Logging;
using Rumble.Platform.Common.Exceptions;
using Rumble.Platform.Common.Interop;
using Rumble.Platform.Common.Minq;
using Rumble.Platform.Common.Models.Alerting;
using Rumble.Platform.Common.Services;
using Rumble.Platform.Common.Utilities;
using Rumble.Platform.Data;

namespace AlertingService.Services;

public class SenderService : MinqTimerService<Alert>
{
    private const int CYCLE_TIME = 60_000;
    
    private readonly DynamicConfig _config;
    private readonly ApiService _api;
    
    public SenderService(DynamicConfig config, ApiService api) : base("alerts", CYCLE_TIME)
    {
        _config = config;
        _api = api;
    }

    protected override void OnElapsed()
    {
        // TODO: If we shift entirely to a pagerduty integration, we should refactor this to be cleaner
        List<Alert> outbox = mongo
            .WithTransaction(out Transaction ta)
            .Where(query => query
                .NotContainedIn(alert => alert.Status, new [] { Alert.AlertStatus.Sent, Alert.AlertStatus.Resolved, Alert.AlertStatus.Canceled })
                .NotEqualTo(alert => alert.Escalation, Alert.EscalationLevel.Final) // Should we re-notify?
                .LessThanOrEqualTo(alert => alert.SendAfter, Timestamp.UnixTime)
                .GreaterThanOrEqualToRelative(alert => alert.Trigger.Count, alert => alert.Trigger.CountRequired)
            )
            .ToList();

        Log.Local(Owner.Will, $"Found {outbox.Count} alerts to send.");

        foreach (Alert toSend in outbox)
        {
            switch (toSend.Status)
            {
                case Alert.AlertStatus.Pending:
                case Alert.AlertStatus.PendingResend:
                case Alert.AlertStatus.Acknowledged:
                case Alert.AlertStatus.Escalated:
                    // https://rumblegames.slack.com/archives/D0211U9PSBE/p1696528095037499
                    // Eric Sheris: can alert service 107 and 207 fire to low pri instead of high pri?
                    PagerDutyIncident incident = PagerDuty.CreateIncident(toSend, level: PlatformEnvironment.IsProd
                        ? PagerDuty.Urgency.RedAlert
                        : PagerDuty.Urgency.YellowAlert
                    );
                    if (incident == null)
                        Log.Warn(Owner.Will, "Potentially failed to create PD incident; requires investigation.", data: new
                        {
                            Alert = toSend
                        });
                    toSend.Status = Alert.AlertStatus.Sent;
                    break;
                case Alert.AlertStatus.Sent:
                case Alert.AlertStatus.Resolved:
                case Alert.AlertStatus.Canceled:
                default:
                    break;
            }
            
            Log.Local(Owner.Will, $"{toSend}", emphasis: Log.LogType.CRITICAL);
            Update(toSend);
        }

        try
        {
            mongo
                .OnRecordsAffected(result => Log.Local(Owner.Will, $"{result.Affected} alerts closed."))
                .Where(query => query
                    .EqualTo(alert => alert.Status, Alert.AlertStatus.Pending)
                    .LessThanRelative(alert => alert.Trigger.Count, alert => alert.Trigger.CountRequired)
                    .LessThan(alert => alert.Expiration, Timestamp.UnixTime - (CYCLE_TIME * 3)) // allow a grace period to guarantee we don't close alerts too early.
                )
                .Update(query => query.Set(alert => alert.Status, Alert.AlertStatus.TriggerNotMet));
        }
        catch (Exception e)
        {
            Log.Error(Owner.Will, "Unable to close alerts.", exception: e);
        }
    }

    private void Send(Alert alert)
    {
        try
        {
            switch (alert.Type)
            {
                case Alert.AlertType.All:
                    SendToSlack(alert);
                    SendEmail(alert);
                    break;
                case Alert.AlertType.Slack:
                    SendToSlack(alert);
                    break;
                case Alert.AlertType.Email:
                default:
                    SendEmail(alert);
                    break;
            }
            
            alert.LastSent = Timestamp.UnixTime;
            alert.Status = alert.Status switch
            {
                Alert.AlertStatus.Pending => Alert.AlertStatus.Sent,
                Alert.AlertStatus.PendingResend => Alert.AlertStatus.Escalated,
                _ => alert.Status
            };
            alert.SendAfter = Timestamp.UnixTime + Alert.SECONDS_BEFORE_ESCALATION;
            Log.Error(alert.Owner, $"Alert sent: {alert.Message}", data: new RumbleJson
            {
                { "trigger", alert.Trigger },
                { "alertData", alert.Data }
            });
        }
        catch (Exception e)
        {
            Log.Error(Owner.Will, "Unable to send alert.", data: new
            {
                Alert = alert.ToString()
            }, exception: e);
        }
    }

    private void SendToSlack(Alert alert)
    {
        string none = _config.Require<string>("slackDefault");
        string first = _config.Require<string>("slackFirstEscalation");
        string final = _config.Require<string>("slackSecondEscalation");

        string channel = alert.Escalation switch
        {
            Alert.EscalationLevel.None => none,
            Alert.EscalationLevel.First => first,
            Alert.EscalationLevel.Final => final,
            _ => none
        };

        _api
            .Request(SlackMessageClient.POST_MESSAGE)
            .AddAuthorization(PlatformEnvironment.SlackLogBotToken)
            .SetPayload(alert.ToSlackMessage(channel))
            .OnFailure(response => throw new FailedRequestException("Unable to send alert to Slack."))
            .OnSuccess(response =>
            {
                if (!response.Require<bool>("ok"))
                    throw new FailedRequestException("Unable to send alert to Slack.");
            })
            .Post();
    }

    private void SendEmail(Alert alert)
    {
        if (_config.Optional<bool>("emailsDisabled"))
            return;
        
        string none = _config.Require<string>("emailDefault");
        string first = _config.Require<string>("emailFirstEscalation");
        string final = _config.Require<string>("emailSecondEscalation");
        
        string email = alert.Escalation switch
        {
            Alert.EscalationLevel.None => none,
            Alert.EscalationLevel.First => first,
            Alert.EscalationLevel.Final => final,
            _ => none
        };
        
        _api
            .Request(PlatformEnvironment.Url("dmz/alert"))
            .AddAuthorization(_config.AdminToken)
            .SetPayload(new RumbleJson
            {
                { "email", email },
                { "alert", alert.JSON }
            })
            .OnFailure(response =>
            {
                Log.Error(Owner.Will, "");
            })
            .Post();
    }
}