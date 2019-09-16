using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace WebTerm
{
    public interface ITerminalService : IDisposable
    {
        Action<string> OnRead { get; set; }
        Action<string> OnError { get; set; }
        Action OnIdle { get; set; }

        Task WriteAsync(string data);

        Task StartAsync();
    }
}
