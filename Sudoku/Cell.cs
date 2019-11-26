using System;
using System.Collections.Generic;
using System.Text;
using System.Linq;

namespace Sudoku
{
    public class GridCell
    {
        public int Row;
        public int Col;
        public int SquareRow { get; }
        public int SquareCol { get; }
        public int? Value { get; set; }
        public int Square { get; }
        public bool Original { get; set; }
        public int Index { get; }
        public int SquareIndex { get; set; }
        public List<int> Candidates { get; set; }

        public GridCell(int row, int col)
        {
            Row = row;
            Col = col;

            SquareRow = (Row % 3 == 0) ? 3 : Row % 3;  // Square row is 1, 2, or 3
            SquareCol = (Col % 3 == 0) ? 3 : Col % 3;  // Square col is 1, 2, or 3
            
            Square = SquareFromRowCol(Row, Col);
            
            Candidates = new List<int>();

            Index = (Row * 9) + col;
            int squareIndex = (SquareRow * 3) + SquareCol;
        }
        
        public string CellValueOrSpace
        {
            get
            {
                return (Value == null) ? " " : Value.ToString();
            }
        }

        public string CellValueMarkedWithOriginal
        {
            get
            {
                if (Value == null) return "  ";
                if (Original) return "." + Value.ToString();
                return " " + Value.ToString();
            }
        }

        public static int SquareFromRowCol(int row, int col)
        {
            int squareCol = (int)Math.Floor((decimal)(col - 1) / 3);
            if (row >= 1 && row <= 3)
            {
                return 1 + squareCol;
            }
            else if (row >= 4 && row <= 6)
            {
                return 4 + squareCol;
            }
            else
            {
                return 7 + squareCol;
            }
        }

        public static int RowFromSquareRow(int square, int squareRow)
        {
            if (squareRow > 3) return 0;
            var squareAdd = (int)(Math.Floor((decimal)(square - 1) / 3));
            return squareAdd + squareRow;
        }

        public static int ColFromSquareCol(int square, int squareCol)
        {
            if (squareCol > 3) return 0;
            var squareAdd = (int)(Math.Floor((decimal)(square - 1) / 3));
            return squareAdd + squareCol;
        }

        public static int GridRow(int square, int squareRow)
        {
            if (square >= 1 && square <= 3)
            {
                return squareRow;
            }
            else if (square >= 4 && square <= 6)
            {
                return 3 + squareRow;
            }
            else if (square >= 7 && square <= 9)
            {
                return 6 + squareRow;
            }
            return 0;
        }
        public static int GridCol(int square, int squareCol)
        {
            if (square == 1 || square == 4 || square == 7)
            {
                return squareCol;
            }
            else if (square == 2 || square == 5 || square == 8)
            {
                return squareCol + 3;
            }
            else if (square == 3 || square == 6 || square == 9)
            {
                return squareCol + 6;
            }
            return 0;
        }


    }
}
