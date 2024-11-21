using System.Reflection;

namespace Plastic.API.Parsing;

using System.Linq.Expressions;
using Errors;
using Lexing;
using static Lexing.TokenType;
using static Typing;

public class Parser(List<Token> Tokens) {
    private int current = 0;

    // Symbol table to store global variables
    private static readonly Dictionary<string, Variable> globalVariables = new();

    // === Core Parsing Methods ===

    /// <summary>
    /// Entry point for parsing.
    /// </summary>
    public BlockExpression Parse() {
        List<Expression> statements = new();
        while (!IsAtEnd) {
            statements.Add(Declaration());
        }
        return Expression.Block(statements);
    }

    /// <summary>
    /// Handles declarations of variables and other statements.
    /// </summary>
    private Expression Declaration() {
        try {
            if (MatchType(out Type type)) {
                return VarDeclaration(type);
            }
            return Statement();
        }
        catch (ParseError error) {
            Synchronize();
            return Expression.Empty();
        }
    }

    /// <summary>
    /// Handles variable declarations.
    /// </summary>
    private Expression VarDeclaration(Type type) {
        Token name = Consume(IDENTIFIER, "Expect variable name.");
        Expression initializer = null;

        if (Match(EQUAL)) {
            initializer = Expr();
            initializer = EnsureType(initializer, type);
        }

        Consume(SEMICOLON, "Expect ';' after variable declaration.");

        ParameterExpression variableExpr = Expression.Variable(type, name.Lexeme);

        if (!globalVariables.TryAdd(name.Lexeme, new(name.Lexeme, type, variableExpr, null))) {
            throw Error(name, $"Variable '{name.Lexeme}' is already declared.");
        }

        return initializer == null
            ? variableExpr
            : Expression.Block(new[] { variableExpr }, Expression.Assign(variableExpr, initializer));
    }

    /// <summary>
    /// Handles statements such as print or assignments.
    /// </summary>
    private Expression Statement() {
        if (Match(PRINT)) return PrintStatement();
        if (Match(IDENTIFIER)) return VariableOrAssignment(Previous);
        return ExpressionStatement();
    }

    /// <summary>
    /// Handles assignments or variable access.
    /// </summary>
    private Expression VariableOrAssignment(Token identifier) {
        if (!globalVariables.TryGetValue(identifier.Lexeme, out Variable variable)) {
            throw Error(identifier, $"Variable '{identifier.Lexeme}' is not defined.");
        }

        if (Match(EQUAL)) {
            Expression value = Expr();
            value = EnsureType(value, variable.Type);

            globalVariables[identifier.Lexeme] = variable with { Value = value };

            return Expression.Assign(variable.Expr, value);
        }

        return variable.Expr;
    }

    /// <summary>
    /// Handles print statements.
    /// </summary>
    private Expression PrintStatement() {
        Expression value = Expr();
        Consume(SEMICOLON, "Expect ';' after value.");
        value = EnsureType(value, typeof(object));
        return Expression.Call(
            typeof(Console).GetMethod("WriteLine", new[] { typeof(object) }),
            value
        );
    }

    /// <summary>
    /// Handles general expressions as statements.
    /// </summary>
    private Expression ExpressionStatement() {
        Expression value = Expr();
        Consume(SEMICOLON, "Expect ';' after expression.");
        return value;
    }

    // === Expression Parsing ===

    private Expression Expr() => Assignment();

    private Expression Assignment() {
        Expression expr = Equality(); // Start with the equality expression.

        if (Match(EQUAL, PLUS_EQUAL, MINUS_EQUAL, STAR_EQUAL, SLASH_EQUAL, MOD_EQUAL, POW_EQUAL)) {
            Token op = Previous;
            Expression value = Assignment(); // Recursively parse the right-hand side of the assignment.

            if (expr is ParameterExpression variableExpr) {
                // Ensure type compatibility
                value = EnsureType(value, variableExpr.Type);

                return op.Type switch {
                    EQUAL => Expression.Assign(variableExpr, value),
                    PLUS_EQUAL => Expression.AddAssign(variableExpr, value),
                    MINUS_EQUAL => Expression.SubtractAssign(variableExpr, value),
                    STAR_EQUAL => Expression.MultiplyAssign(variableExpr, value),
                    SLASH_EQUAL => Expression.DivideAssign(variableExpr, value),
                    MOD_EQUAL => Expression.ModuloAssign(variableExpr, value),
                    POW_EQUAL => Expression.PowerAssign(variableExpr, value), // Custom implementation for **=
                    _ => throw Error(op, "Unsupported assignment operator.")
                };
            }

            throw Error(op, "Invalid assignment target.");
        }

        return expr;
    }



    private Expression Equality() {
        Expression left = Comparison();
        while (Match(BANG_EQUAL, EQUAL_EQUAL)) {
            Token op = Previous;
            Expression right = Comparison();

            if (left.Type != right.Type) {
                if (NumericTypes.Contains(left.Type) && NumericTypes.Contains(right.Type)) {
                    (left, right) = PromoteNumericTypes(left, right);
                } else {
                    left = EnsureType(left, typeof(object));
                    right = EnsureType(right, typeof(object));
                }
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
            } else {
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
    // Handle literals
        if (Match(FALSE)) return False;
        if (Match(TRUE)) return True;
        if (Match(NULL)) return Null;
        if (Match(NUMBER)) return Expression.Constant(Previous.Literal, Previous.Literal.GetType());
        if (Match(STRING_LIT)) return Expression.Constant(Previous.Literal, typeof(string));
        if (Match(CHAR_LIT)) return Expression.Constant(Previous.Literal, typeof(char));

        // Handle prefix increment (++) and decrement (--)
        if (Match(INCREMENT, DECREMENT)) {
            Token op = Previous; // Save the operator (++ or --)
            if (Match(IDENTIFIER)) {
                string name = Previous.Lexeme;
                if (globalVariables.TryGetValue(name, out Variable variable)) {
                    ParameterExpression variableExpr = variable.Expr;
                    return op.Type == INCREMENT
                        ? Expression.PreIncrementAssign(variableExpr)
                        : Expression.PreDecrementAssign(variableExpr);
                }
                throw Error(Previous, $"Undefined variable '{name}'.");
            }
            throw Error(Peek, "Expected variable after prefix operator.");
        }
        if (Match(IDENTIFIER)) {
            string name = Previous.Lexeme;

            if (globalVariables.TryGetValue(name, out Variable variable)) {
                ParameterExpression variableExpr = variable.Expr;
                if (Match(INCREMENT)) {
                    return Expression.PostIncrementAssign(variableExpr);
                }
                if (Match(DECREMENT)) {
                    return Expression.PostDecrementAssign(variableExpr);
                }
                return variableExpr;
            }

            throw Error(Previous, $"Undefined variable '{name}'.");
        }

        // Handle grouped expressions
        if (Match(L_PAREN)) {
            Expression expr = Expr();
            Consume(R_PAREN, "Expected ')' after expression.");
            return expr;
        }

        throw Error(Peek, "Expect expression.");
    }


    // === Utility and Helper Methods ===

    private bool Match(params TokenType[] types) {
        foreach (TokenType type in types) {
            if (Check(type)) {
                Advance();
                return true;
            }
        }
        return false;
    }

    private bool Check(TokenType type) => !IsAtEnd && Peek.Type == type;

    private Token Advance() {
        if (!IsAtEnd) current++;
        return Previous;
    }

    private Token Consume(TokenType type, string message) {
        if (Check(type)) return Advance();
        throw Error(Peek, message);
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

    private bool MatchType(out Type type) {
        if (Match(INT)) { type = typeof(int); return true; }
        if (Match(LONG)) { type = typeof(long); return true; }
        if (Match(DOUBLE)) { type = typeof(double); return true; }
        if (Match(STRING_TYPE)) { type = typeof(string); return true; }
        if (Match(CHAR_TYPE)) { type = typeof(char); return true; }
        if (Match(BOOL)) { type = typeof(bool); return true; }
        if (Match(OBJECT)) { type = typeof(object); return true; }
        if (Match(IDENTIFIER)) {
            type = ResolveUserDefinedType(Previous.Lexeme);
            return true;
        }
        type = null;
        return false;
    }

    private Type ResolveUserDefinedType(string typeName) {
        Type? type = Type.GetType(typeName);
        if (type == null) {
            throw Error(Previous, $"{typeName} is not a defined type.");
        }

        return type;
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