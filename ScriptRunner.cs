namespace Plastic;

using System.Diagnostics;

public static class ScriptRunner {
    public static void Run(string path) {
        if (!File.Exists(path)) {
            Console.WriteLine($"Error: Could not find file '{path}'");
            return;
        }

        try {
            ProcessStartInfo startInfo = new() {
                FileName = Environment.ProcessPath,
                ArgumentList = { $"{path}", $"{Plastic.timing}" },
                UseShellExecute = true,
                WindowStyle = ProcessWindowStyle.Normal,
            };

            Process.Start(startInfo);
        }
        catch (Exception ex) {
            Console.WriteLine($"Error launching script: {ex.Message}");
        }
    }
}