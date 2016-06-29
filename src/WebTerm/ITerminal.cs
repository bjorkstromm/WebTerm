using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace WebTerm
{
    public interface ITerminal : IDisposable
    {
        Action<string> OnRead { get; set; }
        Action<string> OnError { get; set; }

        Task WriteAsync(string data);

        void Start();
    }
}
