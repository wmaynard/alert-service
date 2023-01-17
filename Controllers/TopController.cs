using System;
using System.Collections.Generic;
using AlertService.Models;
using AlertService.Utilities;
using AlertService.Utilities.Minq;
using Microsoft.AspNetCore.Mvc;
using MongoDB.Driver;
using RCL.Logging;
using Rumble.Platform.Common.Attributes;
using Rumble.Platform.Common.Services;
using Rumble.Platform.Common.Utilities;
using Rumble.Platform.Common.Web;
using Rumble.Platform.Data;

namespace AlertService.Controllers;

[Route("alert")]
public class TopController : PlatformController
{
    private readonly AlertService _alertService;
    
    [HttpGet, Route("test")]
    public ActionResult Test()
    {
        _alertService.Test(Optional<bool>("all"));
        
        return Ok();
    }
}



public class AlertService : PlatformMongoService<Alert>
{
    public AlertService() : base("alerts")
    {
        
    }

    public void Test(bool flag)
    {
        DynamicConfig.Instance.Interval = 15_000;
DynamicConfig.Instance.OnRefresh += (sender, json) =>
{
    Log.Local(Owner.Will, $"Hiya!  DC2 returned values! {json.Keys.Count}");
};
        _collection
            .Find(_ => true)
            .ToList();
        // _collection.InsertOne();
        
        Minq<Alert> minq = Minq<Alert>.Connect(CollectionName);

        Builders<Models.Alert>.Filter.Gt(alert => alert.Count, 5);



        if (flag)
            minq
                .Filter()
                .Delete();
        else
            minq
                .Filter(builder => builder.GreaterThan(alert => alert.Count, 5))
                .And(builder => builder.LessThan(alert => alert.Timestamp, Timestamp.UnixTime))
                .Not(builder => builder.EqualTo(alert => alert.Animal, "cat"))
                .Delete();

        List<Alert> alerts = new List<Alert>();
        while (alerts.Count < 1000)
            alerts.Add(new Alert
            {
                Message = Guid.NewGuid().ToString(),
                Timestamp = Timestamp.UnixTime,
                Count = new Random().Next(0, 100),
                Foo = new Random().Next(0, 100),
                Animal = new Random().Next(0, 100) switch
                {
                    < 10 => "dog",
                    < 50 => "cat",
                    < 60 => "turtle",
                    < 80 => "bird",
                    _ => "fox"
                }
            });

        Alert[] toInsert = alerts.ToArray();
        minq.Insert(toInsert);

        List<Alert> all = minq.Filter().Find();
        
        // minq.Filter().Update(updater => updater
        //     .Increment(alert => alert.Count, 300)
        //     .Set(alert => alert.Animal, "tribble")
        //     .Union(alert => alert.Collection, "Hello, World!")
        // );
        // minq.Filter().Update(updater => updater.Union(alert => alert.Collection, "Boozy!"));
        // minq.Filter().Update(updater => updater.RemoveItems(
        //     field: alert => alert.Collection, 
        //     "Hello, World2!"
        // ).RemoveFirstItem(alert => alert));

        minq.Filter().Update(updater => updater
            .Increment(alert => alert.Count, 300)
            .Set(alert => alert.Animal, "tribble")
            .Union(alert => alert.Collection, "Apple!")
        );
        minq.Filter().Update(updater => updater.Union(alert => alert.Collection, "Boozy!"));
        minq.Filter().Update(updater => updater.Union(alert => alert.Collection, "Cake!", "Delicious!", "E. Coli!"));
        // minq.Filter().Update(updater => updater
        //     .RemoveItems(
        //         field: alert => alert.Collection, 
        //         "Hello, World2!"
        //     )
        //     .RemoveLastItem(alert => alert.Collection)
        //     .RemoveFirstItem(alert => alert.Collection)
        // );
        minq.Filter().Update(builder => builder.RemoveItems(alert => alert.Collection, "Boozy!"));
        minq.Filter().Update(builder => builder.CurrentTimestamp(alert => alert.Foo));
        minq.Filter().Update(builder => builder.Increment(alert => alert.Bar, 38.6));
        minq.Filter().Update(builder => builder.Minimum(alert => alert.Animal, "bar"));

    }

    // public void Pseudo()
    // {
    //     dynamic collection = new { };
    //     dynamic model = null;
    //
    //     collection
    //         .Find(builder => builder.Gt(alert => alert.Timestamp, 0));
    //     collection
    //         .Find(builder => builder.Gt(alert => alert.Timestamp, 0))
    //         .Update(builder => builder
    //             .Increment(alert => alert.Count, 1)
    //             .Set(alert => alert.Message, "foo")
    //         );
    //     collection
    //         .Find(builder => builder.Gt(alert => alert.Timestamp, 0))
    //         .Delete();
    //     
    //     collection
    //         .Find(builder => builder.Gt(alert => alert.Timestamp, 0))
    //         .Replace(model);
    //
    //     collection.Insert(paramModels);
    // }
}