using System;
using System.Collections.Generic;
using RCL.Logging;
using Rumble.Platform.Common.Exceptions;
using Rumble.Platform.Common.Interop;
using Rumble.Platform.Common.Minq;
using Rumble.Platform.Common.Models.Alerting;
using Rumble.Platform.Common.Services;
using Rumble.Platform.Common.Utilities;

namespace AlertingService.Services;


public class SenderService : MinqTimerService<Alert>
{
    private readonly DynamicConfig _config;
    private readonly ApiService _api;
    
    public SenderService(DynamicConfig config, ApiService api) : base("alerts", 5_000)
    {
        _config = config;
        _api = api;
    }

    protected override void OnElapsed()
    {
        List<Alert> outbox = mongo
            .WithTransaction(out Transaction ta)
            .Where(query => query
                .NotContainedIn(alert => alert.Status, new [] { Alert.AlertStatus.Resolved, Alert.AlertStatus.Canceled })
                .NotEqualTo(alert => alert.Escalation, Alert.EscalationLevel.Final) // Should we re-notify?
                .LessThanOrEqualTo(alert => alert.SendAfter, Timestamp.UnixTime)
                .GreaterThanOrEqualToRelative(alert => alert.Trigger.Count, alert => alert.Trigger.CountRequired)
            )
            .ToList();

        foreach (Alert toSend in outbox)
        {
            switch (toSend.Status)
            {
                case Alert.AlertStatus.Unsent:
                    Send(toSend);
                    break;
                case Alert.AlertStatus.Acknowledged:
                case Alert.AlertStatus.Sent:
                case Alert.AlertStatus.Escalated:
                    toSend.Escalate();
                    break;
                case Alert.AlertStatus.Resolved:
                case Alert.AlertStatus.Canceled:
                default:
                    break;
            }
            
            Log.Local(Owner.Will, $"{toSend}", emphasis: Log.LogType.CRITICAL);
            Update(toSend);
        }
    }

    private void Send(Alert alert)
    {
        try
        {
            if (alert.Type == Alert.AlertType.All)
            {
                SendToSlack(alert);
                SendEmail(alert);
            }
            else if (alert.Type == Alert.AlertType.Slack)
                SendToSlack(alert);
            else
                SendEmail(alert);
            
            alert.LastSent = Timestamp.UnixTime;
            alert.Status = Alert.AlertStatus.Sent;
        }
        catch (Exception e)
        {
            Log.Error(Owner.Will, "Unable to send alert.", data: new
            {
                Alert = alert.ToString()
            });
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
    private void SendEmail(Alert alert) { }
}