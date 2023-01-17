using System.Collections.Generic;
using Rumble.Platform.Data;

namespace AlertService.Models;

public class Alert : PlatformCollectionDocument
{
    public string Message { get; set; }
    public long Timestamp { get; set; }
    public long Count { get; set; }
    public long Foo { get; set; }
    public short Bar { get; set; }
    public string Animal { get; set; }
    public HashSet<string> Collection { get; set; }

    public Alert()
    {
        // Collection ??= System.Array.Empty<string>();
        Collection = new HashSet<string>();
    }
}