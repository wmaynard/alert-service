using System;
using System.Collections.Generic;
using System.Linq;
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

    public Alert FindLastAlert(Alert incoming) => mongo
        .Where(query => query
            .EqualTo(alert => alert.Title, incoming.Title)
            .EqualTo(alert => alert.Message, incoming.Message)
        )
        .Sort(sort => sort.OrderByDescending(alert => alert.CreatedOn))
        .Limit(1)
        .FirstOrDefault();
}