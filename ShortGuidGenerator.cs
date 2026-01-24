using System;
using System.Text;

public static class ShortGuidGenerator        // created static class ShortGuidGenerator
{                                             // Has no state (no variables stored), Just performs one small utility task
                                              //  Does not depend on object data

    private const string Characters = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789";

    public static string Generate(int length = 6)
    {
        if (length <= 0)
            throw new ArgumentException("Length must be greater than zero.", nameof(length));

        var stringBuilder = new StringBuilder(length);
        var random = new Random();

        for (int i = 0; i < length; i++)
        {
            stringBuilder.Append(Characters[random.Next(Characters.Length)]);
        }
        return stringBuilder.ToString();
    }
}
