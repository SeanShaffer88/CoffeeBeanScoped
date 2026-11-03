using Microsoft.Win32;

namespace CoffeeBean.Utils;

 internal static class AutoStartup
 {
     private const string RunKeyPath = @"HKEY_CURRENT_USER\Software\Microsoft\Windows\CurrentVersion\Run";
     private const string ValueName = "CoffeeBeanScoped";
     private const string LegacyValueName = "CoffeeBean";

     public static bool IsEnabled =>
         (string)Registry.GetValue(RunKeyPath, ValueName, null) == AssemblyInfoRetriever.ExecutableFullName;

     public static void Enable()
     {
         Registry.SetValue(RunKeyPath, ValueName, AssemblyInfoRetriever.ExecutableFullName);
         DeleteLegacyValue();
     }

     public static void Disable()
     {
         using var key = Registry.CurrentUser.OpenSubKey(@"Software\Microsoft\Windows\CurrentVersion\Run", true);
         key?.DeleteValue(ValueName, false);
         key?.DeleteValue(LegacyValueName, false);
     }

     private static void DeleteLegacyValue()
     {
         using var key = Registry.CurrentUser.OpenSubKey(@"Software\Microsoft\Windows\CurrentVersion\Run", true);
         key?.DeleteValue(LegacyValueName, false);
     }
 }
