using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using MongoDB.Driver;
using Rumble.Platform.Common.Utilities;
using Rumble.Platform.Data;

namespace AlertingService.Utilities;





public class RongoUpdate<T, TField>
{
    private UpdateDefinition<T> update;

    public RongoUpdate<T, TField> Increment(FieldDefinition<T, TField> field, TField amount)
    {
        update.Inc(field, value: amount);
        return this;
    }

    public void Foo()
    {
    }
}

[Flags]
public enum RongoOperation
{
    DeleteOne,
    DeleteMany,
    FindOne,
    FindMany,
    InsertOne,
    InsertMany,
    Projection,
    ReplaceOne,
    UpdateOne,
    UpdateMany
}

























