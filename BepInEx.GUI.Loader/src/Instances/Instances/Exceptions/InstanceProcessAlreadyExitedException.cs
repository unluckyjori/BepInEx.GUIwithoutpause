using System;

namespace Instances.Exceptions
{
    public class InstanceProcessAlreadyExitedException : Exception
    {
        private const string ProcessAlreadyExited = "The process instance has already exited";

        public InstanceProcessAlreadyExitedException() : base(ProcessAlreadyExited) { }

        public InstanceProcessAlreadyExitedException(Exception innerException) : base(ProcessAlreadyExited, innerException) { }
    }
}