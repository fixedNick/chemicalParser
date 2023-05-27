﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace chemicalParser.Chemicals;

internal class ChemicalInfo
{
    public int Id { get; private set; }
    public string RuName { get; private set; }
    public string EnName { get; private set; }
    public string Formula { get; private set; }
    public string InChiKey { get; private set; }
    public string Cas { get; private set; }
}