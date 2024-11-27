using System.Security.Principal;

namespace WinActionWeaverTests.TestHelpers;

public static class AdminPrivilegeHelper
{
    public static bool IsRunningAsAdmin()
    {
        using var identity = WindowsIdentity.GetCurrent();
        var principal = new WindowsPrincipal(identity);
        return principal.IsInRole(WindowsBuiltInRole.Administrator);
    }
} 