using System;
using System.Collections.Generic;
using System.Linq;
using AlertingService.Models;
using RCL.Logging;
using Rumble.Platform.Common.Exceptions;
using Rumble.Platform.Common.Interop;
using Rumble.Platform.Common.Minq;
using Rumble.Platform.Common.Models.Alerting;
using Rumble.Platform.Common.Services;
using Rumble.Platform.Common.Utilities;
using Rumble.Platform.Data;

namespace AlertingService.Services;

public class AlertService : MinqService<Alert>
{
    public AlertService() : base("alerts") { }

    // TODO: Order by descending
    public Alert FindLastAlert(string message) => mongo
        .Where(query => query
            .EqualTo(alert => alert.Message, message)
        )
        .ToList()
        .MaxBy(alert => alert.CreatedOn);
}