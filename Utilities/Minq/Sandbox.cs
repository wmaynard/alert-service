using System;
using System.Globalization;
using System.Linq;
using System.Linq.Expressions;
using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using MongoDB.Driver;
using MongoDB.Driver.Core.Misc;
using MongoDB.Driver.Linq;
using ExpressionVisitor = System.Linq.Expressions.ExpressionVisitor;

namespace AlertingService.Utilities.Minq;

public class Sandbox<T>
{
    private readonly LambdaExpression _expression;
    
    // public static string Render(Expression<Func<T, object>> field)
    // {
    //     PipelineBindingContext pipelineBindingContext = new PipelineBindingContext(serializerRegistry);
    //     LambdaExpression lambda = ExpressionHelper.GetLambda(PartialEvaluator.Evaluate((System.Linq.Expressions.Expression) this._expression));
    //     DocumentExpression replacement = new DocumentExpression((IBsonSerializer) documentSerializer);
    //     pipelineBindingContext.AddExpressionMapping((System.Linq.Expressions.Expression) lambda.Parameters[0], (System.Linq.Expressions.Expression) replacement);
    //     IFieldExpression fieldExpression;
    //     
    //     if (TryGetExpression<IFieldExpression>(FieldExpressionFlattener.FlattenFields(pipelineBindingContext.Bind(lambda.Body)), out fieldExpression))
    //         throw new InvalidOperationException("Unable to determine the serialization information.");
    //     
    //     var foo = new ExpressionFieldDefinition<T>(field)
    //         .Render(
    //             documentSerializer: BsonSerializer.SerializerRegistry.GetSerializer<T>(),
    //             serializerRegistry: BsonSerializer.SerializerRegistry
    //         );
    //
    //     return foo.FieldName;
    // }
    //
    // public static bool TryGetExpression<T>(Expression node, out T value) where T : class
    // {
    //     while (node.NodeType is ExpressionType.Convert or ExpressionType.ConvertChecked or ExpressionType.Quote)
    //         node = ((UnaryExpression)node).Operand;
    //
    //     value = node as T;
    //     return value != null;
    // }
    //
    // public static Expression FlattenFields(Expression node)
    // {
    //     FieldExpressionFlattener visitor = new FieldExpressionFlattener();
    //     return visitor.Visit(node);
    // }
    //
    // public RenderedFieldDefinition Render(IBsonSerializer<T> documentSerializer, IBsonSerializerRegistry serializerRegistry, LinqProvider linqProvider)
    // {
    //     return TranslateExpressionToField(_expression, documentSerializer, serializerRegistry);
    // }
    //
    //
    // internal RenderedFieldDefinition TranslateExpressionToField<TDocument>(LambdaExpression expression, IBsonSerializer<TDocument> documentSerializer, IBsonSerializerRegistry serializerRegistry)
    // {
    //     var parameter = expression.Parameters.Single();
    //     var context = TranslationContext.Create(expression, documentSerializer);
    //     var symbol = context.CreateSymbol(parameter, documentSerializer, isCurrent: true);
    //     context = context.WithSymbol(symbol);
    //     var body = RemovePossibleConvertToObject(expression.Body);
    //     var field = ExpressionToFilterFieldTranslator.Translate(context, body);
    //
    //     return new RenderedFieldDefinition(field.Path, field.Serializer);
    //
    //     static Expression RemovePossibleConvertToObject(Expression expression)
    //     {
    //         if (expression is UnaryExpression unaryExpression &&
    //             unaryExpression.NodeType == ExpressionType.Convert &&
    //             unaryExpression.Type == typeof(object))
    //         {
    //             return unaryExpression.Operand;
    //         }
    //
    //         return expression;
    //     }
}