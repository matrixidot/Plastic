namespace Plastic.API.Lexing;

using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using static TokenType;

public class Lexer(string source) {
    private readonly List<Token> Tokens = new();

    private int Start;
    private int Current;
    private int Line = 1;

    private static readonly Dictionary<string, TokenType> Keywords = new() {
        ["if"] = IF,
        ["elif"] = ELIF,
        ["else"] = ELSE,
        ["while"] = WHILE,
        ["for"] = FOR,
        ["continue"] = CONTINUE,
        ["break"] = BREAK,
        ["return"] = RETURN,
        ["class"] = CLASS,
        ["super"] = SUPER,
        ["this"] = THIS,
        ["null"] = NULL,
        ["true"] = TRUE,
        ["false"] = FALSE,
        ["int"] = INT,
        ["long"] = LONG,
        ["double"] = DOUBLE,
        ["bool"] = BOOL,
        ["string"] = STRING_TYPE,
        ["char"] = CHAR_TYPE,
        ["object"] = OBJECT,
        ["print"] = PRINT,
    };

    private static readonly HashSet<string> ValidTypes = new() {
        "int", "long", "double", "string", "char", "object", "bool"
    };

    public List<Token> LexSource()
    {
        while (!IsAtEnd())
        {
            Start = Current;
            Lex();
        }
        Tokens.Add(new Token(EOF, "", null, Line));
        return Tokens;
    }

    private void Lex() {
        char c = Advance();
        switch (c) {
            case '(': HandleLeftParen(); break;
            case ')': AddToken(R_PAREN); break;
            case '{': AddToken(L_BRACE); break;
            case '}': AddToken(R_BRACE); break;
            case '[': AddToken(L_BRACKET); break;
            case ']': AddToken(R_BRACKET); break;
            case '~': AddToken(BIN_NOT); break;
            case '&': AddToken(Match('&') ? AND : BIN_AND); break;
            case '|': AddToken(Match('|') ? OR : BIN_OR); break;
            case '^': AddToken(BIN_XOR); break;
            case ',': AddToken(COMMA); break;
            case ';': AddToken(SEMICOLON); break;
            case '"': StringLiteral(); break;
            case '\'': CharLiteral(); break;
            case ' ':
            case '\r':
            case '\t':
                break;
            case '\n':
                Line++;
                break;
            case '.':
                if (char.IsDigit(Peek()))
                    Number();
                else
                    AddToken(DOT);
                break;
            case '+':
                AddToken(Match('+') ? INCREMENT : Match('=') ? PLUS_EQUAL : PLUS);
                break;
            case '-':
                AddToken(Match('-') ? DECREMENT : Match('=') ? MINUS_EQUAL : MINUS);
                break;
            case '*':
                if (Match('*'))
                    AddToken(Match('=') ? POW_EQUAL : POW);
                else
                    AddToken(Match('=') ? STAR_EQUAL : STAR);
                break;
            case '/':
                if (Match('/')) {
                    while (Peek() != '\n' && !IsAtEnd()) Advance();
                }
                else {
                    AddToken(Match('=') ? SLASH_EQUAL : SLASH);
                }
                break;
            case '%':
                AddToken(Match('=') ? MOD_EQUAL : MOD);
                break;
            case '>':
                if (Match('='))
                    AddToken(GREATER_EQUAL);
                else if (Match('>'))
                    AddToken(RIGHT_SHIFT);
                else
                    AddToken(GREATER);
                break;
            case '<':
                if (Match('='))
                    AddToken(LESS_EQUAL);
                else if (Match('<'))
                    AddToken(LEFT_SHIFT);
                else
                    AddToken(LESS);
                break;
            case '!':
                AddToken(Match('=') ? BANG_EQUAL : BANG);
                break;
            case '=':
                AddToken(Match('=') ? EQUAL_EQUAL : EQUAL);
                break;
            default:
                if (char.IsDigit(c))
                    Number();
                else if (char.IsLetter(c) || c == '_')
                    Identifier();
                else
                    Plastic.Error(Line, $"Unexpected character '{c}'");
                break;
        }
    }

    private bool Match(char expected) {
        if (IsAtEnd() || source[Current] != expected) return false;
        Current++;
        return true;
    }

    private bool Match(string expected) {
        if (Current + expected.Length > source.Length) return false;
        if (source.Substring(Current, expected.Length) != expected) return false;
        Current += expected.Length;
        return true;
    }

    private void Identifier() {
        while (char.IsLetterOrDigit(Peek()) || Peek() == '_') Advance();
        string text = source.Substring(Start, Current - Start);
        AddToken(Keywords.GetValueOrDefault(text, IDENTIFIER), text);
    }

    private void Number() {
        while (char.IsDigit(Peek())) Advance();

        if (Peek() == '.' && char.IsDigit(PeekNext())) {
            Advance(); // Consume '.'
            while (char.IsDigit(Peek())) Advance();
        }

        string numberStr = source.Substring(Start, Current - Start);
        if (int.TryParse(numberStr, out int i)) {
            AddToken(NUMBER, i);
        } else if (long.TryParse(numberStr, out long l)) {
            AddToken(NUMBER, l);
        } else if (double.TryParse(numberStr, out double d)) {
            AddToken(NUMBER, d);
        }
        else {
            Plastic.Error(Line, $"Invalid number format: {numberStr}");
        }
    }

    private void HandleLeftParen() {
        int tempCurrent = Current;

        while (!IsAtEnd() && (char.IsLetterOrDigit(Peek()) || Peek() == '_')) {
            Advance();
        }

        if (!IsAtEnd() && Peek() == ')') {
            string typeName = source.Substring(tempCurrent, Current - tempCurrent).Trim();

            if (IsType(typeName)) {
                Advance(); // Consume ')'
                AddToken(TYPECAST, typeName);
                return;
            }
        }

        // Not a type cast, reset Current to the character after '('
        Current = tempCurrent;
        AddToken(L_PAREN);
    }


    private bool IsType(string identifier) {
        return ValidTypes.Contains(identifier) || IsUserDefinedType(identifier);
    }

    private bool IsUserDefinedType(string typeName) {
        return Type.GetType(typeName) != null;
    }

    private void StringLiteral() {
        while (!IsAtEnd()) {
            if (Peek() == '"' && source[Current - 1] != '\\') {
                break;
            }

            if (Peek() == '\n') Line++;
            Advance();
        }

        if (IsAtEnd()) {
            Plastic.Error(Line, "Unterminated string literal.");
            return;
        }

        Advance(); // Consume closing '"'
        string value = Regex.Unescape(source.Substring(Start + 1, Current - Start - 2));
        AddToken(STRING_LIT, value);
    }

    private void CharLiteral() {
        if (IsAtEnd()) {
            Plastic.Error(Line, "Unterminated character literal.");
            return;
        }

        char value;
        if (Peek() == '\\') {
            Advance(); // Consume '\'

            if (IsAtEnd()) {
                Plastic.Error(Line, "Unterminated escape sequence in character literal.");
                return;
            }

            value = Advance() switch {
                't' => '\t',
                'n' => '\n',
                'r' => '\r',
                '0' => '\0',
                '\'' => '\'',
                '\\' => '\\',
                _ => throw new Exception("Invalid escape sequence in character literal.")
            };
        }
        else {
            value = Advance();
        }

        if (IsAtEnd() || Advance() != '\'') {
            Plastic.Error(Line, "Unterminated character literal.");
            return;
        }

        AddToken(CHAR_LIT, value);
    }

    private void AddToken(TokenType type, object literal = null) {
        string text = source.Substring(Start, Current - Start);
        Tokens.Add(new Token(type, text, literal, Line));
    }

    private char Advance() => source[Current++];

    private bool IsAtEnd() => Current >= source.Length;

    private char Peek() => IsAtEnd() ? '\0' : source[Current];

    private char PeekNext() => Current + 1 >= source.Length ? '\0' : source[Current + 1];
}