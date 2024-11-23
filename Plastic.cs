namespace Plastic;

using System.Diagnostics;
using System.Linq.Expressions;
using API.Errors;
using API.Lexing;
using API.Parsing;
using static API.Lexing.TokenType;

public static class Plastic {
    private static double timer;
    public static bool timing;
    private static bool HadError;
    private static bool HadRuntimeError;
    
    public static void Main(string[] args)
    {
        if (args.Length >= 1) {
            timing = bool.Parse(args[1]);
            string source = File.ReadAllText(args[0]);
            Run(source);
            Console.WriteLine("\nCompleted... Press any key to exit.");
            Console.ReadKey();
        }
        else {
            REPL.Start();
        }
    }
    
    public static void Run(string source) {
        InitTimer();
        Lexer lexer = new Lexer(source);
        List<Token> tokens = lexer.LexSource();
        Time("Lexing");
        Parser parser = new Parser(tokens);
        BlockExpression ToExecute = parser.Parse();
        Time("Parsing");
        if (HadError) return;
        Expression<Action> lambda = Expression.Lambda<Action>(ToExecute);
        Action compiled = lambda.Compile();
        Time("Compiled");
        compiled();
        Time("Execution");
    }
    
    
    public static void RuntimeError(RuntimeError error) {
        Console.Error.WriteLine($"[Line {error.Token.Line}]\n{error.Message}");
        HadRuntimeError = true;
    }
    public static void Error(int line, string message) => Report(line, "", message);
    public static void Error(Token token, string message) => Report(token.Line, token.Type == EOF ? "at end" : $"at '{token.Lexeme}'", message);
    private static void Report(int line, string where, string message) {
        Console.Error.WriteLine($"Error [Line {line}]: {(where.Length > 0 ? $" {where}" : "")}: {message}");
        HadError = true;
    }
    
    private static void Time(string process) {
        if (!timing) return;
        Console.WriteLine($"[{process}]: Completed in {(double) DateTime.Now.Ticks / TimeSpan.TicksPerMillisecond / 1000.0 - timer} seconds.");
        timer = (double) DateTime.Now.Ticks / TimeSpan.TicksPerMillisecond / 1000.0;
    }

    private static void InitTimer() {
        timer = (double) DateTime.Now.Ticks / TimeSpan.TicksPerMillisecond / 1000.0;
    }
}