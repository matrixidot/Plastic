namespace Plastic;

public static class REPL {
    public static void Start() {
        Console.WriteLine("Plastic REPL. Type '.help' for commands, '.exit' to quit.");
        var buffer = new List<string>();
        int openBraces = 0;

        while (true)
        {
            Console.Write(buffer.Count == 0 ? "Th> " : "... ");
            string? line = Console.ReadLine();

            if (string.IsNullOrEmpty(line)) {
                continue;
            }
            if (line.Trim().StartsWith(".run ")) {
                string path = line.Substring(5).Trim();
                ScriptRunner.Run(path);
            }
            else switch (line.Trim()) {
                case ".exit": return;
                case ".clear": Console.Clear(); break;
                case ".help": PrintHelp(); break;
                case ".timer":
                    Plastic.timing = !Plastic.timing;
                    Console.WriteLine($"Timer is now {(Plastic.timing ? "on" : "off")}");
                    break;
                default: {
                    buffer.Add(line);
                    openBraces += CountUnmatchedOpenBraces(line);

                    if (openBraces > 0) continue;
                    string input = string.Join("\n", buffer);
                    buffer.Clear();
                    openBraces = 0;
                    Plastic.Run($"{input}");
                    break;
                }
            }
        }
    }

    private static int CountUnmatchedOpenBraces(string line)
    {
        int count = 0;
        foreach (char c in line) {
            switch (c) {
                case '{' or '(' or '[': count++;
                    break;
                case '}' or ')' or ']': count--;
                    break;
            }
        }
        return count;
    }

    private static void PrintHelp() {
        Console.WriteLine(@"Commands:
                .help         Displays this help text.
                .clear        Clears the console.
                .run <path>   Runs a script in a new window.
                .exit         Exits the REPL.
            Multiline statements are supported. Type until all open braces '{', '(', '[' are closed.");
    }
}