using System;
using System.Collections.Generic;
//using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Ambre.Breconcile.Reconciler
{
    using Misty.Lib.Products;

    // *****************************************************************
    // ****                     Pos class                           ****
    // *****************************************************************
    public class Pos
    {
        public InstrumentName Instr;
        public int Qty;
        public Pos(InstrumentName instr, int qty = 0)
        {
            this.Instr = instr;
            this.Qty = qty;
        }
        public override string ToString()
        {
            return string.Format("{0} {1}", this.Instr, this.Qty);
        }
    }


    // *****************************************************************
    // ****                PosTable class                           ****
    // *****************************************************************
    public class PosTable
    {
        public List<Pos[]> Rows = new List<Pos[]>();
        public List<string> ColNames = new List<string>();
        //
        // Constructor
        //
        public PosTable(List<string> columnNames)
        {
            ColNames.AddRange(columnNames);
        }
        public PosTable(params object[] columnNames)
        {
            foreach (object o in columnNames)
                this.ColNames.Add(o.ToString());
        }
        // 
        // Row creation, manipulation 
        //
        public Pos[] CreateRow(out int newRowIndex)
        {
            Pos[] newRow = new Pos[ColNames.Count];
            newRowIndex = Rows.Count;
            Rows.Add(newRow);
            return newRow;
        }
        public Pos[] CreateRow()
        {
            int n;
            return CreateRow(out n);
        }
        public Pos RemoveAt(int row, int col)
        {
            if (row < 0 || col < 0)
                return null;
            if (row >= this.Rows.Count || col >= this.Rows[0].Length)
                return null;
            Pos pos = this.Rows[row][col];
            this.Rows[row][col] = null;
            return pos;
        }
        public Pos[] RemoveAt(int row)
        {
            if (row < 0)
                return null;
            if (row >= this.Rows.Count)
                return null;
            Pos[] removedRow = this.Rows[row];
            this.Rows.RemoveAt(row);
            return removedRow;
        }
        public void DeleteEmptyRows()
        {
            int row = 0;
            while (row < this.Rows.Count)
            {
                if (this.IsRowEmpty(row))
                    this.RemoveAt(row);
                else
                    row++;
            }
        }
        // 
        // Queries
        //
        public int FindColumn(string columnName)
        {
            return this.ColNames.IndexOf(columnName);
        }
        public int FindRow(InstrumentName name, int column)
        {
            for (int row = 0; row < Rows.Count; ++row)
                if (this.Rows[row][column].Instr.Equals(name))
                    return row;
            return -1;
        }
        public bool IsRowEmpty(int row)
        {
            if (row < 0)
                return true;
            if (row >= this.Rows.Count)
                return true;
            // Examine the row of interest.
            Pos[] aRow = this.Rows[row];
            for (int i = 0; i < aRow.Length; ++i)
                if (aRow[i] != null)
                    return false;
            return true;
        }
        /// <summary>
        /// Counts the number of entries in a particular row.
        /// </summary>
        /// <param name="row"></param>
        /// <returns>Number of non-empty entries in row.</returns>
        public int CountEntries(int row)
        {
            if (row < 0)
                return 0;
            if (row >= this.Rows.Count)
                return 0;
            // Examine the row of interest.
            int count = 0;
            Pos[] aRow = this.Rows[row];
            for (int i = 0; i < aRow.Length; ++i)
                if (aRow[i] != null)
                    count++;
            return count;
        }
        public bool IsReconciledRow(int row, int clearingColumn)
        {
            // Sum entire contents of this row, excluding clearing column entry.
            int sum = 0;
            for (int col = 0; col < this.Rows[row].Length; ++col)
                if (col != clearingColumn && this.Rows[row][col] != null)
                    sum += this.Rows[row][col].Qty;
            // Compare results with clearing column.
            if (this.Rows[row][clearingColumn] != null && this.Rows[row][clearingColumn].Qty != sum)
                return false;
            else if (this.Rows[row][clearingColumn] == null && sum != 0)
                return false;
            return true;
        }
        public bool IsReconciled(int clearingColumn)
        {
            if (clearingColumn < 0 || clearingColumn >= this.ColNames.Count)
                return false;
            for (int row = 0; row < this.Rows.Count; ++row)
                if (!IsReconciledRow(row, clearingColumn))
                    return false;
            return true;
        }
        //
        // Output
        //            
        private const string Format1 = "{1,6} {0,-22} "; //tighter 
        //
        public override string ToString()
        {
            StringBuilder s = new StringBuilder();
            for (int row = 0; row < this.Rows.Count; ++row)
            {
                for (int col = 0; col < this.Rows.Count; ++col)
                    s.AppendFormat(PosTable.Format1, this.Rows[row][col].Instr, this.Rows[row][col].Qty);
                s.AppendFormat("\r\n");
            }
            return s.ToString();
        }
        /*
        public override string ToString()
        {
            StringBuilder s = new StringBuilder();
            s.AppendFormat(this.ToStringHeader());
            for (int row = 0; row < this.Table.Count; ++row)
                s.AppendFormat("\r\n{0}", this.ToStringRow(row));
            return s.ToString();
        }
        */
        //
        private StringBuilder _sRow = new StringBuilder();
        public string ToStringRow(int row)
        {
            _sRow.Clear();
            for (int col = 0; col < this.Rows[row].Length; ++col)
            {
                Pos pos = this.Rows[row][col];
                if (pos != null)
                    _sRow.AppendFormat(PosTable.Format1, pos.Instr, pos.Qty);
                else
                    _sRow.AppendFormat(PosTable.Format1, " ", " ");
            }
            return _sRow.ToString();
        }
        private StringBuilder _sHeader = new StringBuilder();
        public string ToStringHeader()
        {
            _sHeader.Clear();
            for (int col = 0; col < this.ColNames.Count; ++col)
                _sHeader.AppendFormat(PosTable.Format1, this.ColNames[col], string.Empty);
            return _sHeader.ToString();
        }



    }// PosTable class
}
