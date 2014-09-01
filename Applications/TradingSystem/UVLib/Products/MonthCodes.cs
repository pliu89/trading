using System;

namespace UV.Lib.Products
{
    public enum MonthCodes : uint
    {
        None = 00
        ,F = 01
        ,G = 02
        ,H = 03
        ,J = 04
        ,K = 05
        ,M = 06
        ,N = 07
        ,Q = 08
        ,U = 09
        ,V = 10
        ,X = 11
        ,Z = 12
        ,TermIdentified = 13            // This contract is not a month/year label, it is "term enumerated".
                                        // Rather, the numeric in its name refers to the offset from the leading contract.
    }
}
