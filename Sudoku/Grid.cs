using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Serilog;

namespace Sudoku
{
    public class ValueLocation
    {
        public int Row { get; set; }
        public int Col { get; set; }
        public int Value { get; set; }

        public ValueLocation(int row, int col, int value)
        {
            Row = row;
            Col = col;
            Value = value;
        }
    }

    public class GridRowValues
    {
        public int Row { get; set; }
        public List<int> Values { get; set; }
    }

    public class Grid
    {
        public List<GridCell> Cells { get; set; }
        public int AssignedCells { get; set; }

        public Grid()
        {
            Cells = new List<GridCell>();

            for (int i = 1; i <= 9; i++)
            {
                for (int j = 1; j <= 9; j++)
                {
                    Cells.Add(new GridCell(i, j));
                }
            }
        }

        public int TotalCandiates => Cells.Sum(c => c.TotalCandidates);
        public int TotalValues => Cells.Count(c => (c.Value > 0));


        public GridCell Cell(int row, int col)
        {
            if (row > 9 || row < 1) throw new Exception("Index out of Range");
            if (col > 9 || col < 1) throw new Exception("Index out of Range");

            return Cells.FirstOrDefault(c => c.Row == row && c.Col == col);
        }

        public GridCell Cell(int square, int squareRow, int squareCol)
        {
            int row = GridCell.RowFromSquareRow(square, squareRow);
            int col = GridCell.ColFromSquareCol(square, squareCol);
            return Cell(row, col);
        }

        public void AssignValueToCell(int row, int col, int value, bool original = false)
        {
            if (row > 9 || row < 1) return;
            if (col > 9 || col < 1) return;

            var cell = Cells.FirstOrDefault(c => c.Row == row && c.Col == col);
            cell.Value = value;
            cell.Original = original;
            AssignedCells++;
        }

        public void AddLocations(List<ValueLocation> valueLocations)
        {
            foreach (var valLocation in valueLocations)
            {
                AssignValueToCell(valLocation.Row, valLocation.Col, valLocation.Value, true);
            }
        }

        public void AddLocations(List<GridRowValues> rowValues)
        {
            foreach (GridRowValues values in rowValues)
            {
                for (int col = 1; col <= 9; col++)
                {
                    if (values.Values[col - 1] != 0)
                    {
                        AssignValueToCell(values.Row, col, values.Values[col - 1], true);
                    }
                }
            }
        }

        public void ClearCandidates()
        {
            foreach (GridCell cell in this.Cells)
            {
                cell.Candidates.Clear();
            }
        }

        public static void WriteEmphasis(string text, bool emphasis = false)
        {
            if (emphasis)
            {
                Console.ForegroundColor = ConsoleColor.Yellow;
            }
            Console.Write(text);
            Console.ForegroundColor = ConsoleColor.Gray;
        }

        public static void WriteLineEmphasis(string text, bool emphasis = false)
        {
            if (emphasis)
            {
                Console.ForegroundColor = ConsoleColor.Yellow;
            }
            Console.WriteLine(text);
            Console.ForegroundColor = ConsoleColor.Gray;
        }

        public void PrintGrid()
        {
            Console.WriteLine("Grid");
            Console.WriteLine("============");

            StringBuilder output = new StringBuilder();
            for (int i = 1; i <= 9; i++)
            {
                Console.Write("  "); output.Append("  ");
                for (int j = 1; j <= 9; j++)
                {
                    WriteEmphasis(Cell(i, j).ValueOrSpace, Cell(i, j).Original); output.Append(Cell(i, j).ValueMarkedWithOriginal);
                    if (j == 3 || j == 6)
                    {
                        Console.Write(" || "); output.Append(" ||");
                    }
                    else if (j < 9)
                    {
                        Console.Write(" | "); output.Append(" |");
                    }
                }

                if (i != 9)
                {
                    Console.WriteLine(); output.AppendLine("");
                    if (i == 3 || i == 6)
                    {
                        output.AppendLine(" ======================================");
                        Console.WriteLine(" =====================================");
                    }
                    else
                    {
                        output.AppendLine(" --------------------------------------");
                        Console.WriteLine(" -------------------------------------");
                    }
                }
            }

            Log.Information("Grid");
            Log.Information("============");
            Log.Information(output.ToString());
            Console.WriteLine(" "); Log.Information(" ");
            Console.WriteLine(" ");

        }
        public void PrintCandidates()
        {
            Log.Debug("Candidates");
            Log.Debug("============");

            for (int row = 1; row <= 9; row++)
            {
                for (int col = 1; col <= 9; col++)
                {
                    if (Cell(row, col).Value == null)
                    {
                        var possibleValues = String.Join(", ", Cell(row, col).Candidates);
                        Log.Debug($"Row {row}, Col {col}, Values: {possibleValues}");
                    }
                }
                Log.Debug(" ");
            }
        }

        public void PrintCandidatesBySquare()
        {
            Log.Debug("Candidates");
            Log.Debug("============");

            for (int square = 1; square <= 9; square++)
            {

                Log.Debug("Square: " + square);

                for (int squareRow = 1; squareRow <= 3; squareRow++)
                {
                    for (int squareCol = 1; squareCol <= 3; squareCol++)
                    {
                        int row = GridCell.GridRow(square, squareRow);
                        int col = GridCell.GridCol(square, squareCol);

                        //if (Cell(row, col).Value == null)
                        //{
                        var value = Cell(row, col).Value;
                        var stringValue = (value == null) ? " " : value.ToString();
                        var candiates = String.Join(", ", Cell(row, col).Candidates);
                        Log.Debug($"Row {row}, Col {col} (sRow {squareRow}, sCol {squareCol}), Value {stringValue}, Candidates: {candiates}");
                        //}
                    }
                }
            }
        }
    }
}
