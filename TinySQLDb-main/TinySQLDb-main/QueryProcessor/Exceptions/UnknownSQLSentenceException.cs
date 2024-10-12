using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace QueryProcessor.Exceptions
{
    public class UnknownSQLSentenceException : Exception
    {
        public UnknownSQLSentenceException() : base() { }
        public UnknownSQLSentenceException(string message) : base(message) { }
    }
}
