using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace chemicalParser.JDX
{
    internal class ValuesNotFoundException : Exception
    {
        public ValuesNotFoundException() : base("Min/Max values not found") { }
    }
}
