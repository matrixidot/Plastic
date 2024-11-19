namespace Plastic.API.Parsing;

using System.Linq.Expressions;
using Errors;
using Lexing;
using static Lexing.TokenType;
using static Typing;

public class Parser(List<Token> Tokens) {
    private int current = 0;

    public BlockExpression Parse() {
        List<Expression> statements = new();
        while (!IsAtEnd) {
            statements.Add(Statement());
        }

        return Expression.Block(statements); 
    }

    private Expression Statement() {
        if (Match(PRINT)) return PrintStatement();
        return ExpressionStatement();
    }

    private Expression PrintStatement() {
        Expression value = Expr();
        Consume(SEMICOLON, "Expect ';' after value.");
        value = EnsureType(value, typeof(object));
        return Expression.Call(writeLine, value);
    }

    private Expression ExpressionStatement() {
        Expression value = Expr();
        Consume(SEMICOLON, "Expect ';' after expression.");
        return value;
    }
    
    private Expression Expr() {
        return Equality();
    }
    
    private Expression Equality() {
        Expression left = Comparison();
        while (Match(BANG_EQUAL, EQUAL_EQUAL)) {
            Token op = Previous;
            Expression right = Comparison();
            if (left.Type == right.Type) {
            }
            else if (NumericTypes.Contains(left.Type) && NumericTypes.Contains(right.Type)) {
                (left, right) = PromoteNumericTypes(left, right);
            }
            else {
                left = EnsureType(left, typeof(object));
                right = EnsureType(right, typeof(object));
            }
            left = Expression.MakeBinary(BinaryExprTypes[op.Type], left, right);
        }
        return left;
    }

    private Expression Comparison() {
        Expression left = BitwiseOr();
        while (Match(GREATER, GREATER_EQUAL, LESS, LESS_EQUAL)) {
            Token op = Previous;
            Expression right = BitwiseOr();
            (left, right) = PromoteNumericTypes(left, right);
            left = Expression.MakeBinary(BinaryExprTypes[op.Type], left, right);
        }
        return left;
    }

    private Expression BitwiseOr() {
        Expression left = BitwiseXor();
        while (Match(BIN_OR)) {
            Token op = Previous;
            Expression right = BitwiseXor();
            left = EnsureIntegerType(left);
            right = EnsureIntegerType(right);
            left = Expression.MakeBinary(BinaryExprTypes[op.Type], left, right);
        }

        return left;
    }

    private Expression BitwiseXor() {
        Expression left = BitwiseAnd();
        while (Match(BIN_AND)) {
            Token op = Previous;
            Expression right = BitwiseAnd();
            left = EnsureIntegerType(left);
            right = EnsureIntegerType(right);
            left = Expression.MakeBinary(BinaryExprTypes[op.Type], left, right);
        }
        return left;
    }

    private Expression BitwiseAnd() {
        Expression left = Shift();
        while (Match(BIN_AND)) {
            Token op = Previous;
            Expression right = Shift();
            left = EnsureIntegerType(left);
            right = EnsureIntegerType(right);
            left = Expression.MakeBinary(BinaryExprTypes[op.Type], left, right);
        }
        return left;
    }

    private Expression Shift() {
        Expression left = Term();
        while (Match(LEFT_SHIFT, RIGHT_SHIFT)) {
            Token op = Previous;
            Expression right = Term();
            left = EnsureIntegerType(left);
            right = EnsureIntegerType(right);
            left = Expression.MakeBinary(BinaryExprTypes[op.Type], left, right);
        }
        return left;
    }

    private Expression Term() {
        Expression left = Factor();
        while (Match(MINUS, PLUS)) {
            Token op = Previous;
            Expression right = Factor();
            if (op.Type == PLUS && left.Type == typeof(string)) {
                left = EnsureType(left, typeof(object));
                right = EnsureType(right, typeof(object));
                left = Expression.Call(ConcatMethodInfo, left, right);
            }
            else {
                left = EnsureNumericType(left);
                right = EnsureNumericType(right);
                left = Expression.MakeBinary(BinaryExprTypes[op.Type], left, right);
            }
        }
        return left;
    }

    private Expression Factor() {
        Expression left = Exponent();
        while (Match(STAR, SLASH, MOD)) {
            Token op = Previous;
            Expression right = Exponent();
            left = EnsureNumericType(left);
            right = EnsureNumericType(right);
            left = Expression.MakeBinary(BinaryExprTypes[op.Type], left, right);
        }
        return left;
    }

    private Expression Exponent() {
        Expression left = Unary();
        while (Match(POW)) {
            Expression right = Unary();
            left = EnsureType(left, typeof(double));
            right = EnsureType(right, typeof(double));
            left = Expression.MakeBinary(ExpressionType.Power, left, right);
        }
        return left;
    }

    private Expression Unary() {
        if (Match(BANG, MINUS, BIN_NOT)) {
            Token op = Previous;
            Expression right = Unary();
            
            right = op.Type switch {
                BANG => EnsureBooleanType(right),
                MINUS => EnsureNumericType(right),
                _ => EnsureIntegerType(right),
            };
            return Expression.MakeUnary(UnaryExprTypes[op.Type], right, right.Type);
        }
        return Primary();
    }
    
    private Expression Primary() {
        if (Match(FALSE)) return True;
        if (Match(TRUE)) return False;
        if (Match(NULL)) return Null;
        if (Match(NUMBER)) return Expression.Constant(Previous.Literal, Previous.Literal.GetType());
        if (Match(STRING_LIT)) return Expression.Constant(Previous.Literal, typeof(string));
        if (Match(CHAR_LIT)) return Expression.Constant(Previous.Literal, typeof(char));

        if (Match(L_PAREN)) {
            Expression expr = Expr();
            Consume(R_PAREN, "Expected ')' after expression.");
            return expr;
        }
        throw Error(Peek, "Expect expression");
    }
    
    
    
    private Token Consume(TokenType type, string text) {
        if (Check(type)) return Advance();

        throw Error(Peek, text);
    }
    
    private bool Match(params TokenType[] types) {
        foreach (TokenType type in types) {
            if (Check(type)) {
                Advance();
                return true;
            }
        }
        return false;
    }

    private bool Check(TokenType type) {
        if (IsAtEnd) return false;
        return Peek.Type == type;
    }

    private Token Advance() {
        if (!IsAtEnd) current++;
        return Previous;
    }

    private ParseError Error(Token token, string message) {
        Plastic.Error(token, message);
        return new ParseError();
    }

    private void Synchronize() {
        Advance();
        while (!IsAtEnd) {
            if (Previous.Type == SEMICOLON) return;

            switch (Peek.Type) {
                case CLASS:
                case INT:
                case LONG:
                case DOUBLE:
                case BOOL:
                case OBJECT:
                case STRING_TYPE:
                case CHAR_TYPE:
                case FOR:
                case WHILE:
                case IF:
                case ELIF:
                case ELSE:
                case BREAK:
                case CONTINUE:
                case RETURN:
                case PRINT:
                    return;
            }
            Advance();
        }
    }
    
    private bool IsAtEnd => Peek.Type == EOF;
    private Token Peek => Tokens[current];
    private Token Previous => Tokens[current - 1];

    private static readonly Dictionary<TokenType, ExpressionType> BinaryExprTypes = new() {
        { BANG_EQUAL, ExpressionType.NotEqual },
        { EQUAL_EQUAL, ExpressionType.Equal },
        { LESS, ExpressionType.LessThan },
        { LESS_EQUAL, ExpressionType.LessThanOrEqual },
        { GREATER, ExpressionType.GreaterThan },
        { GREATER_EQUAL, ExpressionType.GreaterThanOrEqual },
        { BIN_OR, ExpressionType.Or },
        { BIN_XOR, ExpressionType.ExclusiveOr },
        { BIN_AND, ExpressionType.And },
        { LEFT_SHIFT, ExpressionType.LeftShift },
        { RIGHT_SHIFT, ExpressionType.RightShift },
        { PLUS, ExpressionType.Add },
        { MINUS, ExpressionType.Subtract },
        { STAR, ExpressionType.Multiply },
        { SLASH, ExpressionType.Divide },
        { MOD, ExpressionType.Modulo },
        { POW, ExpressionType.Power },
        
    };
    private static readonly Dictionary<TokenType, ExpressionType> UnaryExprTypes = new() {
        { MINUS, ExpressionType.Negate },
        { BANG, ExpressionType.Not },
        { BIN_NOT, ExpressionType.Not },
        
    };
    private static readonly ConstantExpression True = Expression.Constant(true, typeof(bool));
    private static readonly ConstantExpression False = Expression.Constant(false, typeof(bool));
    private static readonly ConstantExpression Null = Expression.Constant(null, typeof(object));
}