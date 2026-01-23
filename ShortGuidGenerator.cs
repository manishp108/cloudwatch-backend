using System;
using System.Text;

public static class ShortGuidGenerator      // created static class ShortGuidGenerator
{                                           // Has no state (no variables stored), Just performs one small utility task

                                             //  Does not depend on object data
    public static string Generate()
    {
        return Guid.NewGuid()
                   .ToString("N")
                   .Substring(0, 12);
    }
}
