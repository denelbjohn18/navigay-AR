using System;

namespace Navigay.Data
{
    public sealed class GraphLoadException : Exception
    {
        public GraphLoadException(string message) : base(message)
        {
        }

        public GraphLoadException(string message, Exception inner) : base(message, inner)
        {
        }
    }
}
