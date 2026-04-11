using Microsoft.Win32;

namespace RefreshToAccess2.Services
{
    public static class RegistryService
    {
        private const string KeyPath = "HKEY_CURRENT_USER\\Software\\RefreshToAccess\\";

        public static void EnsureCreated()
        {
            Registry.CurrentUser.CreateSubKey("Software\\RefreshToAccess\\");
        }

        public static string Read(string key)
        {
            try
            {
                return Registry.GetValue(KeyPath, key, "")?.ToString() ?? "";
            }
            catch
            {
                return "";
            }
        }

        public static void Write(string key, string data)
        {
            Registry.SetValue(KeyPath, key, data);
        }
    }
}
