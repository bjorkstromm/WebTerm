using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace WebTerm.Services
{
    internal class TerminalServiceFactory : ITerminalServiceFactory
    {
        public ITerminalService CreateTerminalService()
        {
            return new TerminalService();
        }
    }
}
