using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Soduko
{


    public class Sudoku
    {

        public Grid Grid { get; set; }

        public Sudoku()
        {
            Grid = new Grid();
        }


        public void Solve()
        { 
            Console.WriteLine("Starting Grid");
            Grid.PrintGrid();

            for (int i = 0; i < 20; i++)
            {
                MarkCandidates();
                BlockAndColumnRowInteraction();
                FindSoleCandidates();
                Grid.PrintGrid();
            }

            PrintPossibleValues();
        }

        public void FindSoleCandidates()
        {
            var cellsWithOnePossiblity = Grid.Cells.Where(c => c.Candidates.Count == 1);
            foreach (var cell in cellsWithOnePossiblity)
            {
                if (cell.Value == 0)
                {
                    cell.Value = cell.Candidates.FirstOrDefault();
                    Grid.UpdatedCells++;
                }
            }
        }

        public void MarkCandidates()
        {
            Grid.UpdatedCells = 0;
            for (int row = 1; row <= 9; row++)
            {
                for (int col = 1; col <= 9; col++)
                {
                    if (Grid.Cell(row, col).Value == 0)
                    {
                        Grid.Cell(row, col).Candidates = new List<int>();
                        for (int value = 1; value <= 9; value++)
                        {
                            if (!MatchInRow(row, value) && !MatchInColumn(col, value) && !MatchInSquare(row, col, value))
                            {
                                Grid.Cell(row, col).Candidates.Add(value);
                            }
                        }
                    }
                }
            }
        }

        public bool MatchInRow(int row, int value)
        {
            var matched = Grid.Cells.Count(c => c.Row == row && c.NotZeroValue == value);
            return (matched > 0);
        }

        public bool MatchInColumn(int col, int value)
        {
            var matched = Grid.Cells.Count(c => c.Col == col && c.NotZeroValue == value);
            return (matched > 0);
        }

        public bool MatchInSquare(int row, int col, int value)
        {
            int square = Cell.GetSquare(row, col);
            var matched = Grid.Cells.Count(c => c.Square == square && c.NotZeroValue == value);
            return (matched > 0);
        }

        public void PrintPossibleValues()
        {
            for (int row = 1; row <= 9; row++)
            {
                for (int col = 1; col <= 9; col++)
                {
                    if (Grid.Cell(row, col).Value == 0)
                    {
                        var possibleValues = String.Join(", ", Grid.Cell(row, col).Candidates);
                        Console.WriteLine($"Row {row}, Col {col}, Values: {possibleValues}");
                    }
                }
                Console.WriteLine();
            }
        }

        public void BlockAndColumnRowInteraction()
        {
            for (int square = 1; square <= 9; square++)
            {
                for (int value = 1; value <= 3; value++)
                {
                    for (int squareRow = 1; squareRow <= 3; squareRow++)
                    {
                        if (ValueInSquareAndInRow(square, squareRow, value) &&
                            ValueInSquareAndNotInRow(square, squareRow, value))
                        {
                            // Remove candidates
                        }
                    }

                    for (int squareCol = 1; squareCol <= 3; squareCol++)
                    {
                        if (ValueInSquareAndInCol(square, squareCol, value) &&
                            ValueInSquareAndNotInCol(square, squareCol, value))
                        {
                            // Remove candidates
                        }
                    }
                }
            }
        }

        public bool ValueInSquareAndInRow(int square, int squareRow, int value)
        {
            // Count cells where: in square, value is a candidate, IN row
            var matches = Grid.Cells.Count(c => c.Square == square && c.Candidates.Contains(value) && c.SquareRow == squareRow);
            return (matches > 0);
        }

        public bool ValueInSquareAndNotInRow(int square, int squareRow, int value)
        {
            // Count cells where: in square, value is a candidate, NOT IN row
            var matches = Grid.Cells.Count(c => c.Square == square && c.Candidates.Contains(value) && c.SquareRow != squareRow);
            return (matches > 0);
        }

        public bool ValueInSquareAndInCol(int square, int squareCol, int value)
        {
            // Count cells where: in square, value is a candidate, IN row
            var matches = Grid.Cells.Count(c => c.Square == square && c.Candidates.Contains(value) && c.SquareCol == squareCol);
            return (matches > 0);
        }

        public bool ValueInSquareAndNotInCol(int square, int squareCol, int value)
        {
            // Count cells where: in square, value is a candidate, NOT IN row
            var matches = Grid.Cells.Count(c => c.Square == square && c.Candidates.Contains(value) && c.SquareCol != squareCol);
            return (matches > 0);
        }

        public int RemoveCandidatesInRow()
        {
            throw new NotImplementedException();
        }
    }
}
