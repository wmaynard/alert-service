// using System;
// using System.Collections.Generic;
// using System.Linq;
// using System.Linq.Expressions;
// using Microsoft.AspNetCore.Mvc;
// using MongoDB.Bson.Serialization;
// using MongoDB.Driver;
// using Rumble.Platform.Common.Exceptions;
// using Rumble.Platform.Common.Extensions;
// using Rumble.Platform.Data;
//
// namespace AlertingService.Utilities.Minq;
//
// public class MinqFilterBuilder<T> where T : PlatformDataModel
// {
//     internal enum FilterType { And, Not, Or }
//     internal FilterType Type { get; set; }
//     internal FilterDefinitionBuilder<T> Builder { get; init; }
//     internal FilterDefinition<T> Filter => Builder.And(Filters);
//
//     private List<FilterDefinition<T>> Filters { get; set; }
//
//     internal MinqFilterBuilder(FilterType type = FilterType.And)
//     {
//         Builder = Builders<T>.Filter;
//         Type = type;
//         Filters = new List<FilterDefinition<T>>();
//     }
//
//     internal FilterDefinition<T> Build()
//     {
//         return Filters.Any()
//             ? Type switch
//             {
//                 FilterType.And => Builder.And(Filters),
//                 FilterType.Not when Filters.Count == 1 => Builder.Not(Filters.First()),
//                 FilterType.Not => Builder.Not(Builder.And(Filters)),
//                 FilterType.Or => Builder.Or(Filters)
//             }
//             : Builders<T>.Filter.Empty;
//     }
//
//     public MinqFilterBuilder<T> Is<U>(U model) where U : PlatformCollectionDocument
//     {
//         if (model.Id == null || !model.Id.CanBeMongoId())
//             throw new PlatformException("Record does not exist, is not a CollectionDocument or the ID is invalid.");
//         return AddFilter($"{{_id:ObjectId('{model.Id}')}}");
//     }
//     public MinqFilterBuilder<T> EqualTo<U>(Expression<Func<T, U>> field, U value) => AddFilter(Builder.Eq(field, value));
//
//     public MinqFilterBuilder<T> NotEqualTo<U>(Expression<Func<T, U>> field, U value) => AddFilter(Builder.Ne(field, value));
//
//     public MinqFilterBuilder<T> GreaterThan<U>(Expression<Func<T, U>> field, U value) => AddFilter(Builder.Gt(field, value));
//
//     public MinqFilterBuilder<T> GreaterThanOrEqualTo<U>(Expression<Func<T, U>> field, U value) => AddFilter(Builder.Gte(field, value));
//
//     public MinqFilterBuilder<T> LessThan<U>(Expression<Func<T, U>> field, U value) => AddFilter(Builder.Lt(field, value));
//
//     public MinqFilterBuilder<T> LessThanOrEqualTo<U>(Expression<Func<T, U>> field, U value) => AddFilter(Builder.Lte(field, value));
//
//     /// <summary>
//     /// Returns documents where the specified field is contained within the provided enumerable.
//     /// </summary>
//     /// <param name="field"></param>
//     /// <param name="value"></param>
//     /// <typeparam name="U"></typeparam>
//     /// <returns></returns>
//     public MinqFilterBuilder<T> ContainedIn<U>(Expression<Func<T, U>> field, IEnumerable<U> value) => AddFilter(Builder.In(field, value));
//
//     /// <summary>
//     /// Returns documents where the specified field is not contained within the provided enumerable.
//     /// </summary>
//     /// <param name="field"></param>
//     /// <param name="value"></param>
//     /// <typeparam name="U"></typeparam>
//     /// <returns></returns>
//     public MinqFilterBuilder<T> NotContainedIn<U>(Expression<Func<T, U>> field, IEnumerable<U> value) => AddFilter(Builder.Nin(field, value));
//
//     /// <summary>
//     /// Returns documents where the specified field is an array that contains the specified value.
//     /// </summary>
//     /// <param name="field"></param>
//     /// <param name="value"></param>
//     /// <typeparam name="U"></typeparam>
//     /// <returns></returns>
//     public MinqFilterBuilder<T> Contains<U>(Expression<Func<T, IEnumerable<U>>> field, U value) => AddFilter(Builder.AnyEq(field, value));
//
//     public MinqFilterBuilder<T> DoesNotContain<U>(Expression<Func<T, IEnumerable<U>>> field, U value) => AddFilter(Builder.AnyNe(field, value));
//     public MinqFilterBuilder<T> ElementGreaterThan<U>(Expression<Func<T, IEnumerable<U>>> field, U value) => AddFilter(Builder.AnyGt(field, value));
//     public MinqFilterBuilder<T> ElementGreaterThanOrEqualTo<U>(Expression<Func<T, IEnumerable<U>>> field, U value) => AddFilter(Builder.AnyGte(field, value));
//     public MinqFilterBuilder<T> ElementLessThan<U>(Expression<Func<T, IEnumerable<U>>> field, U value) => AddFilter(Builder.AnyLt(field, value));
//     public MinqFilterBuilder<T> ElementLessThanOrEqualTo<U>(Expression<Func<T, IEnumerable<U>>> field, U value) => AddFilter(Builder.AnyLte(field, value));
//     public MinqFilterBuilder<T> ContainsOneOf<U>(Expression<Func<T, IEnumerable<U>>> field, IEnumerable<U> value) => AddFilter(Builder.AnyIn(field, value));
//     public MinqFilterBuilder<T> DoesNotContainOneOf<U>(Expression<Func<T, IEnumerable<U>>> field, IEnumerable<U> value) => AddFilter(Builder.AnyNin(field, value));
//
//
//     /// <summary>
//     /// Returns a document where the specified field exists on the database.  Note that this is different from null-checking or default-checking.
//     /// If you have a [BsonIgnoreIfNull] attribute on your model, and that property is null, then the field will not exist in the database.
//     /// </summary>
//     /// <param name="field"></param>
//     public MinqFilterBuilder<T> FieldExists(Expression<Func<T, object>> field) => AddFilter(Builder.Exists(field));
//     /// <summary>
//     /// Returns a document where the specified field is absent on the database.  Note that this is different from null-checking or default-checking.
//     /// If you have a [BsonIgnoreIfNull] attribute on your model, and that property is null, then the field will not exist in the database.
//     /// </summary>
//     /// <param name="field"></param>
//     public MinqFilterBuilder<T> FieldDoesNotExist(Expression<Func<T, object>> field) => AddFilter(Builder.Exists(field, exists: false));
//     
//     public MinqFilterBuilder<T> Mod(Expression<Func<T, object>> field, long modulus, long remainder) => AddFilter(Builder.Mod(field, modulus, remainder));
//
//     /// <summary>
//     /// Creates a filter using a nested model.
//     /// </summary>
//     /// <param name="field"></param>
//     /// <param name="builder"></param>
//     /// <typeparam name="U"></typeparam>
//     /// <returns></returns>
//     public MinqFilterBuilder<T> Where<U>(Expression<Func<T, IEnumerable<U>>> field, Action<MinqFilterBuilder<U>> builder) where U : PlatformDataModel
//     {
//         MinqFilterBuilder<U> filter = new MinqFilterBuilder<U>();
//         builder.Invoke(filter);
//         
//         return AddFilter(Builder.ElemMatch(field, filter.Filter));
//     }
//
//     public MinqFilterBuilder<T> LengthEquals<U>(Expression<Func<T, object>> field, int size) => AddFilter(Builder.Size(field, size));
//     public MinqFilterBuilder<T> LengthGreaterThan(Expression<Func<T, object>> field, int size) => AddFilter(Builder.SizeGt(field, size));
//     public MinqFilterBuilder<T> LengthGreaterThanOrEqualTo(Expression<Func<T, object>> field, int size) => AddFilter(Builder.SizeGte(field, size));
//     public MinqFilterBuilder<T> LengthLessThan(Expression<Func<T, object>> field, int size) => AddFilter(Builder.SizeLt(field, size));
//     public MinqFilterBuilder<T> LengthLessThanOrEqualTo(Expression<Func<T, object>> field, int size) => AddFilter(Builder.SizeLte(field, size));
//
//     public MinqFilterBuilder<T> GreaterThanOrEqualToRelative(string field1, string field2) => AddFilter($"{{ $expr: {{ $gte: [ '${field1}', '${field2}' ] }} }}");
//     public MinqFilterBuilder<T> GreaterThanOrEqualToRelative(Expression<Func<T, object>> field1, Expression<Func<T, object>> field2)
//     {
//         // FilterDefinition<T> filter = $"{{ $expr: {{ $gte: [ '${Render(field1)}', '${Render(field2)}' ] }} }}";
//         // ExpressionFieldDefinition<T> fd = new ExpressionFieldDefinition<T>(field1);
//         // RenderedFieldDefinition foo = fd.Render(
//         //     BsonSerializer.SerializerRegistry.GetSerializer<T>(),
//         //     BsonSerializer.SerializerRegistry
//         // );
//         //{$expr: { $gte: ["$Trigger.Count", "$Trigger.CountRequired"]}}
//         return AddFilter($"{{ $expr: {{ $gte: [ '${Render(field1)}', '${Render(field2)}' ] }} }}");
//     }
//
//     // Unnecessary?
//     public void Not(){}
//     public void And(){}
//     public void Or(){}
//
//     private MinqFilterBuilder<T> AddFilter(FilterDefinition<T> filter)
//     {
//         Filters.Add(filter);
//         return this;
//     }
//     public static string Render(Expression<Func<T, object>> field) => new ExpressionFieldDefinition<T>(field)
//         .Render(
//             documentSerializer: BsonSerializer.SerializerRegistry.GetSerializer<T>(),
//             serializerRegistry: BsonSerializer.SerializerRegistry
//         ).FieldName;
// }
//
// public static class MinqExpressionExtension
// {
//     public static string GetFieldName<T>(this Expression<Func<T, object>> field) where T : PlatformCollectionDocument => Minq<T>.Render(field);
// }