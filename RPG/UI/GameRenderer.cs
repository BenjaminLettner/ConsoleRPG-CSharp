using System;
using System.Collections.Generic;
using System.Linq;
using RPG.Engine;
using RPG.Models;
using Spectre.Console;

namespace RPG.UI
{
    public class GameRenderer
    {
        private int _lastRenderedWidth;
        private int _lastRenderedHeight;
        
        // Camera settings - adjust for better visibility
        private int _viewportWidth = 40;  // Increased for better horizontal visibility
        private int _viewportHeight = 20; // Increased for better vertical visibility
        
        // Enhanced sprite representations (using Unicode characters)
        private class EntitySprite
        {
            public string TopLeft { get; }
            public string TopRight { get; }
            public string BottomLeft { get; }
            public string BottomRight { get; }
            public string Color { get; }
            
            public EntitySprite(string topLeft, string topRight, string bottomLeft, string bottomRight, string color)
            {
                TopLeft = topLeft;
                TopRight = topRight;
                BottomLeft = bottomLeft;
                BottomRight = bottomRight;
                Color = color;
            }
            
            // Single character sprite constructor
            public EntitySprite(string character, string color)
            {
                TopLeft = character;
                TopRight = character;
                BottomLeft = character;
                BottomRight = character;
                Color = color;
            }
        }
        
        // Sprites for different entities
        private readonly EntitySprite _playerSprite = new EntitySprite("@", "bold yellow");
        private readonly EntitySprite _wallSprite = new EntitySprite("#", "grey");
        private readonly EntitySprite _floorSprite = new EntitySprite(".", "grey");
        private readonly Dictionary<string, EntitySprite> _enemySprites = new Dictionary<string, EntitySprite>
        {
            { "Goblin", new EntitySprite("G", "green") },
            { "Skeleton", new EntitySprite("S", "grey") },
            { "Rat", new EntitySprite("R", "yellow") },
            { "Orc", new EntitySprite("O", "red") },
            { "Undead", new EntitySprite("U", "magenta") },
            { "Demon", new EntitySprite("D", "red") }
        };
        private readonly EntitySprite _itemSprite = new EntitySprite("i", "blue");
        private readonly EntitySprite _exitSprite = new EntitySprite(">", "bold blink magenta");
        private readonly EntitySprite _shopSprite = new EntitySprite("$", "bold yellow");
        
        public GameRenderer()
        {
            _lastRenderedWidth = Console.WindowWidth;
            _lastRenderedHeight = Console.WindowHeight;
        }
        
        public void Render(Player player, List<Enemy> enemies, Dictionary<(int, int), Item> items, 
                          TileType[,] map, List<string> messageLog,
                          (int X, int Y)? exitPosition = null,
                          (int X, int Y)? shopPosition = null,
                          string? exitDirection = null)
        {
            // Check if console window size changed
            if (Console.WindowWidth != _lastRenderedWidth || Console.WindowHeight != _lastRenderedHeight)
            {
                Console.Clear();
                _lastRenderedWidth = Console.WindowWidth;
                _lastRenderedHeight = Console.WindowHeight;
            }
            
            Console.SetCursorPosition(0, 0);
            
            // Create a grid for the map
            var grid = new Grid();
            grid.AddColumn();
            
            // Calculate camera position based on player position
            int cameraX = Math.Max(0, player.X - _viewportWidth / 2);
            int cameraY = Math.Max(0, player.Y - _viewportHeight / 2);
            
            // Ensure camera doesn't go beyond map boundaries
            int mapWidth = map.GetLength(0);
            int mapHeight = map.GetLength(1);
            
            cameraX = Math.Min(cameraX, Math.Max(0, mapWidth - _viewportWidth));
            cameraY = Math.Min(cameraY, Math.Max(0, mapHeight - _viewportHeight));
            
            // Debug info
            var mapInfoPanel = new Panel(new Markup($"[bold]Map Size:[/] {mapWidth}x{mapHeight} | [bold]Camera:[/] ({cameraX},{cameraY}) | [bold]Player:[/] ({player.X},{player.Y})"));
            mapInfoPanel.Border = BoxBorder.None;
            mapInfoPanel.Padding = new Padding(0, 0, 0, 0);
            grid.AddRow(mapInfoPanel);
            
            try 
            {
                // Add the game map to the grid
                var mapPanel = new Panel(RenderMapWithCamera(player, enemies, items, map, cameraX, cameraY, exitPosition, shopPosition));
                mapPanel.Border = BoxBorder.Rounded;
                mapPanel.BorderStyle = new Style(Color.Green);
                mapPanel.Header = new PanelHeader("Dungeon");
                mapPanel.Expand = true;
                grid.AddRow(mapPanel);
                
                // Add player stats
                var statsPanel = new Panel(RenderPlayerStats(player, exitDirection));
                statsPanel.Border = BoxBorder.Rounded;
                statsPanel.BorderStyle = new Style(Color.Gold1);
                statsPanel.Padding = new Padding(1, 0, 1, 0);
                grid.AddRow(statsPanel);
                
                // Add message log
                if (messageLog.Count > 0)
                {
                    var logPanel = new Panel(RenderMessageLog(messageLog));
                    logPanel.Border = BoxBorder.Rounded;
                    logPanel.BorderStyle = new Style(Color.Yellow);
                    logPanel.Header = new PanelHeader("Message Log");
                    grid.AddRow(logPanel);
                }
                
                // Render the grid
                AnsiConsole.Write(grid);
            }
            catch (Exception ex)
            {
                Console.Clear();
                Console.WriteLine($"Error in rendering: {ex.Message}");
                Console.WriteLine($"Stack trace: {ex.StackTrace}");
            }
        }
        
        private Table RenderMapWithCamera(Player player, List<Enemy> enemies, Dictionary<(int, int), Item> items,
                                        TileType[,] map, int cameraX, int cameraY,
                                        (int X, int Y)? exitPosition = null,
                                        (int X, int Y)? shopPosition = null)
        {
            // Calculate the viewport area
            int viewportEndX = Math.Min(cameraX + _viewportWidth, map.GetLength(0));
            int viewportEndY = Math.Min(cameraY + _viewportHeight, map.GetLength(1));
            
            // Ensure we have valid bounds
            if (viewportEndX <= cameraX || viewportEndY <= cameraY)
            {
                // Invalid viewport, create a simple error message
                var errorTable = new Table();
                errorTable.Border = TableBorder.None;
                errorTable.AddColumn("Error");
                errorTable.AddRow("[red]Invalid viewport dimensions[/]");
                return errorTable;
            }
            
            // Create a table for the map (no headers or borders)
            var table = new Table();
            table.Border = TableBorder.None;
            table.Expand = true;
            
            // Add columns to the table - one for each viewport column
            for (int x = cameraX; x < viewportEndX; x++)
            {
                table.AddColumn(new TableColumn("").Centered().NoWrap());
            }
            
            // Add rows with map data
            for (int y = cameraY; y < viewportEndY; y++)
            {
                var rowCells = new string[viewportEndX - cameraX];
                
                for (int x = cameraX; x < viewportEndX; x++)
                {
                    // Basic safety checks
                    if (x < 0 || x >= map.GetLength(0) || y < 0 || y >= map.GetLength(1))
                    {
                        rowCells[x - cameraX] = "[red]#[/]";
                        continue;
                    }
                    
                    try
                    {
                        // Get the entity at this position
                        rowCells[x - cameraX] = RenderEntityAt(x, y, player, enemies, items, map, exitPosition, shopPosition);
                    }
                    catch (Exception)
                    {
                        // Fallback if rendering fails
                        rowCells[x - cameraX] = "[red]?[/]";
                    }
                }
                
                table.AddRow(rowCells);
            }
            
            return table;
        }
        
        private string RenderEntityAt(int x, int y, Player player, List<Enemy> enemies, 
                                   Dictionary<(int, int), Item> items, TileType[,] map,
                                   (int X, int Y)? exitPosition, (int X, int Y)? shopPosition)
        {
            // Safety checks
            if (x < 0 || x >= map.GetLength(0) || y < 0 || y >= map.GetLength(1))
            {
                return "[red]#[/]";
            }
            
            // Determine what entity is at this position and render the appropriate sprite
            
            // Check for player
            if (x == player.X && y == player.Y)
            {
                return "[yellow]@[/]";
            }
            
            // Check for enemies
            foreach (var enemy in enemies)
            {
                if (enemy.IsAlive && enemy.X == x && enemy.Y == y)
                {
                    if (_enemySprites.TryGetValue(enemy.Type, out var sprite))
                    {
                        return $"[{sprite.Color}]{sprite.TopLeft}[/]";
                    }
                    return "[red]E[/]"; // Default fallback
                }
            }
            
            // Check for items
            if (items.ContainsKey((x, y)))
            {
                var item = items[(x, y)];
                string itemColor = item.Type switch
                {
                    ItemType.Weapon => "blue",
                    ItemType.Armor => "cyan",
                    ItemType.Consumable => "green",
                    _ => "white"
                };
                return $"[{itemColor}]i[/]";
            }
            
            // Check for exit position
            if (exitPosition.HasValue && x == exitPosition.Value.X && y == exitPosition.Value.Y)
            {
                return "[magenta]>[/]";
            }
            
            // Check for shop position
            if (shopPosition.HasValue && x == shopPosition.Value.X && y == shopPosition.Value.Y)
            {
                return "[yellow]$[/]";
            }
            
            // Render map tiles
            if (map[x, y] == TileType.Wall)
            {
                return "[grey]#[/]";
            }
            else // Floor
            {
                return "[grey].[/]";
            }
        }
        
        private Table RenderPlayerStats(Player player, string? exitDirection = null)
        {
            var table = new Table();
            table.Border = TableBorder.None;
            table.Expand = true;
            
            table.AddColumn("Health");
            table.AddColumn("Level");
            table.AddColumn("XP");
            table.AddColumn("Gold");
            table.AddColumn("Weapon");
            table.AddColumn("Armor");
            if (exitDirection != null)
            {
                table.AddColumn("Exit");
            }
            
            // HP as text instead of ProgressBar (simpler)
            string hpColor = player.HP < player.MaxHP / 3 ? "red" : (player.HP < player.MaxHP * 2 / 3 ? "yellow" : "green");
            string hpText = $"HP: [{hpColor}]{player.HP}/{player.MaxHP}[/]";
            
            string weaponInfo = player.EquippedWeapon != null 
                ? $"{player.EquippedWeapon.Name} (+{player.EquippedWeapon.Power})" 
                : "None";
                
            string armorInfo = player.EquippedArmor != null 
                ? $"{player.EquippedArmor.Name} (+{player.EquippedArmor.Power})" 
                : "None";
            
            var rowData = new List<string>
            {
                hpText,
                $"Level: {player.Level}",
                $"XP: {player.XP}/{player.XPThreshold}",
                $"Gold: [yellow]{player.Gold}[/]",
                weaponInfo,
                armorInfo
            };
            
            if (exitDirection != null)
            {
                string directionSymbol = exitDirection switch
                {
                    "north" => "↑",
                    "south" => "↓",
                    "east" => "→",
                    "west" => "←",
                    _ => "?"
                };
                rowData.Add($"[magenta]{directionSymbol} {exitDirection}[/]");
            }
            
            table.AddRow(rowData.ToArray());
            
            return table;
        }
        
        private Markup RenderMessageLog(List<string> messages)
        {
            string log = string.Join("\n", messages);
            return new Markup(log);
        }
        
        public Item? ShowInventory(List<Item> inventory)
        {
            if (inventory.Count == 0)
            {
                AnsiConsole.MarkupLine("[red]Your inventory is empty.[/]");
                AnsiConsole.MarkupLine("[gray]Press any key to continue...[/]");
                Console.ReadKey(true);
                return null;
            }
            
            // Create table for inventory
            var table = new Table();
            table.Border = TableBorder.Rounded;
            table.Expand = true;
            table.Title = new TableTitle("Your Inventory");
            
            table.AddColumn(new TableColumn("Name").Centered());
            table.AddColumn(new TableColumn("Type").Centered());
            table.AddColumn(new TableColumn("Power").Centered());
            table.AddColumn(new TableColumn("Description").LeftAligned());
            
            for (int i = 0; i < inventory.Count; i++)
            {
                var item = inventory[i];
                
                string typeInfo = item.Type.ToString();
                string colorCode = item.Type switch
                {
                    ItemType.Weapon => "blue",
                    ItemType.Armor => "cyan",
                    ItemType.Consumable => "green",
                    _ => "white"
                };
                
                string equippedMark = "";
                
                table.AddRow(
                    $"{i+1}. {item.Name}{equippedMark}",
                    $"[{colorCode}]{typeInfo}[/]",
                    item.Power.ToString(),
                    item.Description
                );
            }
            
            AnsiConsole.Write(table);
            
            // Prompt for item selection
            AnsiConsole.MarkupLine("\nSelect an item number to use/equip (or 0 to cancel): ");
            string? input = Console.ReadLine();
            
            if (!string.IsNullOrEmpty(input) && int.TryParse(input, out int choice) && choice > 0 && choice <= inventory.Count)
            {
                return inventory[choice - 1];
            }
            
            return null;
        }
        
        public void ShowQuestLog(List<Quest> quests)
        {
            Console.Clear();
            
            if (quests.Count == 0)
            {
                AnsiConsole.MarkupLine("[gray]You don't have any active quests.[/]");
                AnsiConsole.MarkupLine("\nPress any key to continue...");
                Console.ReadKey(true);
                return;
            }
            
            // Create a table for the quest log
            var table = new Table();
            table.Border = TableBorder.Rounded;
            table.Expand = true;
            table.Title = new TableTitle("Quest Log");
            
            table.AddColumn(new TableColumn("Description").LeftAligned());
            table.AddColumn(new TableColumn("Progress").Centered());
            table.AddColumn(new TableColumn("Reward").Centered());
            
            // Add each quest
            foreach (var quest in quests)
            {
                string statusColor = quest.Completed ? "green" : "yellow";
                string progress = quest.Completed ? "[green]Completed[/]" : $"[yellow]{quest.GetProgress()}[/]";
                
                table.AddRow(
                    quest.Description,
                    progress,
                    $"{quest.RewardXP} XP"
                );
            }
            
            AnsiConsole.Write(table);
            
            AnsiConsole.MarkupLine("\nPress any key to continue...");
            Console.ReadKey(true);
        }
        
        public void ShowGameOver(Player player)
        {
            Console.Clear();
            
            var panel = new Panel(
                Align.Center(
                    new Markup($"[bold red]Game Over![/]\n\n" +
                               $"You reached level [yellow]{player.Level}[/]\n" +
                               $"Gold collected: [yellow]{player.Gold}[/]\n" +
                               $"Enemies defeated: {player.Quests.SelectMany(q => q.Target == "Goblin" || q.Target == "Skeleton" || q.Target == "Rat" ? new[] { q.CurrentCount } : new[] { 0 }).Sum()}\n" +
                               $"Quests completed: {player.Quests.Count(q => q.Completed)}")
                )
            );
            
            panel.Border = BoxBorder.Double;
            panel.BorderStyle = new Style(Color.Red);
            panel.Expand = true;
            
            var grid = new Grid();
            grid.AddColumn(new GridColumn().NoWrap());
            grid.AddRow(
                Align.Center(
                    new FigletText("Game Over")
                        .Color(Color.Red)
                        .Centered()
                )
            );
            grid.AddRow(panel);
            grid.AddRow(Align.Center(new Markup("[gray]Press any key to exit...[/]")));
            
            AnsiConsole.Write(grid);
            
            Console.ReadKey(true);
        }
        
        public bool ConfirmQuit()
        {
            return AnsiConsole.Confirm("Are you sure you want to quit the game?");
        }
        
        public void ShowHelpScreen()
        {
            Console.Clear();
            
            var panel = new Panel(
                new Markup(
                    "[bold]Controls:[/]\n" +
                    "- [yellow]WASD[/] or [yellow]Arrow Keys[/]: Move\n" +
                    "- [yellow]Space[/]: Attack\n" +
                    "- [yellow]I[/]: Inventory\n" +
                    "- [yellow]Q[/]: Quest Log\n" +
                    "- [yellow]H[/]: Use Heal ability (when available)\n" +
                    "- [yellow]Esc[/]: Quit game\n\n" +
                    
                    "[bold]Map Symbols:[/]\n" +
                    "- [yellow]@[/]: Player\n" +
                    "- [gray]#[/]: Wall\n" +
                    "- [white].[/]: Floor\n" +
                    "- [red]E[/]: Enemy\n" +
                    "- [blue]i[/]: Item\n" +
                    "- [magenta]>[/]: Level exit\n" +
                    "- [yellow]$[/]: Shop\n\n" +
                    
                    "[bold]Game Objective:[/]\n" +
                    "Explore the dungeon, defeat enemies, collect items, and advance through multiple dungeon levels. " +
                    "Visit shops to buy better gear and sell your loot. Complete quests to earn experience and gold."
                )
            );
            
            panel.Border = BoxBorder.Rounded;
            panel.BorderStyle = new Style(Color.Blue);
            panel.Header = new PanelHeader("Help & Instructions");
            panel.Expand = true;
            
            AnsiConsole.Write(panel);
            
            AnsiConsole.MarkupLine("\n[gray]Press any key to return to the game...[/]");
            Console.ReadKey(true);
        }
    }
} 