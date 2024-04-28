using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Instances
{
    public interface IProcessInstance : IDisposable
    {
        //Task SendErrorDataAsync(string input);
        //void SendErrorData(string input);

        Task SendInputDataAsync(string input);

        void SendInputData(string input);

        public Task<IProcessResult> WaitForExitAsync(CancellationToken cancellationToken = default);
        public IProcessResult WaitForExit();

        IProcessResult Kill();

        IReadOnlyCollection<string> OutputData { get; }
        IReadOnlyCollection<string> ErrorData { get; }

        event EventHandler<IProcessResult>? Exited;
        event EventHandler<string>? OutputDataReceived;
        event EventHandler<string>? ErrorDataReceived;
    }
}