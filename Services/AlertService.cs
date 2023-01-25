using System;
using System.Collections.Generic;
using System.Linq;
using AlertingService.Utilities.Minq;
using AlertingService.Models;
using RCL.Logging;
using Rumble.Platform.Common.Exceptions;
using Rumble.Platform.Common.Interop;
using Rumble.Platform.Common.Minq;
using Rumble.Platform.Common.Services;
using Rumble.Platform.Common.Utilities;
using Rumble.Platform.Data;

namespace AlertingService.Services;

public class AlertService : MinqService<Alert>
{
    public static long SECONDS_BEFORE_ESCALATION => DynamicConfig
        .Instance
        ?.Optional<long>("secondsBeforeEscalation")
        ?? 1_800;

    
    public AlertService() : base("alerts") { }

    // TODO: Order by descending
    public Alert FindLastAlert(string message) => mongo
        .Where(query => query
            .EqualTo(alert => alert.Message, message)
        )
        .ToList()
        .MaxBy(alert => alert.CreatedOn);
}

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
            .Where(query => query
                .NotContainedIn(alert => alert.Status, new [] { AlertStatus.Resolved, AlertStatus.Canceled })
                .NotEqualTo(alert => alert.Escalation, EscalationLevel.Final) // Should we re-notify?
                .LessThanOrEqualTo(alert => alert.SendAfter, Timestamp.UnixTime)
                .GreaterThanOrEqualToRelative(alert => alert.Trigger.Count, alert => alert.Trigger.CountRequired)
            )
            .ToList();

        foreach (Alert toSend in outbox)
        {
            switch (toSend.Status)
            {
                case AlertStatus.Unsent:
                    Send(toSend);
                    break;
                case AlertStatus.Acknowledged:
                case AlertStatus.Sent:
                case AlertStatus.Escalated:
                    toSend.Escalate();
                    break;
                case AlertStatus.Resolved:
                case AlertStatus.Canceled:
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
            if (alert.Type == AlertType.Slack)
                SendToSlack(alert);
            else
                SendEmail(alert);
            
            alert.LastSent = Timestamp.UnixTime;
            alert.Status = AlertStatus.Sent;
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
            EscalationLevel.None => none,
            EscalationLevel.First => first,
            EscalationLevel.Final => final,
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