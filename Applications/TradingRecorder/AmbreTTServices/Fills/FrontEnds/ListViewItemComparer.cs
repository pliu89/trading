using System;
using System.Collections.Generic;
using System.Text;
using System.Windows.Forms;

namespace Ambre.TTServices.Fills.FrontEnds
{

    
    public class ListViewItemComparer : System.Collections.IComparer
    {
        //
        // Members
        //
        private int col;
        
        //
        // Constructors
        //
        /// <summary>
        /// Default constructor, sorting on column zero content.
        /// </summary>
        public ListViewItemComparer()                           
        {
            col = 0;
        }
        /// <summary>
        /// Constructor to use when we want to sort the datagrid
        /// using column index provided.
        /// </summary>
        /// <param name="column"></param>
        public ListViewItemComparer(int column)
        {
            col = column;
        }

        //
        // Public methods
        //
        public int Compare(object x, object y)
        {
            int returnVal = -1;
            returnVal = String.Compare(((ListViewItem)x).SubItems[col].Text,
            ((ListViewItem)y).SubItems[col].Text);
            return returnVal;
        }
    

    }
}
