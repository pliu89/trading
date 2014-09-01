using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace UV.Lib.DatabaseReaderWriters.Queries
{
    public enum QueryStatus
    {
        New, 
        Partial,
        Completed,
        Failed
    }
}
