using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Threading.Channels;
using AlertingService.Services;
using RCL.Logging;
using Rumble.Platform.Common.Extensions;
using Rumble.Platform.Common.Interop;
using Rumble.Platform.Common.Utilities;
using Rumble.Platform.Data;

namespace AlertingService.Models;

public class Alert : PlatformCollectionDocument
{
    public Owner Owner { get; set; }
    public long DelayInMs { get; set; }
    public string Title { get; set; }
    public string Message { get; set; }
    public ImpactType Impact { get; set; }
    public RumbleJson Data { get; set; }
    public long SendAfter { get; set; }
    public long EscalationPeriod { get; set; }
    public long LastEscalation { get; set; }
    public long LastSent { get; set; }
    public long CreatedOn { get; set; }
    public Trigger Trigger { get; set; }
    
    public AlertStatus Status { get; set; }
    public AlertType Type { get; set; }
    public EscalationLevel Escalation { get; set; }

    protected override void Validate(out List<string> errors)
    {
        errors = new List<string>();
        if (string.IsNullOrWhiteSpace(Title))
            errors.Add("Title is required.");
        if (string.IsNullOrWhiteSpace(Message))
            errors.Add("Message is required.");
        if (Trigger == null)
            errors.Add("A trigger definition is required.");

        DelayInMs = Math.Max(0, DelayInMs);
        EscalationPeriod = Math.Min(5_000, Math.Max(0, EscalationPeriod));
        LastEscalation = 0;
        Status = AlertStatus.Unsent;
        Escalation = EscalationLevel.None;
        SendAfter = Timestamp.UnixTime + DelayInMs;
        CreatedOn = Timestamp.UnixTime;
        Trigger ??= new Trigger();
        Trigger.Count = 1;
    }

    public Alert Acknowledge()
    {
        Status = AlertStatus.Acknowledged;
        SendAfter = Timestamp.UnixTime + AlertService.SECONDS_BEFORE_ESCALATION * 2;

        return this;
    }

    public Alert Escalate()
    {
        Status = AlertStatus.Escalated;
        Escalation = Escalation switch
        {
            EscalationLevel.None => EscalationLevel.First,
            EscalationLevel.First => EscalationLevel.Final,
            _ => EscalationLevel.First
        };
        LastEscalation = Timestamp.UnixTime;
        SendAfter = Timestamp.UnixTime + AlertService.SECONDS_BEFORE_ESCALATION;
        return this;
    }

    public Alert Resolve()
    {
        Status = AlertStatus.Resolved;
        SendAfter = long.MaxValue;

        return this;
    }

    public Alert Cancel()
    {
        Status = AlertStatus.Canceled;
        SendAfter = long.MaxValue;

        return this;
    }

    public Alert Snooze(int minutes)
    {
        Status = AlertStatus.Unsent;
        Escalation = EscalationLevel.None;
        SendAfter = Timestamp.UnixTime + minutes * 60;

        return this;
    }

    public override string ToString() => $"{Status.GetDisplayName()} | {Escalation.GetDisplayName()} | {Impact.GetDisplayName()} | {Title} | {Message}";

    public SlackMessage ToSlackMessage(string channel)
    {
        string ping = null;
        try
        {
            ping = Escalation switch
            {
                EscalationLevel.None => SlackUser.Find(Owner).Tag,
                _ => "<!here>"
            };
        }
        catch (Exception e)
        {
            ping = "<!here>";
        }

        string status = Status == AlertStatus.Unsent
            ? AlertStatus.Sent.GetDisplayName()
            : Status.GetDisplayName();

        string details = $"```Active: {(Timestamp.UnixTime - CreatedOn).ToFriendlyTime()}\nStatus: {status}\nImpact: {Impact.GetDisplayName()}";
        if (Data != null)
            details += $"Data:\n{Data.Json}";
        details += "```";

        SlackMessage output = new SlackMessage
        {
            Blocks = new List<SlackBlock>
            {
                new SlackBlock(SlackBlock.BlockType.HEADER, Title),
                new SlackBlock($"{ping} {Message}"),
                new SlackBlock(details),
                new SlackBlock(SlackBlock.BlockType.DIVIDER),
#if DEBUG
                new SlackBlock($"<{PlatformEnvironment.Url($"https://localhost:5201/alert/acknowledge?id={Id}")}|Acknowledge>"),
                new SlackBlock($"<{PlatformEnvironment.Url($"https://localhost:5201/alert/resolve?id={Id}")}|Resolve>"),
                new SlackBlock($"<{PlatformEnvironment.Url($"https://localhost:5201/alert/cancel?id={Id}")}|Cancel>"),
#else
                new SlackBlock($"<{PlatformEnvironment.Url($"alert/acknowledge?id={Id}")}|Acknowledge>"),
                new SlackBlock($"<{PlatformEnvironment.Url($"alert/resolve?id={Id}")}|Resolve>"),
                new SlackBlock($"<{PlatformEnvironment.Url($"alert/cancel?id={Id}")}|Cancel>"),
#endif
            },
            Channel = channel
        };

        return output.Compress();;
    }
}

public class Trigger : PlatformDataModel
{
    public int CountRequired { get; set; }
    public long Timeframe { get; set; }
    public int Count { get; set; }
}

public enum AlertType
{
    Slack = 100,
    Email = 200
}

public enum AlertStatus
{
    Unsent = 100,
    Sent = 200,
    Acknowledged = 201,
    Escalated = 202,
    Resolved = 300,
    Canceled = 400
}

public enum EscalationLevel
{
    None = 100,
    First = 200,
    Final = 300
}

public enum ImpactType
{
    None = 100,
    ServicePartiallyUsable = 200,
    ServiceUnusable = 201,
    EnvironmentPartiallyUsable = 300,
    EnvironmentUnusable = 301,
    Unknown = 400
}