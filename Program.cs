using System;
using RPG.Engine;
using RPG.UI;
using Spectre.Console;

namespace RPG
{
    class Program
    {
        static void Main(string[] args)
        {
            // Display the game title and welcome screen
            Console.Clear();
            Console.CursorVisible = false;
            
            AnsiConsole.Write(
                new FigletText("RPG Adventure")
                    .Centered()
                    .Color(Color.Gold1));
            
            AnsiConsole.MarkupLine("\n[bold yellow]Welcome to the Dungeon Explorer RPG![/]");
            AnsiConsole.MarkupLine("\nExplore dungeons, defeat enemies, collect items, and advance through multiple levels!");
            AnsiConsole.MarkupLine("Find shops to buy better gear and complete quests for rewards.");
            AnsiConsole.MarkupLine("\n[bold cyan]Game Symbols:[/]");
            AnsiConsole.MarkupLine("[yellow]@[/]: Player | [red]E[/]: Enemy | [blue]i[/]: Item | [magenta]>[/]: Exit | [yellow]$[/]: Shop");
            AnsiConsole.MarkupLine("\n[bold cyan]Controls:[/]");
            AnsiConsole.MarkupLine(InputHandler.GetAllControls());
            
            // Ask if player wants to see the full help screen
            bool showHelp = AnsiConsole.Confirm("Would you like to see the detailed help screen?");
            if (showHelp)
            {
                var renderer = new GameRenderer();
                renderer.ShowHelpScreen();
            }
            
            AnsiConsole.MarkupLine("\n[gray]Press any key to start your adventure...[/]");
            Console.ReadKey(true);
            
            // Clear the console completely before starting the game
            Console.Clear();
            
            // Create and initialize the game engine
            var game = new GameEngine();
            game.Initialize();
            
            // Run the game loop
            game.Run();
        }
    }
}
