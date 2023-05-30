using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace chemicalParser.JDX;
internal class JdxPair
{
    public string Key { get; private set; }
    public string Value { get; private set; }

    public JdxPair(string key, string val)
    {
        Key = key;
        Value = val;
    }
}
