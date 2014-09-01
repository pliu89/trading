using System;

namespace UV.Lib.Products
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
        ,Synthetic = 6
        ,Option = 7
    }
    #endregion // ProductTypes Enum
}
