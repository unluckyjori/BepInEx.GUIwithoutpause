using System;

namespace Instances.Exceptions
{
    public class InstanceFileNotFoundException : InstanceException
    {
        private const string FileNotFound = "File not found: ";
        public InstanceFileNotFoundException(string fileName, Exception innerException) : base(FileNotFound + fileName, innerException) { }
    }
}