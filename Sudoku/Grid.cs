using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Soduko
{
    public class ValueLocation 
    {
        public int Row { get; set; }
        public int Col { get; set; }
        public int Value { get; set; }
    }

    public class Grid
    {
        public List<Cell> Cells { get; set; }
        public int UpdatedCells { get; set; }

        public Grid()
        {
            Cells = new List<Cell>();

            for (int i = 1; i <= 9; i++ )
            {
                for (int j = 1; j <= 9; j++)
                {
                    Cells.Add(new Cell(i, j));
                }
            }
        }

        public int TotalCandiates => Cells.Sum(c => c.TotalCandidates);
        public int TotalValues => Cells.Count(c => (c.Value > 0));


        public Cell Cell(int row, int col)
        {
            if (row > 9 || row < 1) throw new Exception("Index out of Range");
            if (col > 9 || col < 1) throw new Exception("Index out of Range");

            return Cells.FirstOrDefault(c => c.Row == row && c.Col == col);
        }

        public void AddValue(int row, int col, int value)
        {
            if (row > 9 || row < 1) return;
            if (col > 9 || col < 1) return;

            var cell = Cells.FirstOrDefault(c => c.Row == row && c.Col == col);
            cell.Value = value;
            UpdatedCells++;
        }

        public void AddLocations(List<ValueLocation> valueLocations)
        {
            foreach (var valLocation in valueLocations)
            {
                AddValue(valLocation.Row, valLocation.Col, valLocation.Value);
            }
        }

        public void PrintGrid()
        {
            Console.WriteLine();

            StringBuilder output = new StringBuilder();
            for (int i = 1; i <= 9; i++)
            {
                for (int j = 1; j <= 9; j++)
                {
                    output.Append(Cell(i, j).SelectedOrEmptyValue);
                    if (j == 3 || j == 6)
                    {
                        output.Append(" || ");
                    }
                    else if (j < 9)
                    {
                        output.Append(" | ");
                    }
                }

                if (i != 9)
                {
                    output.AppendLine("");
                    if (i == 3 || i == 6)
                    {
                        output.AppendLine("===================================");
                    }
                    else
                    {
                        output.AppendLine("-----------------------------------");
                    }
                }
            }

            Console.WriteLine(output.ToString());
            Console.WriteLine("Assigned cells: " + UpdatedCells);
            Console.WriteLine();
        }
    }
}
