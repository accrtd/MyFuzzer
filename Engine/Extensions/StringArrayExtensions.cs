namespace Engine.Extensions;

internal static class StringArrayExtensions
{
    public static bool ValidateIfNotEmpty(this string[] str, string errMsg)
    {
        if (str.Length == 0)
        {
            Console.WriteLine(errMsg);
            return true;
        }
        else
            return false;
    }
}
