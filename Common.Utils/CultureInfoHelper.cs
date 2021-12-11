using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

namespace Common.Utils
{
    public static class CultureInfoHelper
    {
        private static readonly HashSet<string> _cultureNames = CreateCultureNames();

        public static HashSet<string> CultureNames => _cultureNames;

        public static bool Exists(string name)
        {
            return _cultureNames.Contains(name);
        }

        private static HashSet<string> CreateCultureNames()
        {
            var cultureInfos = CultureInfo.GetCultures(CultureTypes.AllCultures)
                                          .Where(x => !string.IsNullOrEmpty(x.Name))
                                          .ToArray();
            var allNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            allNames.UnionWith(cultureInfos.Select(x => x.TwoLetterISOLanguageName));
            allNames.UnionWith(cultureInfos.Select(x => x.Name));
            return allNames;
        }
    }
}