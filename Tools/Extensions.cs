namespace Thorium.Tools;

public static class Extensions {
    public static string Stringify<T>(this List<T> list, string joiner) {
        return $"[{string.Join(joiner, list)}]";
    }
    
    public static string Stringify<T>(this T[] arr, string joiner) {
        return $"[{string.Join(joiner, arr)}]";
    }
}