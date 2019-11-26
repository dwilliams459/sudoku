using System;
using System.Collections.Generic;
using System.IO;
using Newtonsoft.Json;
using Serilog;

namespace Sudoku
{
    class Program
    {
        static void Main(string[] args)
        {
            try
            {
                File.Delete("./output.txt");

                Log.Logger = new LoggerConfiguration()
                    .WriteTo.File("./output.txt", outputTemplate: "{Message:lj}{NewLine}")
                    .MinimumLevel.Verbose()
                    .CreateLogger();

                Log.Information($"Starting Sudoku {DateTime.Now}");
                Log.Information("====================================================");
                Log.Information(" ");

                var puzzle = new Sudoku();

                string jsonFile = null;
                if (args != null && args.Length > 0)
                {
                    jsonFile = args[0];
                }

                //LoadGrid(puzzle, jsonFile);
                if (LoadFromGridJson(puzzle, jsonFile) == true)
                {
                    puzzle.Solve();
                }

            }
            catch (Exception ex)
            {
                Log.Information(ex.Message);
                Log.Information(ex.StackTrace);
            }
            finally
            {
                Log.CloseAndFlush();
            }
        }

        public static void LoadGrid(Sudoku puzzle, string jsonFile = "./input.json")
        {
            string json = File.ReadAllText(jsonFile);
            var valueLocations = JsonConvert.DeserializeObject<List<ValueLocation>>(json);

            puzzle.Grid.AddLocations(valueLocations);
        }

        public static bool LoadFromGridJson(Sudoku puzzle, string jsonFile = "")
        {
            jsonFile = (string.IsNullOrWhiteSpace(jsonFile)) ? "./hard1.json" : jsonFile;

            string json = string.Empty;
            List<GridRowValues> valueLocations = new List<GridRowValues>();
            try
            {
                string jsonFilePath = jsonFile;
                if (!File.Exists(jsonFilePath))
                { 
                    jsonFilePath = Path.Combine("grids", jsonFile);
                }

                json = File.ReadAllText(jsonFilePath);
                valueLocations = JsonConvert.DeserializeObject<List<GridRowValues>>(json);
            }
            catch (Exception ex)
            {
                Log.Error($"Error reading and parsing input file {jsonFile}: {ex.Message}");
                return false;
            }

            puzzle.Grid.AddLocations(valueLocations);
            return true;
        }
    }
}
