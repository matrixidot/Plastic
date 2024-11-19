namespace Plastic.API.Parsing;

using System.Linq.Expressions;

public static partial class Typing {
    public static Expression EnsureType(Expression expr, Type targetType) {
        return expr.Type != targetType ? Expression.Convert(expr, targetType) : expr;
    }
    public static Expression EnsureNumericType(Expression expr) {
        if (expr.Type == typeof(int) || expr.Type == typeof(long) || expr.Type == typeof(double)) {
            return expr;
        }
        throw new Exception($"Operand is not a numeric type: {expr.Type}");
    }

    public static Expression EnsureIntegerType(Expression expr) {
        if (expr.Type == typeof(int) || expr.Type == typeof(long)) {
            return expr;
        }
        throw new Exception($"Operand is not an integer type: {expr.Type}");
    }

    public static Expression EnsureBooleanType(Expression expr) {
        if (expr.Type == typeof(bool)) {
            return expr;
        }
        throw new Exception($"Operand is not a boolean type: {expr.Type}");
    }
    
}