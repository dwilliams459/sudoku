using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
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

            iterations = (iterations == 0) ? 10 : iterations;
            for (int i = 1; i <= iterations; i++)
            {
                Log.Information("");
                Log.Information("Iteration: " + i);
                Log.Information("====================================================");

                Log.Debug("Marking Canditates");
                Log.Debug("============");

                SetCandidates();
                Grid.PrintGridWithCandiates();

                int removedCandidates = 0;
                int removeCandidiateIteration = 0;
                do
                {
                    removeCandidiateIteration++;
                    int candidateCount = Grid.Cells.Sum(c => c.Candidates.Count);

                    RemoveCandiatesByRowAndColumn();
                    ManagePreemptiveSets();
                    Grid.PrintGridWithCandiates();

                    removedCandidates = candidateCount - Grid.Cells.Sum(c => c.Candidates.Count);
                    Log.Debug($"Removed {removedCandidates} candidates (iteration {removeCandidiateIteration})");
                } while (removedCandidates > 0 && removeCandidiateIteration < 5);

                Log.Debug($"Assign candiates with Single Possible Location..");
                Grid.AssignedCells = 0;
                AssignedWithSinglePossibleLocation = 0;
                //AssignCandidatesWithSinglePossibleLocationInSquare();
                //AssignCandidatesWithSinglePossibleLocationInRow();
                //AssignCandidatesWithSinglePossibleLocationInCol();
                AssignCandidatesWithSinglePossibleLocation();

                Log.Debug($"SinglePossibleLocation set values to unique candidates: Assigned {AssignedWithSinglePossibleLocation} cells");
                Log.Debug("");

                Log.Information($"Assigned {Grid.AssignedCells} cells");
                Log.Information($"Total cells with assigned values: {Grid.Cells.Count(c => c.Value != null)}");
                Grid.PrintGridWithCandiates(i);
                Grid.PrintGrid(i);

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

        /// <summary>
        /// For each cell, for each candidate (1-9): candidate is not already assigned in the row, column or square
        /// </summary>
        public void SetCandidates()
        {
            Log.Debug("Seting Candidates...");
            // Reset all candidates
            Grid.ClearCandidates();

            // Mark candiates.  
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

        /// <summary>
        /// Given square, row 1-3, value X.  if X is a candidate in rows 1 AND 2 but NOT in 3, X cannot live on that grid row in other Squares
        /// Square 5 row 3 = Grid Row 6)
        /// </summary>
        public int RemoveCandiatesByRowAndColumn()
        {
            Log.Debug("Remove candiates by row and column...");
            int removedCandiates = 0;

            // Iterate trough squares, values
            for (int square = 1; square <= 9; square++)
            {
                for (int valueX = 1; valueX <= 9; valueX++)
                {
                    removedCandiates += RemoveIfCanidateOnlyInOneRow(square, valueX);
                    removedCandiates += RemoveIfCanidateOnlyInOneCol(square, valueX);
                }
            }
            Log.Information($"Remove candiates by row and column removed {removedCandiates} candidates");
            return removedCandiates;
        }

        private int RemoveIfCanidateOnlyInOneRow(int square, int valueX)
        {
            int removedCandiates = 0;
            // For a square, does square row (1, 2, 3) have any of candidate X in them?
            bool candidateInRow1 = (Grid.Cells.Count(c => c.Square == square && c.SquareRow == 1 && c.Candidates.Contains(valueX)) > 0); // Has candidates in row 1
            bool candidateInRow2 = (Grid.Cells.Count(c => c.Square == square && c.SquareRow == 2 && c.Candidates.Contains(valueX)) > 0); // Has candidates in row 1
            bool candidateInRow3 = (Grid.Cells.Count(c => c.Square == square && c.SquareRow == 3 && c.Candidates.Contains(valueX)) > 0); // Has candidates in row 1

            // If Candiate X is in row 1 and 2, but not 3, X cannot be a candidate in any of that grid row (i.e. square 5 row 3 = grid row 6)
            if (candidateInRow1 && !candidateInRow2 && !candidateInRow3)
            {
                Log.Verbose($"  For square {square}, candidate {valueX}: in row1, !row2, !row3");
                removedCandiates += Grid.RemoveCandidatesInRow(GridCell.GridRow(square, 1), valueX, square);
            }
            else if (!candidateInRow1 && candidateInRow2 && !candidateInRow3)
            {
                Log.Verbose($"  For square {square}, candidate {valueX}: in !row1, row2, !row3");
                removedCandiates += Grid.RemoveCandidatesInRow(GridCell.GridRow(square, 2), valueX, square);
            }
            else if (!candidateInRow1 && !candidateInRow2 && candidateInRow3)
            {
                Log.Verbose($"  For square {square}, candidate {valueX}: in !row1, !row2, row3");
                removedCandiates += Grid.RemoveCandidatesInRow(GridCell.GridRow(square, 3), valueX, square);
            }

            return removedCandiates;
        }

        private int RemoveIfCanidateOnlyInOneCol(int square, int valueX)
        {
            int removedCandiates = 0;

            // For a square, does square col (1, 2, 3) have any of candidate X in them?
            bool candidateInCol1 = (Grid.Cells.Count(c => c.Square == square && c.SquareCol == 1 && c.Candidates.Contains(valueX)) > 0); // Has candidates in row 1
            bool candidateInCol2 = (Grid.Cells.Count(c => c.Square == square && c.SquareCol == 2 && c.Candidates.Contains(valueX)) > 0); // Has candidates in row 1
            bool candidateInCol3 = (Grid.Cells.Count(c => c.Square == square && c.SquareCol == 3 && c.Candidates.Contains(valueX)) > 0); // Has candidates in row 1

            // If Candiate X is in col 1 and 2, but not 3, X cannot be a candidate in any of that grid col (i.e. square 5 col 3 = grid col 6)
            if (!candidateInCol1 && !candidateInCol2 && candidateInCol3)
            {
                Log.Verbose($"  Candidate {valueX} square {square}: !col1, !col2, col3");
                removedCandiates += Grid.RemoveCandidatesInCol(GridCell.GridCol(square, 3), valueX, square);
            }
            else if (candidateInCol1 && !candidateInCol2 && !candidateInCol3)
            {
                Log.Verbose($"  Candidate {valueX} square {square}: col1, !col2, !col3");
                removedCandiates += Grid.RemoveCandidatesInCol(GridCell.GridCol(square, 1), valueX, square);
            }
            else if (!candidateInCol1 && candidateInCol2 && !candidateInCol3)
            {
                Log.Verbose($"  Candidate {valueX} square {square}: !col1, col2, !col3");
                removedCandiates += Grid.RemoveCandidatesInCol(GridCell.GridCol(square, 2), valueX, square);
            }

            return removedCandiates;
        }

        private int RemoveIfCanidateOnlyInOneSquare(int square, int valueX)
        {
            int removedCandiates = 0;

            // For a square, does square col (1, 2, 3) have any of candidate X in them?
            bool candidateInCol1 = (Grid.Cells.Count(c => c.Square == square && c.SquareCol == 1 && c.Candidates.Contains(valueX)) > 0); // Has candidates in row 1
            bool candidateInCol2 = (Grid.Cells.Count(c => c.Square == square && c.SquareCol == 2 && c.Candidates.Contains(valueX)) > 0); // Has candidates in row 1
            bool candidateInCol3 = (Grid.Cells.Count(c => c.Square == square && c.SquareCol == 3 && c.Candidates.Contains(valueX)) > 0); // Has candidates in row 1

            // If Candiate X is in col 1 and 2, but not 3, X cannot be a candidate in any of that grid col (i.e. square 5 col 3 = grid col 6)
            if (!candidateInCol1 && !candidateInCol2 && candidateInCol3)
            {
                Log.Verbose($"  Candidate {valueX} square {square}: !col1, !col2, col3");
                removedCandiates += Grid.RemoveCandidatesInCol(GridCell.GridCol(square, 3), valueX, square);
            }
            else if (candidateInCol1 && !candidateInCol2 && !candidateInCol3)
            {
                Log.Verbose($"  Candidate {valueX} square {square}: col1, !col2, !col3");
                removedCandiates += Grid.RemoveCandidatesInCol(GridCell.GridCol(square, 1), valueX, square);
            }
            else if (!candidateInCol1 && candidateInCol2 && !candidateInCol3)
            {
                Log.Verbose($"  Candidate {valueX} square {square}: !col1, col2, !col3");
                removedCandiates += Grid.RemoveCandidatesInCol(GridCell.GridCol(square, 2), valueX, square);
            }

            return removedCandiates;
        }

        /// <summary>
        /// Value X is a candidate in only one cell of a square.  Assign X to that cell.
        /// </summary>
        public void AssignCandidatesWithSinglePossibleLocationInSquare()
        {
            Log.Debug("AssignCandidatesWithSinglePossibleLocationInSquare...");
            for (int square = 1; square <= 9; square++)  // For each square
            {
                for (int candidate = 1; candidate <= 9; candidate++) // For each (possible) candidate value (1-9)
                {
                    // If count of possible locations of this candiate in square is only 1
                    var countOfPossibleLocations = Grid.Cells.Count(c => c.Square == square && c.Candidates.Contains(candidate));
                    if (countOfPossibleLocations == 1)
                    {
                        // Assign Candidate
                        Log.Verbose($"Only one possible location in square {square} for candidate {candidate}");
                        if (square == 5 && candidate == 9)
                        {
                            Grid.PrintGridWithCandiates();
                            Log.Verbose($"Square {square}, candidate {candidate}, count of possible locations: {countOfPossibleLocations}");

                        }
                        var targetCell = Grid.Cells.Where(c => c.Square == square && c.Candidates.Contains(candidate)).FirstOrDefault();
                        Grid.AssignValueToCell(targetCell.Row, targetCell.Col, candidate);
                        AssignedWithSinglePossibleLocation++;
                    }
                }
            }
        }

        /// <summary>
        /// Value X is a candidate in only one cell of a row.  Assign X to that cell.
        /// </summary>
        public void AssignCandidatesWithSinglePossibleLocationInRow()
        {
            Log.Debug("AssignCandidatesWithSinglePossibleLocationInRow...");
            for (int row = 1; row <= 9; row++)  // For each row
            {
                for (int candidate = 1; candidate <= 9; candidate++) // For each (possible) candidate value (1-9)
                {
                    // If count of possible locations of this candiate in row is only 1
                    var countOfPossibleLocations = Grid.Cells.Count(c => c.Row == row && c.Candidates.Contains(candidate));
                    if (countOfPossibleLocations == 1)
                    {
                        Log.Verbose($"One possible location in row {row} for candidate {candidate}");
                        // Assign Candidate
                        var targetCell = Grid.Cells.Where(c => c.Row == row && c.Candidates.Contains(candidate)).FirstOrDefault();
                        Grid.AssignValueToCell(targetCell.Row, targetCell.Col, candidate);
                        AssignedWithSinglePossibleLocation++;
                    }
                }
            }
        }

        /// <summary>
        /// Value X is a candidate in only one cell of a col.  Assign X to that cell.
        /// </summary>
        public void AssignCandidatesWithSinglePossibleLocationInCol()
        {
            Log.Debug("SetCandidatesWithSinglePossibleLocationInCol...");
            for (int col = 1; col <= 9; col++)  // For each col
            {
                for (int candidate = 1; candidate <= 9; candidate++) // For each (possible) candidate value (1-9)
                {
                    // If count of possible locations of this candiate in col is only 1
                    var countOfPossibleLocations = Grid.Cells.Count(c => c.Col == col && c.Candidates.Contains(candidate));
                    if (countOfPossibleLocations == 1)
                    {
                        Log.Verbose($"One possible location in col {col} for candidate {candidate}");
                        // Assign Candidate
                        var targetCell = Grid.Cells.Where(c => c.Col == col && c.Candidates.Contains(candidate)).FirstOrDefault();
                        Grid.AssignValueToCell(targetCell.Row, targetCell.Col, candidate);
                        AssignedWithSinglePossibleLocation++;
                    }
                }

            }
        }

        public void AssignCandidatesWithSinglePossibleLocation()
        {
            Log.Debug("Assigning candidates with single possible location...");
            foreach (GridCell cell in Grid.Cells)
            {
                foreach (int candidate in cell.Candidates)
                {
                    var targetCells = (Grid.Cells.Where(c => c.Col == cell.Col && c.Candidates.Contains(candidate))
                        .Where(c => c.Col == cell.Col && c.Candidates.Contains(candidate))
                        .Where(c => c.Col == cell.Col && c.Candidates.Contains(candidate)));
                    if (targetCells != null && targetCells.Count() == 1)
                    {
                        var targetCell = targetCells.FirstOrDefault();
                        Grid.AssignValueToCell(targetCell.Row, targetCell.Col, candidate);
                        AssignedWithSinglePossibleLocation++;
                    }
                }
            }
        }

        /// <summary>
        /// Find sets of candidates that match in 2 cells.
        /// Example: For cell(candidates): 1(4,9), 3(4,9) --Where 4 and 9 are not candiates in any other cells.
        /// Remove all other candidates from these cells.
        /// </summary>
        /// <returns></returns>
        public void ManagePreemptiveSets()
        {
            // Square Premeptive
            for (var index = 1; index <= 9; index++)
            {
                var valueLocations = new Dictionary<int, List<int>>();

                /// 'index' is a square index
                valueLocations = Grid.ValueLocationsBySquare(index);
                ManagePreemptiveSetPairs(PremptiveSetType.Square, index, valueLocations);

                // 'index' is a row number
                valueLocations = Grid.ValueLocationsByRow(index);
                ManagePreemptiveSetPairs(PremptiveSetType.Row, index, valueLocations);

                // 'index' is a col number
                valueLocations = Grid.ValueLocationsByCol(index);
                ManagePreemptiveSetPairs(PremptiveSetType.Col, index, valueLocations);
            }
        }


        /// <summary>
        /// Find matces or HIDDEN PAIRS (in a row, col, or square)
        /// Find pairs of values that have 2 identical locations
        /// Example: Given Values(with locations): 1(1,3); 2(4,5); 3(1,4,5); 4(1,3)
        ///     Would match values 1(1,3) and 4(1,3) as a pair 
        /// </summary>
        /// <param name="index"></param>
        private void ManagePreemptiveSetPairs(PremptiveSetType setType, int index, Dictionary<int, List<int>> valueLocations)
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

            // // Clear these two candidates out of other cells in this square
            // Log.Verbose($"Clear these two candidates out of other cells in this square");
            // foreach(GridCell cell in Grid.Cells.Where(c => c.Square == square && !squareLocations.Contains(c.SquareIndex)))
            // {
            //     // If location has candidate, remove
            //     Log.Verbose($"  Candidates at {square}-{cell.SquareIndex}: {string.Join(",", cell.Candidates)}");
            //     cell.Candidates = cell.Candidates.Except(values).ToList();
            // }

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

            // Clear these two candidates out of other cells in this row
            // foreach(GridCell cell in Grid.Cells.Where(c => c.Row == row && !cols.Contains(c.Col)))
            // {
            //     // If location has candidate, remove
            //     cell.Candidates = cell.Candidates.Except(values).ToList();
            // }

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

            // // Clear these two candidates out of other cells in this column
            // foreach(GridCell cell in Grid.Cells.Where(c => c.Col == col && !rows.Contains(c.Row)))
            // {
            //     // If location has candidate, remove
            //     cell.Candidates = cell.Candidates.Except(values).ToList();
            // }

            // Log if candidates removed. 
            int removedCandidates = rowCandidates - Grid.Cells.Where(c => c.Col == col).Sum(c => c.Candidates.Count());
            if (removedCandidates > 0)
            {
                Log.Verbose($"  Removed {removedCandidates} candidates in col {col}");
            }
        }

    }
}
