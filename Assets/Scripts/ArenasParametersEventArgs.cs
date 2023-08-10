using System;

namespace ArenasParameters
{
    public class ArenasParametersEventArgs : EventArgs
    {
        public byte[] arenas_yaml { get; set; }
    }
}
