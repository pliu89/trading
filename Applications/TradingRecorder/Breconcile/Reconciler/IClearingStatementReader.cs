using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Ambre.Breconcile.Reconciler
{
    using Misty.Lib.Products;
    using Misty.Lib.OrderHubs;

    public interface IClearingStatementReader
    {

        //
        // Public Properties
        //
        string PositionFilePath
        {
            get;
        }
        Dictionary<string, Dictionary<InstrumentName, List<Fill>>> Position
        {
            get;
        }
        Dictionary<string, Dictionary<InstrumentName, List<Fill>>> Fills
        {
            get;
        }
        //Dictionary<Product, Product> Clearing2BreProductMap
        //{
        //    get;
        //}
        InstrumentNameMapTable InstrumentNameMap
        {
            get;
        }

        //
        // Static functions
        //
        //static bool TryReadStatement(DateTime reconcilliationDate, string statementPath, out IClearingStatementReader reader);




        //
        // Public instance functions
        //
        //void SaveNewProductTable(Dictionary<Product, Product> table, string newFileName = "");


    }

}
