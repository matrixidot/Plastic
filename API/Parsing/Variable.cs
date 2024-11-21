using System.Linq.Expressions;

namespace Plastic.API.Parsing;

public record Variable(string Name, Type Type, ParameterExpression Expr, object? Value) {
    
}