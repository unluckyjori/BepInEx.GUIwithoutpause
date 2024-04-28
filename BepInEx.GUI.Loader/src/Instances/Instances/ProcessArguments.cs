using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using Instances.Exceptions;

namespace Instances
{
    public class ProcessArguments
    {
        private const string FileNotFound = "The system cannot find the file specified";
        private const string DirectoryNotFound = "No such file or directory";

        private readonly ProcessStartInfo _processStartInfo;

        public ProcessArguments(string path, string arguments) :
            this(new ProcessStartInfo { FileName = path, Arguments = arguments })
        { }

        public ProcessArguments(ProcessStartInfo processStartInfo)
        {
            _processStartInfo = processStartInfo;
        }

        public bool IgnoreEmptyLines { get; set; }
        public int DataBufferCapacity { get; set; } = int.MaxValue;

        public event EventHandler<IProcessResult>? Exited;
        public event EventHandler<string>? OutputDataReceived;
        public event EventHandler<string>? ErrorDataReceived;

        public ProcessInstance Start()
        {
            _processStartInfo.CreateNoWindow = true;
            _processStartInfo.UseShellExecute = false;
            _processStartInfo.RedirectStandardOutput = true;
            _processStartInfo.RedirectStandardInput = true;
            _processStartInfo.RedirectStandardError = true;
            var process = new Process
            {
                StartInfo = _processStartInfo,
                EnableRaisingEvents = true,
            };

            var instance = new ProcessInstance(process, IgnoreEmptyLines, DataBufferCapacity);

            instance.Exited += Exited;
            instance.OutputDataReceived += OutputDataReceived;
            instance.ErrorDataReceived += ErrorDataReceived;

            try
            {
                process.Start();
                process.BeginOutputReadLine();
                process.BeginErrorReadLine();
                return instance;
            }

            catch (Win32Exception e) when (e.Message.Contains(FileNotFound) || e.Message.Contains(DirectoryNotFound))
            {
                throw new InstanceFileNotFoundException(_processStartInfo.FileName, e);
            }
        }

        public async Task<IProcessResult> StartAndWaitForExitAsync(CancellationToken cancellationToken = default)
        {
            using var instance = this.Start();
            return await instance.WaitForExitAsync(cancellationToken);
        }
        public IProcessResult StartAndWaitForExit()
        {
            using var instance = this.Start();
            return instance.WaitForExit();
        }
    }
}