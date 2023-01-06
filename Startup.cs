using RCL.Logging;
using Rumble.Platform.Common.Enums;
using Rumble.Platform.Common.Web;

namespace Alert;

public class Startup : PlatformStartup
{
    protected override PlatformOptions ConfigureOptions(PlatformOptions options) => options
        .SetRegistrationName("Alerts")
        .SetTokenAudience(Audience.AlertService)
        .SetProjectOwner(Owner.Will)
        .SetPerformanceThresholds(warnMS: 30_000, errorMS: 60_000, criticalMS: 90_000)
        .DisableFeatures(CommonFeature.ConsoleObjectPrinting);
}