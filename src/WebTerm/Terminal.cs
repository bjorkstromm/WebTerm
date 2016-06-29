using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;

namespace WebTerm
{
    public class Terminal : ITerminal
    {
        private readonly Process _process;

        public Terminal()
        {
            _process = new Process
            {
                StartInfo =
                {
                    FileName = "cmd.exe",
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

        public async Task WriteAsync(string data)
        {
            var streamWriter = _process.StandardInput;
            await streamWriter.WriteLineAsync(data);
            await streamWriter.FlushAsync();
        }

        public void Start()
        {
            _process.Start();
            _process.BeginOutputReadLine();
            _process.BeginErrorReadLine();
        }

        public void Dispose()
        {
            _process.Dispose();
        }
    }
}
