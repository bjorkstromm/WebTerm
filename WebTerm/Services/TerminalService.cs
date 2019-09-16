
namespace WebTerm
{
    
    
    internal class TerminalService : ITerminalService
    {
        private readonly System.Diagnostics.Process _process;


        // https://github.com/GoogleChromeLabs/carlo
        // https://github.com/gkmo/CarloSharp

        private static string GetSystemDirectory(bool placeInEnvironmentVariable)
        {
            string sysDir = "";

            if (System.Environment.Is64BitOperatingSystem)
                sysDir = System.Environment.ExpandEnvironmentVariables("%windir%\\SysWOW64");
            else
                sysDir = System.Environment.ExpandEnvironmentVariables("%windir%\\System32");

            if (placeInEnvironmentVariable)
                System.Environment.SetEnvironmentVariable(
                    "SYSDIR32", sysDir, System.EnvironmentVariableTarget.User
                );

            // C:\Windows\SysWOW64
            // C:\Windows\System32

            return sysDir;
        }
        
        
        private static string GetSystemDirectory()
        {
            // https://github.com/mholo65/WebTerm
            // https://github.com/GoogleChromeLabs/carlo
            // https://github.com/gkmo/CarloSharp
            return GetSystemDirectory(false);
        }


        public TerminalService()
        {
            string fileName = System.IO.Path.Combine(GetSystemDirectory(), "cmd.exe");

            if (System.Environment.OSVersion.Platform == System.PlatformID.Unix)
            {
                fileName = "/usr/bin/bash";

                if (!System.IO.File.Exists(fileName))
                    fileName = "/bin/bash";

                if (!System.IO.File.Exists(fileName))
                    fileName = "/usr/bin/sh";

                if (!System.IO.File.Exists(fileName))
                    fileName = "/bin/sh";


                if (!System.IO.File.Exists(fileName))
                {

                    // cat /etc/shells
                    if (System.IO.File.Exists("/etc/shells"))
                    {

                        using (System.IO.StreamReader sr = new System.IO.StreamReader("/etc/shells"))
                        {
                            string line = null;
                            while ((line = sr.ReadLine()) != null)
                            {
                                line = line.Trim(' ', '\t', '\r', '\n', '\v');
                                // System.Console.WriteLine(line);

                                fileName = line;
                                if (System.IO.File.Exists(fileName))
                                    break;
                            } // Whend 

                        } // End Using sr

                    } // End if (System.IO.File.Exists("/etc/shells")) 

                } // End if (!System.IO.File.Exists(fileName))

            } // End if (System.Environment.OSVersion.Platform == System.PlatformID.Unix) 

            _process = new System.Diagnostics.Process
            {
                StartInfo = new System.Diagnostics.ProcessStartInfo()
                {
                    FileName = fileName,
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

        public System.Action<string> OnRead { get; set; }
        public System.Action<string> OnError { get; set; }
        public System.Action OnIdle { get; set; }

        public async System.Threading.Tasks.Task WriteAsync(string data)
        {
            var streamWriter = _process.StandardInput;
            await streamWriter.WriteLineAsync(data);
            await streamWriter.FlushAsync();

            await WaitForIdleAsync();
        }

        public async System.Threading.Tasks.Task StartAsync()
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

        private async System.Threading.Tasks.Task WaitForIdleAsync()
        {
            bool isIdle = false;
            const int threshold = 10;
            
            while(!isIdle)
            {
                System.TimeSpan startCpuTime = _process.TotalProcessorTime;
                
                await System.Threading.Tasks.Task.Delay(threshold);
                _process.Refresh();
                
                System.TimeSpan endCpuTime = _process.TotalProcessorTime;
                
                isIdle = (endCpuTime - startCpuTime <System.TimeSpan.FromMilliseconds(threshold));
            } // Whend 
            
            OnIdle?.Invoke();
        } // End Task WaitForIdleAsync 
        
        
    } // End Class TerminalService 
    
    
} // End Namespace 
