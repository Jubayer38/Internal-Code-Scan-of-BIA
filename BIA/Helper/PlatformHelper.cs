using System;

namespace BIA.Helper;

public static class PlatformHelper
{
    public static string GetPlatformName() =>
        OperatingSystem.IsWindows() ? AppConstants.OS.Windows : AppConstants.OS.Linux;
}
