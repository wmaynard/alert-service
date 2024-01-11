using System;
using System.Collections.Generic;
using System.Linq;
using MongoDB.Driver.Linq;
using RCL.Logging;
using Rumble.Platform.Common.Exceptions;
using Rumble.Platform.Common.Interop;
using Rumble.Platform.Common.Minq;
using Rumble.Platform.Common.Models.Alerting;
using Rumble.Platform.Common.Services;
using Rumble.Platform.Common.Utilities;
using Rumble.Platform.Data;

namespace AlertingService.Services;

public class AlertService : MinqTimerService<Alert>
{
    public AlertService() : base("incidents", IntervalMs.OneMinute) { }
    
    public Alert FindLastAlert(Alert incoming) => mongo
        .Where(query => query
            .EqualTo(alert => alert.Status, Alert.AlertStatus.Pending)
            .EqualTo(alert => alert.Title, incoming.Title)
            .EqualTo(alert => alert.Message, incoming.Message)
        )
        .Sort(sort => sort.OrderByDescending(alert => alert.CreatedOn))
        .Limit(1)
        .FirstOrDefault();
    
    private void CloseOldAlerts() => mongo
        .OnRecordsAffected(result => Log.Local(Owner.Will, $"{result.Affected} alerts closed."))
        .Where(query => query
            .EqualTo(alert => alert.Status, Alert.AlertStatus.Pending)
            .LessThan(alert => alert.Expiration, Timestamp.Now)
            .LessThanRelative(alert => alert.Trigger.Count, alert => alert.Trigger.CountRequired)
        )
        .Update(query => query.Set(alert => alert.Status, Alert.AlertStatus.TriggerNotMet));
    
    private Alert[] GetPendingAlerts() => mongo
        .Where(query => query
            .EqualTo(alert => alert.Status, Alert.AlertStatus.Pending)
            .GreaterThanOrEqualToRelative(alert => alert.Trigger.Count, alert => alert.Trigger.CountRequired)
        )
        .ToArray();

    protected override void OnElapsed()
    {
        CloseOldAlerts();
        Alert[] outbox = GetPendingAlerts();

        if (!outbox.Any())
            return;

        Log.Local(Owner.Will, $"Found {outbox.Length} alerts to send.");

        foreach (Alert toSend in outbox)
        {
            PagerDutyIncident incident = PagerDuty.CreateIncident(toSend, level: PlatformEnvironment.IsProd
                ? PagerDuty.Urgency.RedAlert
                : PagerDuty.Urgency.YellowAlert
            );
            if (incident == null)
            {
                Log.Warn(Owner.Will, "Potentially failed to create PD incident, alert will remain open in mongo; requires investigation.", data: new
                {
                    Alert = toSend
                });
                continue;
            }

            toSend.Status = Alert.AlertStatus.Sent;
            toSend.PagerDutyEventId = incident.Id;
            toSend.SentOn = Timestamp.Now;

            mongo.Update(toSend);
            
            Log.Local(Owner.Will, $"{toSend}", emphasis: Log.LogType.CRITICAL);
        }
    }
}