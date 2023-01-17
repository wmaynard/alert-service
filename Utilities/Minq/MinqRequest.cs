using System;
using System.Collections.Generic;
using MongoDB.Bson.Serialization;
using MongoDB.Driver;
using Rumble.Platform.Common.Exceptions;
using Rumble.Platform.Data;

namespace AlertService.Utilities.Minq;

public class MinqRequest<T> where T : PlatformCollectionDocument
{
    private IMongoCollection<T> _collection => Parent.Collection;
    internal FilterDefinition<T> _filter { get; set; }
    internal UpdateDefinition<T> _update { get; set; }
    private Minq<T> Parent { get; set; }
    private bool Consumed { get; set; }
    
    public MinqRequest(Minq<T> parent)
    {
        _update = Builders<T>.Update.Combine();
        _filter = Builders<T>.Filter.Empty;
        Parent = parent;
    }

    public long Update(Action<MinqUpdate<T>> action)
    {
        Consumed = true;
        MinqUpdate<T> update = new MinqUpdate<T>();
        action.Invoke(update);
        
        string foo = update.Update.Render(
            BsonSerializer.SerializerRegistry.GetSerializer<T>(),
            BsonSerializer.SerializerRegistry
        ).AsBsonDocument.ToString();
        try
        {
            
            return _collection.UpdateMany(_filter, update.Update).ModifiedCount;
        }
        catch (MongoWriteException e)
        {
            if (e.WriteError.Code == 40 || e.Message.Contains("conflict"))
                throw new PlatformException("Write conflict encountered.  Check that you aren't updating the same field multiple times in one query.");
            throw;
        }
    }

    public MinqRequest<T> Or(Action<MinqFilterBuilder<T>> builder)
    {
        MinqFilterBuilder<T> or = new MinqFilterBuilder<T>();
        builder.Invoke(or);
        _filter = Builders<T>.Filter.Or(_filter, or.Filter);
        return this;
    }
    
    public MinqRequest<T> And(Action<MinqFilterBuilder<T>> builder)
    {
        MinqFilterBuilder<T> and = new MinqFilterBuilder<T>();
        builder.Invoke(and);
        _filter = Builders<T>.Filter.Or(_filter, and.Filter);
        return this;
    }
    
    public MinqRequest<T> Not(Action<MinqFilterBuilder<T>> builder)
    {
        MinqFilterBuilder<T> not = new MinqFilterBuilder<T>();
        builder.Invoke(not);
        
        _filter = Builders<T>.Filter.And(_filter, Builders<T>.Filter.Not(not.Filter));
        return this;
    }

    public void Delete()
    {
        Consumed = true;
        _collection.DeleteMany(_filter);
    }

    public List<T> Find()
    {
        Consumed = true;
        return _collection.Find(_filter).ToList();
    }

    public long Count()
    {
        Consumed = true;
        return _collection.CountDocuments(_filter);
    }
    
    // update, delete, replace, project
}
