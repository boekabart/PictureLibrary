using System;
using System.IO;

namespace SmartMirror
{
    public class EnrichedIOException : IOException
    {
        public EnrichedIOException(string message, Exception innerException)
            : base(message, innerException)
        {
        }
    }
}