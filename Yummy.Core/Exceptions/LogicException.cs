using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Yummy.Core.Exceptions
{
    public class LogicException : Exception
    {
        public string PropertyName { get; }

        public LogicException(string propertyName, string message) : base(message)
        {
            PropertyName = propertyName;
        }
    }
}
