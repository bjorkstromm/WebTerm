using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;

namespace WebTerm
{
    internal class TerminalService : ITerminalService
    {
        private readonly Process _process;

        public TerminalService()
        {
            _process = new Process
            {
                StartInfo =
                {
                    FileName = "cmd.exe",
                    //Arguments = "/k",
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    RedirectStandardInput = true,
                    RedirectStandardError = true
                }
            };
            _process.OutputDataReceived += (sender, e) => OnRead?.Invoke(e.Data);
            _process.ErrorDataReceived += (sender, e) => OnError?.Invoke(e.Data);
        }

        public Action<string> OnRead { get; set; }
        public Action<string> OnError { get; set; }
        public Action OnIdle { get; set; }

        public async Task WriteAsync(string data)
        {
            var streamWriter = _process.StandardInput;
            await streamWriter.WriteLineAsync(data);
            await streamWriter.FlushAsync();

            await WaitForIdleAsync();
        }

        public async Task StartAsync()
        {
            _process.Start();
            _process.BeginOutputReadLine();
            _process.BeginErrorReadLine();

            await WaitForIdleAsync();
        }

        public void Dispose()
        {
            _process.Dispose();
        }

        private async Task WaitForIdleAsync()
        {
            bool isIdle = false;
            const int threshold = 10;

            while(!isIdle)
            {
                var startCpuTime = _process.TotalProcessorTime;

                await Task.Delay(threshold);
                _process.Refresh();

                var endCpuTime = _process.TotalProcessorTime;

                isIdle = (endCpuTime - startCpuTime < TimeSpan.FromMilliseconds(threshold));
            }

            OnIdle?.Invoke();
        }
    }
}
