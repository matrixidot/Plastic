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

    public static readonly Dictionary<(Type, Type), Type> NumericPromotionRules = new() {
        { (typeof(int), typeof(int)), typeof(int) },
        { (typeof(int), typeof(long)), typeof(long) },
        { (typeof(long), typeof(int)), typeof(long) },
        { (typeof(long), typeof(long)), typeof(long) },

        { (typeof(int), typeof(double)), typeof(double) },
        { (typeof(double), typeof(int)), typeof(double) },
        { (typeof(long), typeof(double)), typeof(double) },
        { (typeof(double), typeof(long)), typeof(double) },
        { (typeof(double), typeof(double)), typeof(double) },
    };

    public static readonly Type[] PrimitiveTypes = new Type[] {
        typeof(int),
        typeof(long),
        typeof(double),
        typeof(string),
        typeof(char),
        typeof(bool),
    };
}