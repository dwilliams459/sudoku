using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Serilog;

namespace Sudoku
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
            Log.Information("Starting Grid");
            Grid.PrintGrid();
            bool runBlockAndColumn = false;
            bool ranBlockAndColumn = false;

            for (int i = 1; i <= 30; i++)
            {
                Log.Information("");
                Log.Information("Iteration: " + i);
                Log.Information("=============================");

                Log.Debug("Marking Canditates");
                Log.Debug("============");
                Grid.ClearCandidates();
                AddCandidates();
                //Grid.PrintCandidatesBySquare();

                if (runBlockAndColumn)
                {
                    Log.Debug("Block and Column Row Interaction");
                    Log.Debug("============");
                    BlockAndColumnRowInteraction();
                    //Grid.PrintCandidatesBySquare();
                    runBlockAndColumn = false;
                    ranBlockAndColumn = true;
                }

                Grid.AssignedCells = 0;
                int assignedCells = SetValuesToUniqueCandidatesByRowColSquare();
                Log.Information($"Assgined {Grid.AssignedCells} cells");
                Grid.PrintGrid();

                if (ranBlockAndColumn && Grid.AssignedCells == 0)
                {
                    Log.Information("Ran Block and column, no assigned cells, exiting");                   
                    break;
                }
                else if (Grid.AssignedCells == 0)
                {
                    Log.Information("No assigned cells");
                    runBlockAndColumn = true;
                }
                ranBlockAndColumn = false;
            }
        }

        public int AssignSoleCandidates()
        {
            Grid.AssignedCells = 0;
            var singleCandiateCells = Grid.Cells.Where(c => c.Candidates.Count == 1).ToList();
            singleCandiateCells.ForEach(cell =>
            {
                cell.Value = cell.Candidates[0];
                Grid.AssignedCells++;
                Log.Information($"Assigned cell at row {cell.Row} col {cell.Col} to value {cell.Value}");
            });

            Log.Information($"Assigned cells: {Grid.AssignedCells}");
            return Grid.AssignedCells;
        }

        public void AddCandidates()
        {
            int CandidatesMarked = 0;
            for (int row = 1; row <= 9; row++)
            {
                for (int col = 1; col <= 9; col++)
                {
                    if (Grid.Cell(row, col).Value == null)
                    {
                        Grid.Cell(row, col).Candidates = new List<int>();
                        for (int value = 1; value <= 9; value++)
                        {
                            if (!MatchInRow(row, value) && !MatchInColumn(col, value) && !MatchInSquare(row, col, value))
                            {
                                Grid.Cell(row, col).Candidates.Add(value);
                                CandidatesMarked++;
                            }
                        }
                    }
                }
            }
            Log.Information("Candidates Marked: " + CandidatesMarked);
        }

        public int SetValuesToUniqueCandidatesByRowColSquare()
        {
            int assignedCells = 0;
            for (int row = 1; row <= 9; row++)
            {
                for (int col = 1; col <= 9; col++)
                {
                    var cell = Grid.Cell(row, col);
                    foreach (int candidate in cell.Candidates)
                    {
                        // Counts of matches in this row/col/square, skiping the current cells location
                        int cellsWithCandidateInRow = Grid.Cells.Count(c => c.Row == row && c.Col != col && c.Candidates.Contains(candidate));
                        int cellsWithCandidateInCol = Grid.Cells.Count(c => c.Col == col && c.Row != row && c.Candidates.Contains(candidate));
                        int cellsWithCandidateInSqr = Grid.Cells.Count(c => c.Square == cell.Square 
                            && (c.Row != row && c.Col != col)  // Skip this cell
                            && c.Candidates.Contains(candidate)); 

                        if (cellsWithCandidateInCol == 0 && cellsWithCandidateInCol == 0 && cellsWithCandidateInSqr == 0)
                        {
                            // No cell has the same candidate in row/col/square.
                            Log.Debug($"Set cell at row {row}, col {col} to value {candidate}");
                            cell.Value = candidate;  
                            Grid.AssignedCells++;
                            assignedCells++;
                        }
                    }
                }
            }
            return assignedCells;
        }

        public bool MatchInRow(int row, int value)
        {
            var matched = Grid.Cells.Count(c => c.Row == row && c.Value == value);
            return (matched > 0);
        }

        public bool MatchInColumn(int col, int value)
        {
            var matched = Grid.Cells.Count(c => c.Col == col && c.Value == value);
            return (matched > 0);
        }

        public bool MatchInSquare(int row, int col, int value)
        {
            int square = GridCell.GetSquare(row, col);
            var matched = Grid.Cells.Count(c => c.Square == square && c.Value == value);
            return (matched > 0);
        }

        public void BlockAndColumnRowInteraction()
        {
            int removedCandiates = 0;
            for (int square = 1; square <= 9; square++)
            {
                for (int value = 1; value <= 3; value++)
                {
                    for (int squareRow = 1; squareRow <= 3; squareRow++)
                    {
                        // If all 
                        if (CandidateInSquareAndInRow(square, squareRow, value) &&
                            CandidateInSquareAndNotInRow(square, squareRow, value))
                        {
                            // Remove candidates
                            Log.Verbose($"Candidate {value} in row {GridCell.ColFromSquareCol(square, squareRow)}");
                            removedCandiates += RemoveCandidatesInRow(GridCell.RowFromSquareRow(square, squareRow), value);
                        }
                    }

                    for (int squareCol = 1; squareCol <= 3; squareCol++)
                    {
                        if (CandidateInSquareAndInCol(square, squareCol, value) &&
                            CandidateInSquareAndNotInCol(square, squareCol, value))
                        {
                            // Remove candidates
                            Log.Verbose($"Candidate {value} in col {GridCell.ColFromSquareCol(square, squareCol)}");
                            removedCandiates += RemoveCandidatesInCol(GridCell.ColFromSquareCol(square, squareCol), value);
                        }
                    }
                }
            }
            Log.Information("Block and column removed candidates: " + removedCandiates);
        }

        public bool CandidateInSquareAndInRow(int square, int squareRow, int value)
        {
            // Count cells where: in square, value is a candidate, IN row
            var matches = Grid.Cells.Count(c => c.Square == square && c.Candidates.Contains(value) && c.SquareRow == squareRow);
            Log.Verbose($"CandidateInSquareAndInRow: {square}, {squareRow}, {value}: matches:{matches}");
            return (matches > 0);
        }

        public bool CandidateInSquareAndNotInRow(int square, int squareRow, int value)
        {
            // Count cells where: in square, value is a candidate, NOT IN row
            var matches = Grid.Cells.Count(c => c.Square == square && c.Candidates.Contains(value) && c.SquareRow != squareRow);
            Log.Verbose($"CandidateInSquareAndNotInRow: {square}, {squareRow}, {value}: matches:{matches}");
            return (matches > 0);
        }

        public bool CandidateInSquareAndInCol(int square, int squareCol, int value)
        {
            // Count cells where: in square, value is a candidate, IN row
            var matches = Grid.Cells.Count(c => c.Square == square && c.Candidates.Contains(value) && c.SquareCol == squareCol);
            Log.Verbose($"CandidateInSquareAndInCol: {square}, {squareCol}, {value}: matches:{matches}");
            return (matches > 0);
        }

        public bool CandidateInSquareAndNotInCol(int square, int squareCol, int value)
        {
            // Count cells where: in square, value is a candidate, NOT IN row
            var matches = Grid.Cells.Count(c => c.Square == square && c.Candidates.Contains(value) && c.SquareCol != squareCol);
            Log.Verbose($"CandidateInSquareAndNotInCol: {square}, {squareCol}, {value}: matches:{matches}");
            return (matches > 0);
        }

        public int RemoveCandidatesInRow(int row, int candidate)
        {
            int removedValues = 0;
            Grid.Cells.Where(c => c.Row == row).ToList().ForEach(c =>
            {
                c.Candidates.Remove(candidate);
                removedValues++;
            });

            Log.Verbose($"Removed {removedValues} instances of candiate {candidate} in row {row}");
            return removedValues;
        }

        public int RemoveCandidatesInCol(int col, int candidate)
        {
            int removedValues = 0;
            Grid.Cells.Where(c => c.Col == col).ToList().ForEach(c =>
            {
                c.Candidates.Remove(candidate);
                removedValues++;
            });

            Log.Verbose($"Removed {removedValues} instances of candiate {candidate} in col {col}");
            return removedValues;
        }

    }
}
