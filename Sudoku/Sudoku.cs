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
                Grid.PrintGrid();
                Grid.PrintGridWithCandiates();

                RemoveCandiatesByRowAndColumn();
                Grid.PrintGridWithCandiates();

                ManagePreemptiveSets();

                Log.Debug($"Assign candiates with Single Possible Location..");
                Grid.AssignedCells = 0;
                AssignedWithSinglePossibleLocation = 0;
                AssignCandidatesWithSinglePossibleLocationInSquare();
                AssignCandidatesWithSinglePossibleLocationInRow();
                AssignCandidatesWithSinglePossibleLocationInCol();

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
                    //removedCandiates += RemoveIfCanidateInExactlyTwoCols(square, valueX);
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

        // private int RemoveIfCanidateOnlyInTwoRows(int square, int valueX)
        // {
        //     int removedCandiates = 0;
        //     // For a square, does square row (1, 2, 3) have any of candidate X in them?
        //     bool candidateInRow1 = (Grid.Cells.Count(c => c.Square == square && c.SquareRow == 1 && c.Candidates.Contains(valueX)) > 0); // Has candidates in row 1
        //     bool candidateInRow2 = (Grid.Cells.Count(c => c.Square == square && c.SquareRow == 2 && c.Candidates.Contains(valueX)) > 0); // Has candidates in row 1
        //     bool candidateInRow3 = (Grid.Cells.Count(c => c.Square == square && c.SquareRow == 3 && c.Candidates.Contains(valueX)) > 0); // Has candidates in row 1

        //     // If Candiate X is in row 1 and 2, but not 3, X cannot be a candidate in any of that grid row (i.e. square 5 row 3 = grid row 6)
        //     if (candidateInRow1 && !candidateInRow2 && !candidateInRow3)
        //     {
        //         Log.Verbose($"  For square {square}, candidate {valueX}: in !row1, row2, row3");
        //         removedCandiates += Grid.RemoveCandidatesInRow(GridCell.RowFromSquareRow(square, 1), valueX);
        //     }
        //     else if (!candidateInRow1 && candidateInRow2 && !candidateInRow3)
        //     {
        //         Log.Verbose($"  For square {square}, candidate {valueX}: in row1, !row2, row3");
        //         removedCandiates += Grid.RemoveCandidatesInRow(GridCell.RowFromSquareRow(square, 2), valueX);
        //     }
        //     else if (!candidateInRow1 && !candidateInRow2 && candidateInRow3)
        //     {
        //         Log.Verbose($"  For square {square}, candidate {valueX}: in row1, row2, !row3");
        //         removedCandiates += Grid.RemoveCandidatesInRow(GridCell.RowFromSquareRow(square, 3), valueX);
        //     }

        //     return removedCandiates;
        // }

        // private int RemoveIfCanidateInExactlyTwoCols(int square, int valueX)
        // {
        //     int removedCandiates = 0;

        //     // For a square, does square col (1, 2, 3) have any of candidate X in them?
        //     bool candidateInCol1 = (Grid.Cells.Count(c => c.Square == square && c.SquareCol == 1 && c.Candidates.Contains(valueX)) > 0); // Has candidates in row 1
        //     bool candidateInCol2 = (Grid.Cells.Count(c => c.Square == square && c.SquareCol == 2 && c.Candidates.Contains(valueX)) > 0); // Has candidates in row 1
        //     bool candidateInCol3 = (Grid.Cells.Count(c => c.Square == square && c.SquareCol == 3 && c.Candidates.Contains(valueX)) > 0); // Has candidates in row 1

        //     // If Candiate X is in col 1 and 2, but not 3, X cannot be a candidate in any of that grid col (i.e. square 5 col 3 = grid col 6)
        //     if (candidateInCol1 && candidateInCol2 && !candidateInCol3)
        //     {
        //         Log.Verbose($"  Candidate {valueX} square {square}: col1, col2, !col3");
        //         removedCandiates += Grid.RemoveCandidatesInCol(GridCell.ColFromSquareCol(square, 3), valueX);
        //     }
        //     else if (!candidateInCol1 && candidateInCol2 && candidateInCol3)
        //     {
        //         Log.Verbose($"  Candidate {valueX} square {square}: !col1, col2, col3");
        //         removedCandiates += Grid.RemoveCandidatesInCol(GridCell.ColFromSquareCol(square, 1), valueX);
        //     }
        //     else if (candidateInCol1 && !candidateInCol2 && candidateInCol3)
        //     {
        //         Log.Verbose($"  Candidate {valueX} square {square}: col1, !col2, col3");
        //         removedCandiates += Grid.RemoveCandidatesInCol(GridCell.ColFromSquareCol(square, 2), valueX);
        //     }

        //     return removedCandiates;
        // }

        private int RemoveIfCanidateInExactlyOneCol(int square, int valueX)
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

        /// <summary>
        /// Find sets of candidates that match in 2 > cells.
        /// Example: SQ1: 4, 9. SQ2: 4, 9. --Where 4 and 9 not candiates in any other cells.
        /// Remove all other candidates from these cells.
        /// </summary>
        /// <returns></returns>
        public void ManagePreemptiveSets()
        {
            for (var square = 1; square <= 9; square++)
            {
                Grid.PrintSquareGridWithCandiates(square);

                // For the following calculations, we need grid data as Value/location NOT Candidate/Cell
                // Grid uses SquareIndex, NOT row/column
                // For each value1 (1-9), possible locations = List<int>
                // Get valueLocations Dictionary<int<int>> (4:1,2; 5:7,8; 7:1,2)
                var valLocs = Grid.ValueLocations(square);
                // For each value1 (1-9)

                /// >> HIDDEN PAIRS <<
                // Find pairs of values that have 2 identical locations
                // Example: Given Values(with locations): 1(1,3); 2(4,5); 3(1,4,5); 4(1,3)
                //     Would match values 1(1,3) and 4(1,3) as a pair 

                // Value1: For each value with ONLY two locations (NOT candidates)
                foreach (var value1 in valLocs.Where(v => v.Value.Count() == 2))
                {
                    // For each value2 with ONLY two locations and not == value1 
                    // The '>' below prevents evaluating same values twice: (values 1,2 and 2,1)
                    foreach (var value2 in valLocs.Where(v => v.Value.Count() == 2 && v.Key > value1.Key))
                    {
                        // Does value1.locations == value2.locations?
                        if (value1.Value.All(value2.Value.Contains) && value1.Value.Count == value2.Value.Count)
                        {
                            Log.Debug($"Match found for value(locations): {value1.Key}.locations contains {value2.Key}.locations");
                            Log.Verbose($"Value1: {value1}.Locations({string.Join(",", value1.Value)})");
                            Log.Verbose($"Value2: {value2}.Locations({string.Join(",", value2.Value)})");

                            // Get list of values (1,4) and list of values (1,4)
                            var values = new List<int> { value1.Key, value2.Key };
                            var locations = new List<int> { value1.Value[0], value1.Value[1] }; 

                            // Now that I know that, what do I do??
                            HandleHiddenPair(square, values, locations);
                        }
                    }
                }
                // Clear as mud?  :O

            }
        }

        private void HandleHiddenPair(int square, List<int> values, List<int> locations)
        {
            Log.Debug($"Handle HiddenPair sqare {square}, values {string.Join(",", values)}, locations {string.Join(",", locations)}");
            // value 1 and value 2 are in the same row, remove other values from candidates
            // for all cells in square (other than location 1, location 2)
            // foreach (GridCell cell in Grid.Cells.Where(c => c.Square == square
            //             && c.SquareIndex != locations[0] && c.SquareIndex != locations[1]))
            // {
            //     // Remove value 1, value 2
            //     cell.Candidates.Remove(value1);
            //     cell.Candidates.Remove(value2);
            // }

            // // For both locations, the only candidates should be value1 and value2.
            // var cellLoc1 = Grid.Cells.Where(c => c.Square == square && c.SquareIndex == locations[0]).FirstOrDefault();
            // cellLoc1.Candidates = new List<int> { value1, value2 };
            // var cellLoc2 = Grid.Cells.Where(c => c.Square == square && c.SquareIndex == locations[1]).FirstOrDefault();
            // cellLoc2.Candidates = new List<int> { value1, value2 };
        }

    }
}
