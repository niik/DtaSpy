using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Reflection;
using System.Text.RegularExpressions;

namespace DtaSpy.Tests.Utils
{
    public class ResourceHelper
    {
        public static Stream LoadTestResource(string name)
        {
            var assembly = Assembly.GetExecutingAssembly();
            return assembly.GetManifestResourceStream("DtaSpy.Tests.Data." + FixResourceName(name));
        }

        private static string FixResourceName(string name)
        {
            name = name.Replace('/', '.');
            
            string substitute = name;

            do
            {
                name = substitute;
                substitute = Regex.Replace(name, @"\.(\d+)\.", m => "._" + m.Groups[1].Value + ".");
            } while (substitute != name);

            return name;
        }
    }
}
