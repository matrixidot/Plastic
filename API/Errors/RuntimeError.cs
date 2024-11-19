namespace Plastic.API.Errors;

using Lexing;

public class RuntimeError(Token token, string message) : SystemException {
    public Token Token { get; } = token;

    public string Message { get; } = message;
}