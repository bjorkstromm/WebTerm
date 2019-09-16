using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace WebTerm.Services
{
    public interface ITerminalServiceFactory
    {
        ITerminalService CreateTerminalService();
    }
}
