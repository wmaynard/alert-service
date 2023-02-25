using System;
using Microsoft.AspNetCore.Mvc;
using Rumble.Platform.Common.Utilities;
using Rumble.Platform.Common.Web;
using AlertingService.Services;
using Rumble.Platform.Common.Attributes;
using Rumble.Platform.Common.Enums;
using Rumble.Platform.Common.Exceptions;
using Rumble.Platform.Common.Models.Alerting;
using Rumble.Platform.Data;

namespace AlertingService.Controllers;

[Route("alert")]
public class TopController : PlatformController
{
    private const int RESEND_INTERVAL = 60 * 60 * 12; // 12 hours, in seconds.
    private readonly AlertService _alerts;

    public TopController(AlertService a) => _alerts = a;

    [HttpPost, RequireAuth(AuthType.ADMIN_TOKEN)]
    public ActionResult UpsertAlert()
    {
        Alert alert = Require<Alert>("alert");
        Alert existing = _alerts.FindLastAlert(alert);

        if (existing == null)
        {
            alert.Trigger.Count++;
            _alerts.Insert(alert);
            return Ok(alert);
        }

        switch (existing.Status)
        {
            case Alert.AlertStatus.Pending:
            case Alert.AlertStatus.Sent:
            case Alert.AlertStatus.Acknowledged:
            case Alert.AlertStatus.Escalated:
            case Alert.AlertStatus.PendingResend:
                existing.Trigger.Count++;
                if (existing.LastSent == Timestamp.UnixTime - RESEND_INTERVAL)
                    existing.Status = Alert.AlertStatus.PendingResend;
                _alerts.Update(existing);
                return Ok(existing);
            case Alert.AlertStatus.Resolved:
            case Alert.AlertStatus.Canceled:
            default:
                _alerts.Insert(alert);
                return Ok(alert);
        }
    }

    [HttpPatch, Route("snooze")]
    public ActionResult Snooze()
    {
        Alert alert = _alerts.FromId(Require<string>("alertId"));
        int minutes = Require<int>("minutes");

        _alerts.Update(alert.Snooze(minutes));
        
        return Ok(alert);
    }

    [HttpPatch, Route("escalate")]
    public ActionResult Escalate() => Ok();

    [HttpGet, Route("acknowledge")]
    public ActionResult Acknowledge()
    {
        Alert alert = _alerts.FromId(Require<string>("id"));

        _alerts.Update(alert.Acknowledge());
        
        return Ok(alert);
    }

    [HttpGet, Route("resolve")]
    public ActionResult Resolve()
    {
        Alert alert =  _alerts.FromId(Require<string>("id"));
        
        _alerts.Update(alert.Resolve());

        return Ok(alert);
    }

    [HttpPatch, Route("cancel")]
    public ActionResult Cancel()
    {
        Alert alert = _alerts.FromId(Require<string>("id"));
        
        _alerts.Update(alert.Cancel());

        return Ok(alert);
    }

    [HttpGet, Route("test")]
    public ActionResult Test()
    {
        try
        {
            throw new NotImplementedException();
        }
        catch (Exception e)
        {
            Alert submitted = _apiService.Alert(
                title: "Hello, Demo!",
                message: "This is an example alert from the ApiService!",
                countRequired: 3,
                timeframe: 300,
                data: new RumbleJson
                {
                    { "textExceptionMessage", e.Message }
                },
                impact: ImpactType.ServiceUnusable
            );

            if (submitted == null)
                throw new PlatformException("Unable to send test alert.");

            return Ok(submitted);
        }

        return Ok();
    }
}
