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

        public List<GridCell> SquareCells(int square)
        {
            return Cells.Where(c => c.Square == square).ToList();
        }

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

        public int TotalCandiates => Cells.Sum(c => c.Candidates.Count());
        public int TotalValues => Cells.Count(c => (c.Value > 0));

        public Dictionary<int, List<int>> ValueLocations(int square)
        {
            Dictionary<int, List<int>> valueLocations = new Dictionary<int, List<int>>();

            // Piviot cell-candiates to value-locations.  (Location is a SquareIndex)
            // Example:
            // (Cell.Candidates) 1:1,3,4; 2:3,4; 3:4,5
            // (Value.Locations) 1:1; 2:0; 3:2; 4:3, 5:1 
            for (int value = 1; value <= 9; value++)
            {
                // Get list of locations (square indexes) for this value
                List<int> locations = Cells.Where(c => c.Square == square && c.Candidates.Contains(value)).Select(c => c.SquareIndex).ToList();
                valueLocations.Add(value, locations);
            }

            return valueLocations;
        }

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

            if (!original)
            {
                Log.Information($"Assigned {value} to {row}, {col} (square {GridCell.SquareFromRowCol(row, col)})");
            }

            // Remove value from list of candidates for this row, col, square
            if (!original)
            {
                cell.Candidates.Clear();
                RemoveCandidatesInCol(col, value);
                RemoveCandidatesInRow(row, value);
                RemoveCandidatesInSquare(cell.Square, value);
            }

            AssignedCells++;
        }

        public void AddLocations(List<ValueLocation> valueLocations)
        {
            foreach (var valLocation in valueLocations)
            {
                AssignValueToCell(valLocation.Row, valLocation.Col, valLocation.Value, true);
            }
        }

        public int RemoveCandidatesInRow(int row, int candidate, int notSquare = 0)
        {
            int removedCandidates = 0;
            Cells.Where(c => c.Row == row  && c.Square != notSquare).ToList().ForEach(c =>
            {
                if (c.Candidates.Contains(candidate))
                {
                    c.Candidates.Remove(candidate);
                    removedCandidates++;
                }
            });

            if (removedCandidates > 0)
            {
                Log.Verbose($"  Removed {removedCandidates} instances of candiate {candidate} in row {row}");
            }
            else
            {
                Log.Verbose($"  No instances of candiate {candidate} in row {row}");
            }

            return removedCandidates;
        }

        public int RemoveCandidatesInCol(int col, int candidate, int notSquare = 0)
        {
            int removedCandidates = 0;
            Cells.Where(c => c.Col == col && c.Square != notSquare).ToList().ForEach(c =>
            {
                if (c.Candidates.Contains(candidate))
                {
                    c.Candidates.Remove(candidate);
                    removedCandidates++;
                }
            });

            if (removedCandidates > 0)
            {
                Log.Verbose($"  Removed {removedCandidates} instances of candiate {candidate} in col {col}");
            }
            else
            {
                Log.Verbose($"  No instances of candiate {candidate} in col {col}");
            }

            return removedCandidates;
        }

        public int RemoveCandidatesInSquare(int square, int candidate)
        {
            int removedCandidates = 0;
            Cells.Where(c => c.Square == square).ToList().ForEach(c =>
            {
                if (c.Candidates.Contains(candidate))
                {
                    c.Candidates.Remove(candidate);
                    removedCandidates++;
                }
            });

            if (removedCandidates > 0)
            {
                Log.Verbose($"  Removed {removedCandidates} instances of candiate {candidate} in square {square}");
            }
            else
            {
                Log.Verbose($"  No instances of candiate {candidate} in square {square}");
            }

            return removedCandidates;
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

        public bool ValidateGrid()
        {
            foreach (GridCell cell in Cells.Where(c => c.Value != null))
            {
                bool hasDuplicatesInRow = (Cells.Count(c => c.Row == cell.Row && c.Value != null && c.Value == cell.Value) > 1);
                bool hasDuplicatesInCol = (Cells.Count(c => c.Col == cell.Col && c.Value != null && c.Value == cell.Value) > 1);
                bool hasDuplicatesInSquare = (Cells.Count(c => c.Square == cell.Square && c.Value != null && c.Value == cell.Value) > 1);

                if (hasDuplicatesInRow || hasDuplicatesInCol || hasDuplicatesInSquare)
                {
                    Log.Error($"Invalid grid: Duplicate for value {cell.Value} in {cell.Row}, {cell.Col} ");

                    Console.ForegroundColor = ConsoleColor.DarkRed;
                    Console.WriteLine($"Invalid grid: Duplicate for value {cell.Value} in {cell.Row}, {cell.Col}");
                    Console.WriteLine();
                    Console.ForegroundColor = ConsoleColor.Gray;
                    return false;
                }
            }
            Log.Information("Valid Grid");

            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine("Valid Grid");
            Console.WriteLine();
            Console.ForegroundColor = ConsoleColor.Gray;
            return true;
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

        public void PrintGrid(int? iteration = null)
        {
            Console.Write("Grid ");
            if (iteration != null & iteration > 0)
            {
                Console.Write($"({iteration})");
            }
            Console.WriteLine();
            Console.WriteLine("============");

            StringBuilder output = new StringBuilder();
            for (int i = 1; i <= 9; i++)
            {
                Console.Write("  "); output.Append("  ");
                for (int j = 1; j <= 9; j++)
                {
                    WriteEmphasis(Cell(i, j).CellValueOrSpace, Cell(i, j).Original); output.Append(Cell(i, j).CellValueMarkedWithOriginal);
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

        public void PrintGridWithCandiates(int? iteration = null)
        {
            Console.Write("Grid ");
            if (iteration != null & iteration > 0)
            {
                Console.Write($"({iteration})");
            }
            Console.WriteLine();
            Console.WriteLine("============");

            StringBuilder output = new StringBuilder();
            for (int i = 1; i <= 9; i++)
            {
                Console.Write("  "); output.Append("  ");
                for (int j = 1; j <= 9; j++)
                {
                    WriteEmphasis(Cell(i, j).CellValueOrSpace, Cell(i, j).Original); output.Append(Candidates(i, j));
                    if (j == 3 || j == 6)
                    {
                        Console.Write(" || "); output.Append(" || ");
                    }
                    else if (j < 9)
                    {
                        Console.Write(" | "); output.Append(" | ");
                    }
                }

                if (i != 9)
                {
                    Console.WriteLine(); output.AppendLine("");
                    if (i == 3 || i == 6)
                    {
                        output.AppendLine(" =========================================================================================================================");
                        Console.WriteLine(" =====================================");
                    }
                    else
                    {
                        output.AppendLine(" -------------------------------------------------------------------------------------------------------------------------");
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
            Log.Debug($"Candidates ({this.TotalCandiates})");
            Log.Debug("===============");

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
            Log.Debug($"Candidates ({this.TotalCandiates})");
            Log.Debug("===============");

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

        private string Candidates(int square, int squareRow, int squareCell)
        {
            var cell = Cell(square, squareRow, squareCell);
            var cellString = string.Empty;
            if(cell.Value != null)
            {   
                if (cell.Original){
                    return cell.Value.ToString() + "*        ";

                }
                else{
                    return cell.Value.ToString() + "         ";
                }
            }
            string candidates = "(" + string.Join("", cell.Candidates.ToArray()) + ")"; 
            return candidates + "         ".Substring(0, (10 - candidates.Length)); 
        }

        private string Candidates(int row, int col)
        {
            var cell = Cell(row, col);
            var cellString = string.Empty;
            if(cell.Value != null)
            {
                if (cell.Original){
                    return cell.CellValueOrSpace + "*        ";

                }
                else{
                    return cell.CellValueOrSpace + "         ";
                }
            }
            string candidates = "(" + string.Join("", cell.Candidates.ToArray()) + ")"; 
            return candidates + "          ".Substring(0, (10 - candidates.Length)); 
        }

        public void PrintSquareGridWithCandiates(int square)
        {
            StringBuilder gridSquare = new StringBuilder();

            gridSquare.AppendLine("Square " + square);
            gridSquare.AppendLine("===========");

            var squareCells = Cells.Where(c => c.Square == square);
            for (int sqRow = 1; sqRow <= 3; sqRow++)
            {
                gridSquare.AppendLine($"{Candidates(square, sqRow, 1)} | {Candidates(square, sqRow, 2)} | {Candidates(square, sqRow, 3)}");
                if (sqRow == 1 || sqRow == 2)
                {
                    gridSquare.AppendLine("-----------------------------------------");
                }
            }

            Log.Information(gridSquare.ToString());

        }
    }
}
