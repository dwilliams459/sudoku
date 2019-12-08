using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Newtonsoft.Json;
using Serilog;

namespace Sudoku
{

    public enum PremptiveSetType
    {
        Row = 1,
        Col = 2,
        Square = 3
    }

    public class Sudoku
    {

        public Grid Grid { get; set; }

        private int AssignedWithSinglePossibleLocation;

        private int solveIteration;

        public Sudoku()
        {
            Grid = new Grid();
        }

        public bool Solve(int iterations = 10)
        {
            Log.Information("Starting Grid");
            Log.Information($"Total cells with assigned values: { Grid.Cells.Count(c => c.Value != null)}");

            Grid.PrintGrid(0);
            Grid.ValidateGrid();

            iterations = (iterations == 0) ? 30 : iterations;
            for (solveIteration = 1; solveIteration <= iterations; solveIteration++)
            {
                Log.Information("");
                Log.Information("Iteration: " + solveIteration);
                Log.Information("====================================================");

                Log.Debug("Marking Canditates");
                Log.Debug("============");

                SetCandidates();
                Grid.PrintGridWithCandidates();

                int removedCandidates = 0;
                int removeCandidiateIteration = 0;
                do
                {
                    removeCandidiateIteration++;
                    int candidateCount = Grid.Cells.Sum(c => c.Candidates.Count);

                    RemovePointedPairCandidates();
                    FindHiddenPairsBySquare();
                    Grid.PrintGridWithCandidates();

                    removedCandidates = candidateCount - Grid.Cells.Sum(c => c.Candidates.Count);
                    Log.Debug($"Removed {removedCandidates} candidates (iteration {removeCandidiateIteration})");
                } while (removedCandidates > 0 && removeCandidiateIteration < 5);

                Log.Debug($"Assign candidates with Single Possible Location..");
                Grid.AssignedCells = 0;
                AssignedWithSinglePossibleLocation = 0;

                AssignNakedSingleCandidates();

                Log.Debug($"SinglePossibleLocation set values to unique candidates: Assigned {AssignedWithSinglePossibleLocation} cells");
                Log.Debug("");

                Log.Information($"Assigned {Grid.AssignedCells} cells");
                Log.Information($"Total cells with assigned values: {Grid.Cells.Count(c => c.Value != null)}");
                Grid.PrintGridWithCandidates(solveIteration);
                Grid.PrintGrid(solveIteration);

                if (!Grid.ValidateGrid())
                {
                    return false;
                }

                if (Grid.Cells.Count(c => c.Value == null || c.Value == 0) == 0)
                {
                    Log.Information("No empty cells, successful assignment, exit.");
                    return true;
                }

                if (Grid.AssignedCells == 0)
                {
                    Log.Information("No assigned cells, exiting");
                    return false;
                }
            }
            return true;
        }

        #region Set candidates
        /// <summary>
        /// For each cell, for each candidate (1-9): candidate is not already assigned in the row, column or square
        /// </summary>
        public void SetCandidates()
        {
            Log.Debug("Seting Candidates...");
            // Reset all candidates
            Grid.ClearCandidates();

            // Mark candidates.  
            // For values 1-9: for each cell, there is no matching value in row, col, square
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
            int square = GridCell.GridSquare(row, col);
            var matched = Grid.Cells.Count(c => c.Square == square && c.Value == value);
            return (matched > 0);
        }
        #endregion Set candidates


        #region Assign candidates to cells
        /// <summary>
        /// Assign candidates with only a single possible location: cells that only have a single possible candidate.
        /// </summary>
        public void AssignNakedSingleCandidates()
        {
            Log.Debug("Assigning cells with only one candidate... ");
            bool assignedValue = false;

            foreach (GridCell cell in Grid.Cells)
            {
                if (cell.Candidates.Count == 1)
                {
                    Grid.AssignValueToCell(cell.Row, cell.Col, cell.Candidates.FirstOrDefault());
                }
            }

            do
            {
                // Repeat until none are assigned.
                assignedValue = AssignFirstHiddenSingleCandidate(assignedValue);
            } while (assignedValue == true);
        }

        /// <summary>
        /// </summary>
        public void AssignHiddenSingleCandidates()
        {
            Log.Debug("Assigning candidates with single possible location... ");
            bool assignedValue = false;

            do
            {
                // Repeat until none are assigned.
                assignedValue = AssignFirstHiddenSingleCandidate(assignedValue);
            } while (assignedValue == true);
        }

        private bool AssignFirstHiddenSingleCandidate(bool assignedValue)
        {
            foreach (GridCell cell in Grid.Cells)
            {
                if (cell.Row == 1 && cell.Col == 2)
                {
                    Log.Verbose("Debug stop");
                }
                foreach (int candidate in cell.Candidates)
                {
                    bool inOnlyOneColumn = (Grid.Cells.Count(c => c.Col == cell.Col && c.Candidates.Contains(candidate) == true) == 1);
                    bool inOnlyOneRow = (Grid.Cells.Count(c => c.Row == cell.Row && c.Candidates.Contains(candidate) == true) == 1);
                    bool inOnlyOneSquare = (Grid.Cells.Count(c => c.Square == cell.Square && c.Candidates.Contains(candidate) == true) == 1);

                    if (inOnlyOneColumn || inOnlyOneRow || inOnlyOneSquare)
                    {
                        Grid.AssignValueToCell(cell.Row, cell.Col, candidate, false);
                        ++AssignedWithSinglePossibleLocation;
                        return true;
                    }
                }
            }

            return false;
        }
        #endregion Assign candidates to cells

        #region Remove Candidates
        /// <summary>
        /// Given square, row 1-3, value X.  if X is a candidate in rows 1 AND 2 but NOT in 3, X cannot live on that grid row in other Squares
        /// Square 5 row 3 = Grid Row 6)
        /// </summary>
        /// <summary>
        /// Find sets of candidates that match in 2 cells.
        /// Example: For cell(candidates): 1(4,9), 3(4,9) --Where 4 and 9 are not candidates in any other cells.
        /// Remove all other candidates from these cells.
        /// </summary>
        /// <returns></returns>
        public void FindHiddenPairsBySquare()
        {
            // Square Premeptive
            for (var index = 1; index <= 9; index++)
            {
                var valueLocations = new Dictionary<int, List<int>>();

                /// 'index' is a square index
                valueLocations = Grid.ValueLocationsBySquare(index);
                FindHiddenPairs(PremptiveSetType.Square, index, valueLocations);

                // 'index' is a row number
                valueLocations = Grid.ValueLocationsByRow(index);
                FindHiddenPairs(PremptiveSetType.Row, index, valueLocations);

                // 'index' is a col number
                valueLocations = Grid.ValueLocationsByCol(index);
                FindHiddenPairs(PremptiveSetType.Col, index, valueLocations);
            }
        }

        /// <summary>
        /// Find matces or HIDDEN PAIRS (in a row, col, or square)
        /// Find pairs of values that have 2 identical locations
        /// Example: Given Values(with locations): 1(1,3); 2(4,5); 3(1,4,5); 4(1,3)
        ///     Would match values 1(1,3) and 4(1,3) as a pair 
        /// </summary>
        private void FindHiddenPairs(PremptiveSetType setType, int index, Dictionary<int, List<int>> valueLocations)
        {
            // valueLocations
            //   For the following calculations, we need grid data as Value/location NOT Candidate/Cell
            //   For each value1 (1-9), possible locations = List<int>
            //   Get valueLocations Dictionary<int<int>> (4:1,2; 5:7,8; 7:1,2)

            // Value1: For each value with ONLY two locations (NOT candidates)
            foreach (var value1 in valueLocations.Where(v => v.Value.Count() == 2))
            {
                // For each value2 with ONLY two locations and not == value1 
                // The '>' below prevents evaluating same values twice: (values 1,2 and 2,1)
                foreach (var value2 in valueLocations.Where(v => v.Value.Count() == 2 && v.Key > value1.Key))
                {
                    // Does value1.locations == value2.locations?
                    if (value1.Value.All(value2.Value.Contains) && value1.Value.Count == value2.Value.Count)
                    {
                        Log.Debug($"Match found in {setType.ToString()} {index} for values: {value1.Key},{value2.Key}");
                        Log.Verbose($"  Value1(locations): {value1.Key}({string.Join(",", value1.Value)})");
                        Log.Verbose($"  Value2(locations): {value2.Key}({string.Join(",", value2.Value)})");

                        // Get list of values (1,4) and list of values (1,4)
                        var values = new List<int> { value1.Key, value2.Key };
                        var locations = new List<int> { value1.Value[0], value1.Value[1] };

                        // Now that I know that, what do I do????

                        if (setType == PremptiveSetType.Square)
                        {
                            HandleHiddenPairInSquare(index, values, locations);
                        }
                        else if (setType == PremptiveSetType.Row)
                        {
                            HandleHiddenPairInRow(index, values, locations);
                        }
                        else if (setType == PremptiveSetType.Col)
                        {
                            HandleHiddenPairInCol(index, values, locations);
                        }
                    }
                }
            }
            // Clear as mud?  :O
        }

        private void HandleHiddenPairInSquare(int square, List<int> values, List<int> squareLocations)
        {
            int squareCandidates = Grid.Cells.Where(c => c.Square == square).Sum(c => c.Candidates.Count());

            // Clear other candidates in these two cells
            Log.Verbose($"Clear other candidates in two matching cells");

            Log.Verbose($"  Existing candidates at {square}-{squareLocations[0]}: {string.Join(",", Grid.CellBySquareIndex(square, squareLocations[0]).Candidates)}");
            Grid.CellBySquareIndex(square, squareLocations[0]).Candidates = values;

            Log.Verbose($"  Existing candidates at {square}-{squareLocations[1]}: {string.Join(",", Grid.CellBySquareIndex(square, squareLocations[1]).Candidates)}");
            Grid.CellBySquareIndex(square, squareLocations[1]).Candidates = values;

            // Log if candidates removed. 
            int removedCandidates = squareCandidates - Grid.Cells.Where(c => c.Square == square).Sum(c => c.Candidates.Count());
            if (removedCandidates > 0)
            {
                Log.Verbose($"  Removed {removedCandidates} candidates in square {square}");
            }
        }

        private void HandleHiddenPairInRow(int row, List<int> values, List<int> cols)
        {
            int rowCandidates = Grid.Cells.Where(c => c.Row == row).Sum(c => c.Candidates.Count());

            // Clear other candidates in these two cells
            Log.Verbose($"Clear other candidates in two matching cells");

            Log.Verbose($"  Existing candidates at {row}-{cols[0]}: {string.Join(",", Grid.Cell(cols[0], row).Candidates)}");
            Grid.Cell(row, cols[0]).Candidates = values;
            Log.Verbose($"  Existing candidates at {row}-{cols[1]}: {string.Join(",", Grid.Cell(cols[1], row).Candidates)}");
            Grid.Cell(row, cols[1]).Candidates = values;

            // Log if candidates removed. 
            int removedCandidates = rowCandidates - Grid.Cells.Where(c => c.Row == row).Sum(c => c.Candidates.Count());
            if (removedCandidates > 0)
            {
                Log.Verbose($"  Removed {removedCandidates} candidates in row {row}");
            }
        }

        private void HandleHiddenPairInCol(int col, List<int> values, List<int> rows)
        {
            int rowCandidates = Grid.Cells.Where(c => c.Col == col).Sum(c => c.Candidates.Count());

            // Clear other candidates in these two cells
            Log.Verbose($"Clear other candidates in two matching cells");

            Log.Verbose($"  Existing candidates at {col}-{rows[0]}: {string.Join(",", Grid.Cell(rows[0], col).Candidates)}");
            Grid.Cell(rows[0], col).Candidates = values;
            Log.Verbose($"  Existing candidates at {col}-{rows[1]}: {string.Join(",", Grid.Cell(rows[1], col).Candidates)}");
            Grid.Cell(rows[1], col).Candidates = values;

            // Log if candidates removed. 
            int removedCandidates = rowCandidates - Grid.Cells.Where(c => c.Col == col).Sum(c => c.Candidates.Count());
            if (removedCandidates > 0)
            {
                Log.Verbose($"  Removed {removedCandidates} candidates in col {col}");
            }
        }

        public int RemovePointedPairCandidates()
        {
            Log.Debug("Remove candidates by row and column...");
            int removedCandidates = 0;

            // Iterate trough squares, values
            for (int square = 1; square <= 9; square++)
            {
                for (int valueX = 1; valueX <= 9; valueX++)
                {
                    removedCandidates += RemovePointedPairsInOneRow(square, valueX);
                    removedCandidates += RemovePointedPairsInOneCol(square, valueX);
                }
            }
            Log.Information($"Remove candidates by row and column removed {removedCandidates} candidates");
            return removedCandidates;
        }

        /// <summary>
        /// Remove 
        /// /// </summary>
        /// <param name="square"></param>
        /// <param name="valueX"></param>
        /// <returns></returns>
        private int RemovePointedPairsInOneRow(int square, int valueX)
        {
            /// Example: Candidate 2 is only in Column 1
            /// (1 *2)    (3 4)  (4 5)
            /// (*2 4 8)  (4)    (8 9)
            /// (1 *2 9)  (8)    (7)
            int removedCandidates = 0;
            // For a square, does square row (1, 2, 3) have any of candidate X in them?
            bool candidateInRow1 = (Grid.Cells.Count(c => c.Square == square && c.SquareRow == 1 && c.Candidates.Contains(valueX)) > 0); // Has candidates in row 1
            bool candidateInRow2 = (Grid.Cells.Count(c => c.Square == square && c.SquareRow == 2 && c.Candidates.Contains(valueX)) > 0); // Has candidates in row 1
            bool candidateInRow3 = (Grid.Cells.Count(c => c.Square == square && c.SquareRow == 3 && c.Candidates.Contains(valueX)) > 0); // Has candidates in row 1

            if (square == 7 && valueX == 4 & this.solveIteration == 6)
            {
                Log.Verbose("Debug Stop");
            }

            // If Candidate X is in row 1 and 2, but not 3, X cannot be a candidate in any of that grid row (i.e. square 5 row 3 = grid row 6)
            if (candidateInRow1 && !candidateInRow2 && !candidateInRow3)
            {
                Log.Verbose($"  For square {square}, candidate {valueX}: in row1, !row2, !row3");
                removedCandidates += Grid.RemoveCandidatesInRow(GridCell.GridRow(square, 1), valueX, square);
            }
            else if (!candidateInRow1 && candidateInRow2 && !candidateInRow3)
            {
                Log.Verbose($"  For square {square}, candidate {valueX}: in !row1, row2, !row3");
                removedCandidates += Grid.RemoveCandidatesInRow(GridCell.GridRow(square, 2), valueX, square);
            }
            else if (!candidateInRow1 && !candidateInRow2 && candidateInRow3)
            {
                Log.Verbose($"  For square {square}, candidate {valueX}: in !row1, !row2, row3");
                removedCandidates += Grid.RemoveCandidatesInRow(GridCell.GridRow(square, 3), valueX, square);
            }

            return removedCandidates;
        }

        private int RemovePointedPairsInOneCol(int square, int valueX)
        {
            int removedCandidates = 0;

            // For a square, does square col (1, 2, 3) have any of candidate X in them?
            bool candidateInCol1 = (Grid.Cells.Count(c => c.Square == square && c.SquareCol == 1 && c.Candidates.Contains(valueX)) > 0); // Has candidates in row 1
            bool candidateInCol2 = (Grid.Cells.Count(c => c.Square == square && c.SquareCol == 2 && c.Candidates.Contains(valueX)) > 0); // Has candidates in row 1
            bool candidateInCol3 = (Grid.Cells.Count(c => c.Square == square && c.SquareCol == 3 && c.Candidates.Contains(valueX)) > 0); // Has candidates in row 1

            // If Candidate X is in col 1 and 2, but not 3, X cannot be a candidate in any of that grid col (i.e. square 5 col 3 = grid col 6)
            if (!candidateInCol1 && !candidateInCol2 && candidateInCol3)
            {
                Log.Verbose($"  Candidate {valueX} square {square}: !col1, !col2, col3");
                removedCandidates += Grid.RemoveCandidatesInCol(GridCell.GridCol(square, 3), valueX, square);
            }
            else if (candidateInCol1 && !candidateInCol2 && !candidateInCol3)
            {
                Log.Verbose($"  Candidate {valueX} square {square}: col1, !col2, !col3");
                removedCandidates += Grid.RemoveCandidatesInCol(GridCell.GridCol(square, 1), valueX, square);
            }
            else if (!candidateInCol1 && candidateInCol2 && !candidateInCol3)
            {
                Log.Verbose($"  Candidate {valueX} square {square}: !col1, col2, !col3");
                removedCandidates += Grid.RemoveCandidatesInCol(GridCell.GridCol(square, 2), valueX, square);
            }

            return removedCandidates;
        }

        #endregion Remove Candidates
    }
}
