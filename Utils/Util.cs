using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace chemicalParser.Utils;

internal class Util
{
    public static string GetFreeFileName(string downloadsDirectory, string name, string extension = ".jdx", bool returnPath = false)
    {
        var files = Directory.GetFiles(downloadsDirectory);
        int counter = 0;
        while (true)
        {
            var fileName = $"{name}_{counter}{extension}";
            if (File.Exists($"{downloadsDirectory}/{fileName}") == true)
                counter++;
            else
            {
                if(returnPath) return $"{downloadsDirectory}/{fileName}";
                return fileName;
            }
        }
    }
}
