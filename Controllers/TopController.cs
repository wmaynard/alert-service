using AlertingService.Models;
using Microsoft.AspNetCore.Mvc;
using Rumble.Platform.Common.Utilities;
using Rumble.Platform.Common.Web;
using AlertingService.Services;

namespace AlertingService.Controllers;

[Route("alert")]
public class TopController : PlatformController
{
    private readonly AlertService _alerts;

    public TopController(AlertService a) => _alerts = a;

    [HttpPost]
    public ActionResult Slack()
    {
        Alert alert = Require<Alert>("alert");

        Alert existing = _alerts.FindLastAlert(alert.Message);

        // The previous alert is too old; it's time to insert a new one.
        if (existing == null || existing.CreatedOn < Timestamp.UnixTime - alert.Trigger.Timeframe)
        {
            _alerts.Insert(alert);
            return Ok(alert);
        }
        else
        {
            existing.Trigger.Count++;
            _alerts.Update(existing);
            return Ok(existing);
        }
        
        return Ok();
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
        Alert alert = _alerts.FromId(Require<string>("alertId"));

        _alerts.Update(alert.Acknowledge());
        
        return Ok(alert);
    }

    [HttpGet, Route("resolve")]
    public ActionResult Resolve()
    {
        Alert alert =  _alerts.FromId(Require<string>("alertId"));
        
        _alerts.Update(alert.Resolve());

        return Ok(alert);
    }

    [HttpPatch, Route("cancel")]
    public ActionResult Cancel()
    {
        Alert alert = _alerts.FromId(Require<string>("alertId"));
        
        _alerts.Update(alert.Cancel());

        return Ok(alert);
    }
}
