namespace Plastic.API.Parsing;

using System.Reflection;

public static partial class Typing {
    public static readonly MethodInfo ConcatMethodInfo = 
        typeof(string).GetMethod("Concat", new[] { typeof(object), typeof(object) })
        ?? throw new InvalidOperationException("String.Concat method not found");
    
    public static readonly MethodInfo writeLine = 
        typeof(Console).GetMethod("WriteLine", new[] { typeof(object) })
        ?? throw new InvalidOperationException("Console.WriteLine method not found");
    
    public static readonly HashSet<Type> NumericTypes = [typeof(int), typeof(long), typeof(double)];
}