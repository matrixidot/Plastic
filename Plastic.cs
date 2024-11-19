namespace Plastic;

using System.Diagnostics;
using System.Linq.Expressions;
using API.Errors;
using API.Lexing;
using API.Parsing;
using static API.Lexing.TokenType;

public static class Plastic {
    private static double timer;
    private static bool timing;
    private static bool HadError;
    private static bool HadRuntimeError;
    
    public static void Main(string[] args) {
        switch (args.Length) {
            case 1 when args[0].ToLower() == "quit":
                Console.WriteLine("Quitting Plastic...");
                return;
            case >= 1: {
                timing = bool.Parse(args[1]);
                string source = File.ReadAllText(args[0]);
                try {
                    Run(source);
                    Console.WriteLine("\nCompleted... Press enter to exit.");
                    Console.ReadLine();
                }
                catch (Exception e) {
                    Console.WriteLine("Script Running failed...\n");
                    Console.WriteLine(e.Message);
                    Console.WriteLine(e.StackTrace);
                    Console.WriteLine("\nPress enter to exit.");
                    Console.ReadLine();
                }
                break;
            }
            default: {
                Console.Write("Enter script path to run: ");
                string path = Console.ReadLine();
                if (path.ToLower() == ".timer") {
                    timing = !timing;
                    Console.WriteLine($"Timing: {timing}");
                    Console.Write("Enter script path to run: ");
                    path = Console.ReadLine();
                }
                
                if (!Path.Exists(path)) {
                    Console.WriteLine($"Error: Could not find file '{path}'");
                    Main(args);
                }
                try {
                    ProcessStartInfo startInfo = new() {
                        FileName = Environment.ProcessPath,
                        ArgumentList = { $"{path}", $"{timing}" },
                        UseShellExecute = true,
                        WindowStyle = ProcessWindowStyle.Normal,
                    };

                    Process.Start(startInfo);
                    Main(args);
                }
                catch (Exception ex) {
                    Console.WriteLine($"Error launching script: {ex.Message}");
                }
                break;
            }
        }
    }
    
    private static void Run(string source) {
        InitTimer();
        Lexer lexer = new Lexer(source);
        List<Token> tokens = lexer.LexSource();
        Time("Lexing");
        Parser parser = new Parser(tokens);
        Expression ToExecute = parser.Parse();
        Time("Parsing");
        if (HadError) return;

        if (ToExecute.Type == typeof(void)) {
            Expression<Action> lambda = Expression.Lambda<Action>(ToExecute);
            Action compiled = lambda.Compile();
            Time("Compiled");
            compiled();
        }
        else {
            Expression<Func<object>> lambda = Expression.Lambda<Func<object>>(Expression.Convert(ToExecute, typeof(object)));
            Func<object> compiled = lambda.Compile();
            Time("Compilation");
            object result = compiled();
            Console.WriteLine(result);
        }
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