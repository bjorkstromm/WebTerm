using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Threading.Tasks;

namespace WebTerm.Model
{
    public enum TerminalResponseType
    {
        Idle = 0,
        Dir = 1,
        Error = 2,
        Text = 3,
    }

    [DataContract]
    public class TerminalResponse
    {
        [DataMember]
        public TerminalResponseType Type { get; set; }

        [DataMember]
        public string Data { get; set; }
    }
}
