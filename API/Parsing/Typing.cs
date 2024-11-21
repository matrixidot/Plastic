namespace Plastic.API.Parsing;

using System.Linq.Expressions;
using static Parser;
using static Lexing.TokenType;

public static partial class Typing {
    public static Expression EnsureType(Expression expr, Type targetType) {
        return expr.Type != targetType ? Expression.Convert(expr, targetType) : expr;
    }

    public static (Expression, Expression) PromoteNumericTypes(Expression left, Expression right) {
        if (NumericTypes.Contains(left.Type) && NumericTypes.Contains(right.Type)) {
            if (left.Type == right.Type) return (left, right);
            Type toPromote = NumericPromotionRules[(left.Type, right.Type)];
            left = EnsureType(left, toPromote);
            right = EnsureType(right, toPromote);
            return (left, right);
        }
        throw new Exception($"{left.Type} or {right.Type} is not numeric.");
    } 
    
    public static Expression EnsureNumericType(Expression expr) {
        if (NumericTypes.Contains(expr.Type)) {
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