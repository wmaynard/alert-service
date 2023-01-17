using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using MongoDB.Driver;
using Rumble.Platform.Data;

namespace AlertService.Utilities.Minq;

public class MinqFilterBuilder<T> where T : PlatformDataModel
{
    internal enum FilterType { And, Not, Or }
    internal FilterType Type { get; set; }
    internal FilterDefinitionBuilder<T> Builder { get; init; }
    internal FilterDefinition<T> Filter => Builder.And(Filters);

    private List<FilterDefinition<T>> Filters { get; set; }

    internal MinqFilterBuilder(FilterType type = FilterType.And)
    {
        Builder = Builders<T>.Filter;
        Type = type;
        Filters = new List<FilterDefinition<T>>();
    }

    internal FilterDefinition<T> Build()
    {
        return Filters.Any()
            ? Type switch
            {
                FilterType.And => Builder.And(Filters),
                FilterType.Not when Filters.Count == 1 => Builder.Not(Filters.First()),
                FilterType.Not => Builder.Not(Builder.And(Filters)),
                FilterType.Or => Builder.Or(Filters)
            }
            : Builders<T>.Filter.Empty;
    }

    public MinqFilterBuilder<T> EqualTo<U>(Expression<Func<T, U>> field, U value)
    {
        Filters.Add(Builder.Eq(field, value));
        return this;
    }
    public MinqFilterBuilder<T> NotEqualTo<U>(Expression<Func<T, U>> field, U value)
    {
        Filters.Add(Builder.Ne(field, value));
        return this;
    }
    public MinqFilterBuilder<T> GreaterThan<U>(Expression<Func<T, U>> field, U value)
    {
        Filters.Add(Builder.Gt(field, value));
        return this;
    }
    
    public MinqFilterBuilder<T> GreaterThanOrEqualTo<U>(Expression<Func<T, U>> field, U value)
    {
        Filters.Add(Builder.Gte(field, value));
        return this;
    }
    public MinqFilterBuilder<T> LessThan<U>(Expression<Func<T, U>> field, U value)
    {
        Filters.Add(Builder.Lt(field, value));
        return this;
    }
    public MinqFilterBuilder<T> LessThanOrEqualTo<U>(Expression<Func<T, U>> field, U value)
    {
        Filters.Add(Builder.Lte(field, value));
        return this;
    }
    public MinqFilterBuilder<T> In<U>(Expression<Func<T, U>> field, IEnumerable<U> value)
    {
        Filters.Add(Builder.In(field, value));
        return this;
    }
    public MinqFilterBuilder<T> NotIn<U>(Expression<Func<T, U>> field, IEnumerable<U> value)
    {
        Filters.Add(Builder.Nin(field, value));
        return this;
    }
    public void AnyEq(){}
    public void AnyNe(){}
    public void AnyGt(){}
    public void AnyGte(){}
    public void AnyLt(){}
    public void AnyLte(){}
    public void AnyIn(){}
    public void AnyNin(){}
    public void Exists(){}
    public void Mod(){}
    public void ElemMatch(){}
    public void Size(){}
    public void SizeGt(){}
    public void SizeGte(){}
    public void SizeLt(){}
    public void SizeLte(){}
    
    public void Not(){}
    public void And(){}
    public void Or(){}
    
}