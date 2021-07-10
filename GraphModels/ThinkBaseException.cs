using System;
using System.Collections.Generic;
using System.Text;

namespace ThinkBase.Client.GraphModels
{
    public class ThinkBaseException : Exception
    {
        public ThinkBaseException()
        {
        }

        public ThinkBaseException(string message)
            : base(message)
        {
        }

        public ThinkBaseException(string message, Exception inner)
            : base(message, inner)
        {
        }
    }
}
