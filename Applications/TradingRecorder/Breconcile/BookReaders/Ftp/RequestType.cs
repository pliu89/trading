using System;
using System.Collections.Generic;
using System.Text;

namespace Ambre.Breconcile.Ftp
{
    public enum RequestType
    {
        None,
        Stop,                   // request that Hub stops
        GetNewFiles
    }
}
