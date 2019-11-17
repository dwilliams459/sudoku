using System;
using System.Collections.Generic;

namespace Soduko
{
    class Program
    {
        static void Main(string[] args)
        {
            try
            {
                var puzzle = new Sudoku();
                SetupPuzzle(puzzle);
                puzzle.Solve();

            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                Console.WriteLine(ex.StackTrace);
            }
        }

        public static void SetupPuzzle(Sudoku puzzle)
        {

            puzzle.Grid.AddValue(1, 3, 2);
            puzzle.Grid.AddValue(1, 5, 8);
            puzzle.Grid.AddValue(1, 8, 4);
            puzzle.Grid.AddValue(1, 9, 6);
            puzzle.Grid.AddValue(2, 2, 4);
            puzzle.Grid.AddValue(2, 6, 7);
            puzzle.Grid.AddValue(3, 3, 3);
            puzzle.Grid.AddValue(3, 6, 5);
            puzzle.Grid.AddValue(3, 9, 8);

            puzzle.Grid.AddValue(4, 4, 3);
            puzzle.Grid.AddValue(4, 6, 6);
            puzzle.Grid.AddValue(4, 7, 2);
            puzzle.Grid.AddValue(5, 1, 7);
            puzzle.Grid.AddValue(5, 5, 2);
            puzzle.Grid.AddValue(5, 9, 1);
            puzzle.Grid.AddValue(6, 3, 5);
            puzzle.Grid.AddValue(6, 4, 1);
            puzzle.Grid.AddValue(6, 6, 8);
            
            puzzle.Grid.AddValue(7, 1, 2);
            puzzle.Grid.AddValue(7, 4, 8);
            puzzle.Grid.AddValue(7, 7, 4);
            puzzle.Grid.AddValue(8, 4, 7);
            puzzle.Grid.AddValue(8, 8, 9);
            puzzle.Grid.AddValue(9, 1, 4);
            puzzle.Grid.AddValue(9, 2, 8);
            puzzle.Grid.AddValue(9, 5, 6);
            puzzle.Grid.AddValue(9, 7, 1);
        }
    }
}
