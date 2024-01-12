using Microsoft.AspNetCore.Mvc;
using Rumble.Platform.Common.Utilities;
using Rumble.Platform.Common.Web;
using AlertingService.Services;
using Rumble.Platform.Common.Attributes;
using Rumble.Platform.Common.Models.Alerting;

namespace AlertingService.Controllers;

[Route("alert")]
public class TopController : PlatformController
{
    #pragma warning disable
    private readonly AlertService _alerts;
    #pragma warning restore

    [HttpPost, RequireAuth(AuthType.ADMIN_TOKEN)]
    public ActionResult UpsertAlert()
    {
        Alert alert = Require<Alert>("alert");
        Alert existing = _alerts.FindLastAlert(alert);

        if (existing == null)
        {
            _alerts.Insert(alert);
            return Ok(alert);
        }

        switch (existing.Status)
        {
            case Alert.AlertStatus.Pending:
                existing.Trigger.Count += alert.Trigger.Count;
                _alerts.Update(existing);
                return Ok(existing);
            case Alert.AlertStatus.Sent:
            case Alert.AlertStatus.TriggerNotMet:
            default:
                _alerts.Insert(alert);
                return Ok(alert);
        }
    }
}
