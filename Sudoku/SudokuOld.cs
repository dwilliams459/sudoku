using System.Linq;
using Serilog;

namespace Sudoku
{
    /// <summary>
    /// Old methods go here
    /// </summary>
    public class SudokuOld : Sudoku
    {
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


        public bool CandidateInSquareAndInRow(int square, int squareRow, int value)
        {
            // Count cells where: in square, value is a candidate, IN row
            var matches = Grid.Cells.Count(c => c.Square == square && c.Candidates.Contains(value) && c.SquareRow == squareRow);
            //Log.Verbose($"    CandidateInSquareAndInRow: {square}, {squareRow}, {value}: matches:{matches}");
            return (matches > 0);
        }

        public bool CandidateInSquareAndNotInRow(int square, int squareRow, int value)
        {
            // Count cells where: in square, value is a candidate, NOT IN row
            var matches = Grid.Cells.Count(c => c.Square == square && c.Candidates.Contains(value) && c.SquareRow != squareRow);
            //Log.Verbose($"    CandidateInSquareAndNotInRow: {square}, {squareRow}, {value}: matches:{matches}");
            return (matches > 0);
        }

        public bool CandidateInSquareAndInCol(int square, int squareCol, int value)
        {
            // Count cells where: in square, value is a candidate, IN row
            var matches = Grid.Cells.Count(c => c.Square == square && c.Candidates.Contains(value) && c.SquareCol == squareCol);
            //Log.Verbose($"    CandidateInSquareAndInCol: {square}, {squareCol}, {value}: matches:{matches}");
            return (matches > 0);
        }

        public bool CandidateInSquareAndNotInCol(int square, int squareCol, int value)
        {
            // Count cells where: in square, value is a candidate, NOT IN row
            var matches = Grid.Cells.Count(c => c.Square == square && c.Candidates.Contains(value) && c.SquareCol != squareCol);
            //Log.Verbose($"    CandidateInSquareAndNotInCol: {square}, {squareCol}, {value}: matches:{matches}");
            return (matches > 0);
        }


        public int SetValuesToUniqueCandidatesByRowColSquare()
        {
            Log.Debug("SetValuesToUniqueCandidatesByRowColSquare..");
            int assignedCells = 0;

            // Iterate through row, col, candidate (1-9)
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
                            Grid.AssignValueToCell(cell.Row, cell.Col, candidate); // cell.Value = candidate;  
                            Grid.AssignedCells++;
                            assignedCells++;
                        }
                    }
                }
            }
            Log.Verbose($"SetValuesToUniqueCandidatesByRowColSquare assigned {assignedCells} cells");
            return assignedCells;
        }

    }
}