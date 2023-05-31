using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace chemicalParser.Exceptions;

internal class BadInputException : Exception
{
	public BadInputException() : base("Ошибка при вводе данных в форму"){}
    public BadInputException(string message) : base(message) { }
}
