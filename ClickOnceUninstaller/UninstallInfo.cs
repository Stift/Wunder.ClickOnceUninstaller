using System;
using System.Collections.Generic;
using Microsoft.Win32;

namespace Wunder.ClickOnceUninstaller
{
    public class UninstallInfo
    {
        public const string UninstallRegistryPath = @"Software\Microsoft\Windows\CurrentVersion\Uninstall";

        private UninstallInfo()
        {
        }

        public static UninstallInfo Find(string appName)
        {
            var uninstall = Registry.CurrentUser.OpenSubKey(UninstallRegistryPath);
            if (uninstall != null)
            {
                foreach (var app in uninstall.GetSubKeyNames())
                {
                    var sub = uninstall.OpenSubKey(app);
                    if (sub != null && sub.GetValue("DisplayName") as string == appName)
                    {
                        return new UninstallInfo
                                   {
                                       Key = app,
                                       UninstallString = sub.GetValue("UninstallString") as string,
                                       ShortcutFolderName = sub.GetValue("ShortcutFolderName") as string,
                                       ShortcutSuiteName = sub.GetValue("ShortcutSuiteName") as string,
                                       ShortcutFileName = sub.GetValue("ShortcutFileName") as string,
                                       SupportShortcutFileName = sub.GetValue("SupportShortcutFileName") as string,
                                       Version = sub.GetValue("DisplayVersion") as string
                                   };
                    }
                }
            }

            return null;
        }

        public string Key { get; set; }

        public string UninstallString { get; private set; }

        public string ShortcutFolderName { get; set; }

        public string ShortcutSuiteName { get; set; }

        public string ShortcutFileName { get; set; }

        public string SupportShortcutFileName { get; set; }

        public string Version { get; set; }

        public string GetPublicKeyToken()
        {
            var enumerator = WhereTrimStartsWith(UninstallString.Split(','), "PublicKeyToken=").GetEnumerator();
            enumerator.MoveNext();

            var token = enumerator.Current.Substring(16);
            if (token.Length != 16) throw new ArgumentException();
            return token;
        }

        private IEnumerable<string> WhereTrimStartsWith(IEnumerable<string> getValueNames, string implication)
        {
            foreach (var valueName in getValueNames)
            {
                if (valueName.Trim().StartsWith(implication))
                    yield return valueName;
            }
        }

    }
}
