namespace Plastic.API.Lexing;

public record Token(TokenType Type, string Lexeme, object Literal, int Line) {
    public override string ToString() {
        return $"[{Type}, {Literal}]";
    }
}