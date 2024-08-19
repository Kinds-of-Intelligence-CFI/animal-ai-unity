using System;

/// <summary>
/// ArenasParameters namespace contains classes that are used to deserialize YAML files.
/// </summary>
namespace ArenasParameters
{
    public class ArenasParametersEventArgs : EventArgs
    {
        public byte[] arenas_yaml { get; set; } /* Byte array that contains the YAML file */

        public ArenasParametersEventArgs() { } /* Empty constructor */
    }
}
