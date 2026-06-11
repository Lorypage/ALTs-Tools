namespace RefreshToAccess2.Localization
{
    /// <summary>
    /// Convenience accessor for localized strings from C# code.
    /// </summary>
    public static class Loc
    {
        /// <summary>Returns the localized string for <paramref name="key"/>.</summary>
        public static string T(string key) => LocalizationManager.Instance[key];

        /// <summary>
        /// Returns the localized format string for <paramref name="key"/>
        /// with <paramref name="args"/> substituted via <see cref="string.Format(string, object[])"/>.
        /// </summary>
        public static string T(string key, params object[] args)
            => string.Format(LocalizationManager.Instance[key], args);
    }
}
