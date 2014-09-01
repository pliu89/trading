using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Misty.Lib.Products
{
    #region ProductTypes Enum
    // *****************************************************************
    // ****                 ProductTypes Enum                       ****
    // *****************************************************************          
    // 
    //
    public enum ProductTypes : uint
    {
        Unknown = 0                 // this is the default value, useful for identifying the "empty" Product
        ,Future = 1
        ,Equity = 2
        ,Spread = 3
        ,Bond = 4
        ,AutoSpreaderSpread = 5
        ,Strategy = 6
        ,Option = 7
        ,Cash
    }
    #endregion // ProductTypes Enum
}
