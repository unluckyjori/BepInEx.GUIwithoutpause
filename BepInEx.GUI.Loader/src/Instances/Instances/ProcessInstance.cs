using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Instances.Exceptions;

namespace Instances
{
    public class ProcessInstance : IProcessInstance
    {
        private readonly bool _ignoreEmptyLines;
        private readonly int _dataBufferCapacity;
        private readonly Process _process;
        private readonly TaskCompletionSource<bool> _mainTask = new TaskCompletionSource<bool>();
        private readonly TaskCompletionSource<bool> _stdoutTask = new TaskCompletionSource<bool>();
        private readonly TaskCompletionSource<bool> _stderrTask = new TaskCompletionSource<bool>();
        private readonly Queue<string> _outputData = new Queue<string>();
        private readonly Queue<string> _errorData = new Queue<string>();

        //private StreamWriter? StandardErrorInput = null;

        internal ProcessInstance(Process process, bool ignoreEmptyLines, int dataBufferCapacity)
        {
            process.OutputDataReceived += ReceiveOutput;
            process.ErrorDataReceived += ReceiveError;
            process.Exited += ReceiveExit;

            _process = process;
            _ignoreEmptyLines = ignoreEmptyLines;
            _dataBufferCapacity = dataBufferCapacity;
        }
        public Process GetProcess()
        {
            return _process;
        }
        public IReadOnlyCollection<string> OutputData => _outputData.ToList().AsReadOnly();
        public IReadOnlyCollection<string> ErrorData => _errorData.ToList().AsReadOnly();

        public event EventHandler<IProcessResult>? Exited;
        public event EventHandler<string>? OutputDataReceived;
        public event EventHandler<string>? ErrorDataReceived;

        public async Task SendInputDataAsync(string input)
        {
            if (_process.HasExited)
                throw new InstanceProcessAlreadyExitedException();

            await _process.StandardInput.WriteAsync(input).ConfigureAwait(false);
            await _process.StandardInput.FlushAsync().ConfigureAwait(false);
        }
        public void SendInputData(string input)
        {
            if (_process.HasExited)
                throw new InstanceProcessAlreadyExitedException();

            _process.StandardInput.Write(input);
            _process.StandardInput.Flush();
        }

        //public async Task SendErrorDataAsync(string input)
        //{
        //    if (_process.HasExited)
        //        throw new InstanceProcessAlreadyExitedException();

        //    StandardErrorInput ??= new StreamWriter(_process.StandardError.BaseStream);
        //    await StandardErrorInput.WriteAsync(input).ConfigureAwait(false);
        //    await StandardErrorInput.FlushAsync().ConfigureAwait(false);
        //}
        //public void SendErrorData(string input)
        //{
        //    if (_process.HasExited)
        //        throw new InstanceProcessAlreadyExitedException();

        //    StandardErrorInput ??= new StreamWriter(_process.StandardError.BaseStream);

        //    StandardErrorInput.Write(input);
        //    StandardErrorInput.Flush();
        //}

        public IProcessResult Kill()
        {
            try
            {
                _process.Kill();
                return GetResult();
            }
            catch (InvalidOperationException e)
            {
                throw new InstanceProcessAlreadyExitedException(e);
            }
        }

        public async Task<IProcessResult> WaitForExitAsync(CancellationToken cancellationToken = default)
        {
            if (cancellationToken != default)
                cancellationToken.Register(() => _process.Kill());

            await _mainTask.Task.ConfigureAwait(false);
            return GetResult();
        }

        public IProcessResult WaitForExit()
        {
            try
            {
                _process.WaitForExit();
                return GetResult();
            }
            catch (SystemException e)
            {
                throw new InstanceProcessAlreadyExitedException(e);
            }
        }

        public void Dispose()
        {
            _process.Dispose();
        }

        private void ReceiveExit(object sender, EventArgs e)
        {
            Task.WhenAll(_stdoutTask!.Task, _stderrTask!.Task).ContinueWith(task =>
            {
                Exited?.Invoke(sender, GetResult());
                return _mainTask.TrySetResult(true);
            });
        }
        private void ReceiveOutput(object _, DataReceivedEventArgs e) => AddData(_outputData, e.Data, OutputDataReceived, _stdoutTask);

        private void ReceiveError(object _, DataReceivedEventArgs e) => AddData(_errorData, e.Data, ErrorDataReceived, _stderrTask);

        private void AddData(Queue<string> dataList, string? data, EventHandler<string>? eventHandler, TaskCompletionSource<bool> taskCompletionSource)
        {
            if (data == null)
            {
                taskCompletionSource.TrySetResult(true);
                return;
            }

            if (_ignoreEmptyLines && data == string.Empty)
                return;

            dataList.Enqueue(data);

            ///all ways false as int n - int.maxvalue <= 0 if dataBufferCap is defualt value
            for (int i = 0; i < dataList.Count - _dataBufferCapacity; i++)
                dataList.Dequeue();
            eventHandler?.Invoke(this, data);

        }

        private IProcessResult GetResult()
        {
            int exitCode = _process.HasExited ? _process.ExitCode : -100;
            return new ProcessResult(exitCode, _outputData.ToArray(), _errorData.ToArray());
        }

        private void ThrowIfProcessExited()
        {
            if (_process.HasExited)
                throw new InstanceProcessAlreadyExitedException();
        }
    }
}