using System;
using System.Linq;
using MongoDB.Driver;
using RCL.Logging;
using Rumble.Platform.Common.Utilities;
using Rumble.Platform.Data;

namespace AlertService.Utilities.Minq;

public class Minq<T> where T : PlatformCollectionDocument
{
    private static MongoClient _client { get; set; }
    internal readonly IMongoCollection<T> Collection;
    
    public Minq(IMongoCollection<T> collection)
    {
        Collection = collection;
    }

    public MinqRequest<T> Filter(params FilterDefinition<T>[] filters)
    {
        if (!filters.Any())
            return new MinqRequest<T>(this);
        return new MinqRequest<T>(this)
        {
            _filter = filters.Any()
                ? Builders<T>.Filter.Empty
                : Builders<T>.Filter.And(filters)
        };
    }

    public MinqRequest<T> Filter<TField>(FieldDefinition<T, TField> field, TField value)
    {
        return new MinqRequest<T>(this)
        {
            _filter = Builders<T>.Filter.Gt(field, value)
        };
    }

    public MinqRequest<T> Filter(Action<MinqFilterBuilder<T>> builder)
    {
        MinqFilterBuilder<T> filter = new MinqFilterBuilder<T>();
        builder.Invoke(filter);
        
        return new MinqRequest<T>(this);
    }
    
    public void Insert(params T[] models)
    {
        if (!models.Any())
            throw new Exception();
        Collection.InsertMany(models);
    }
    
    public static Minq<T> Connect(string collectionName)
    {
        _client ??= new MongoClient(PlatformEnvironment.MongoConnectionString);
        
        return new Minq<T>(_client
            .GetDatabase(PlatformEnvironment.MongoDatabaseName)
            .GetCollection<T>(collectionName)
        );
    }
}
