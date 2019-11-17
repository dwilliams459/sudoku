using System;
using System.Collections.Generic;
using System.Text;
using System.Linq;

namespace Soduko
{
    public class Cell
    {
        public int Row;
        public int Col;
        public int Value { get; set; }
        public int? NotZeroValue
        {
            get
            {
                return (Value == 0) ? null : (int?)Value;
            }
        }
        public List<int> Candidates { get; set; }

        public int TotalCandidates => Candidates.Count();

        public string SelectedOrEmptyValue
        {
            get
            {
                return (Value == 0) ? " " : Value.ToString();
            }
        }

        public int Square;

        public void SetSquare()
        {
            Square = GetSquare(Row, Col);
        }

        public static int GetSquare(int row, int col)
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

        public int SquareRow => Row % 3;
        public int SquareCol => Col % 3;

        public static int GridRow(int square, int squareRow) => square * squareRow;
        public static int GridCol(int square, int squareCol) => square * squareCol;

        public Cell(int row, int col)
        {
            Row = row;
            Col = col;
            SetSquare();

            Candidates = new List<int>();
        }
    }
}
