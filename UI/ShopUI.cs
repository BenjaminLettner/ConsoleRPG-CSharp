using System;
using System.Collections.Generic;
using RPG.Models;
using Spectre.Console;

namespace RPG.UI
{
    public class ShopUI
    {
        private readonly Player _player;
        private readonly Dictionary<string, Item> _shopInventory;
        private readonly GameRenderer _renderer;
        
        private const int BaseSellPrice = 5; // Gold per item
        
        public ShopUI(GameRenderer renderer, Player player, Dictionary<string, Item> shopInventory)
        {
            _renderer = renderer;
            _player = player;
            _shopInventory = shopInventory;
        }
        
        public void ShowShop()
        {
            bool shopping = true;
            
            while (shopping)
            {
                Console.Clear();
                
                // Header
                AnsiConsole.Write(
                    new FigletText("Shop")
                        .Centered()
                        .Color(Color.Gold1));
                
                AnsiConsole.MarkupLine($"[yellow]Gold: {_player.Gold}[/]");
                AnsiConsole.WriteLine();
                
                // Shop menu options
                var selection = AnsiConsole.Prompt(
                    new SelectionPrompt<string>()
                        .Title("What would you like to do?")
                        .PageSize(10)
                        .AddChoices(new[] {
                            "Buy Items",
                            "Sell Items",
                            "Leave Shop"
                        }));
                
                switch (selection)
                {
                    case "Buy Items":
                        BuyItems();
                        break;
                    case "Sell Items":
                        SellItems();
                        break;
                    case "Leave Shop":
                        shopping = false;
                        Console.Clear(); // Clear the console when leaving
                        AnsiConsole.MarkupLine("[yellow]Leaving shop... Shop cooldown is now active.[/]");
                        Thread.Sleep(1000); // Brief pause to show the message
                        Console.Clear();
                        break;
                }
            }
        }
        
        private void BuyItems()
        {
            if (_shopInventory.Count == 0)
            {
                AnsiConsole.MarkupLine("[red]The shop has no items for sale.[/]");
                AnsiConsole.MarkupLine("[gray]Press any key to continue...[/]");
                Console.ReadKey(true);
                return;
            }
            
            // Create table of items for sale
            var table = new Table();
            table.Border = TableBorder.Rounded;
            table.Expand = true;
            table.Title = new TableTitle("Items For Sale");
            
            table.AddColumn(new TableColumn("Name").Centered());
            table.AddColumn(new TableColumn("Type").Centered());
            table.AddColumn(new TableColumn("Power").Centered());
            table.AddColumn(new TableColumn("Price").Centered());
            table.AddColumn(new TableColumn("Description").LeftAligned());
            
            var shopItems = new Dictionary<int, Item>();
            int index = 1;
            
            foreach (var item in _shopInventory.Values)
            {
                shopItems[index] = item;
                int price = CalculatePrice(item);
                
                string typeInfo = item.Type.ToString();
                string colorCode = item.Type switch
                {
                    ItemType.Weapon => "blue",
                    ItemType.Armor => "cyan",
                    ItemType.Consumable => "green",
                    _ => "white"
                };
                
                table.AddRow(
                    $"{index}. {item.Name}",
                    $"[{colorCode}]{typeInfo}[/]",
                    item.Power.ToString(),
                    $"[yellow]{price} gold[/]",
                    item.Description
                );
                
                index++;
            }
            
            AnsiConsole.Write(table);
            AnsiConsole.MarkupLine($"\n[yellow]Your Gold: {_player.Gold}[/]");
            
            // Prompt for item selection
            AnsiConsole.MarkupLine("\nSelect an item number to buy (or 0 to cancel): ");
            string? input = Console.ReadLine();
            
            if (!string.IsNullOrEmpty(input) && int.TryParse(input, out int choice) && choice > 0 && choice <= shopItems.Count)
            {
                var selectedItem = shopItems[choice];
                int price = CalculatePrice(selectedItem);
                
                if (_player.Gold >= price)
                {
                    _player.Gold -= price;
                    _player.AddToInventory(selectedItem);
                    AnsiConsole.MarkupLine($"[green]You bought {selectedItem.Name} for {price} gold.[/]");
                }
                else
                {
                    AnsiConsole.MarkupLine("[red]You don't have enough gold to buy that item.[/]");
                }
            }
            
            AnsiConsole.MarkupLine("[gray]Press any key to continue...[/]");
            Console.ReadKey(true);
        }
        
        private void SellItems()
        {
            if (_player.Inventory.Count == 0)
            {
                AnsiConsole.MarkupLine("[red]You have no items to sell.[/]");
                AnsiConsole.MarkupLine("[gray]Press any key to continue...[/]");
                Console.ReadKey(true);
                return;
            }
            
            // Create table of items to sell
            var table = new Table();
            table.Border = TableBorder.Rounded;
            table.Expand = true;
            table.Title = new TableTitle("Your Inventory");
            
            table.AddColumn(new TableColumn("Name").Centered());
            table.AddColumn(new TableColumn("Type").Centered());
            table.AddColumn(new TableColumn("Power").Centered());
            table.AddColumn(new TableColumn("Value").Centered());
            table.AddColumn(new TableColumn("Description").LeftAligned());
            
            for (int i = 0; i < _player.Inventory.Count; i++)
            {
                var item = _player.Inventory[i];
                int value = CalculateSellValue(item);
                
                string typeInfo = item.Type.ToString();
                string colorCode = item.Type switch
                {
                    ItemType.Weapon => "blue",
                    ItemType.Armor => "cyan",
                    ItemType.Consumable => "green",
                    _ => "white"
                };
                
                string equippedMark = "";
                if ((item.Type == ItemType.Weapon && _player.EquippedWeapon == item) ||
                    (item.Type == ItemType.Armor && _player.EquippedArmor == item))
                {
                    equippedMark = " [red](Equipped)[/]";
                }
                
                table.AddRow(
                    $"{i+1}. {item.Name}{equippedMark}",
                    $"[{colorCode}]{typeInfo}[/]",
                    item.Power.ToString(),
                    $"[yellow]{value} gold[/]",
                    item.Description
                );
            }
            
            AnsiConsole.Write(table);
            AnsiConsole.MarkupLine($"\n[yellow]Your Gold: {_player.Gold}[/]");
            
            // Prompt for item selection
            AnsiConsole.MarkupLine("\nSelect an item number to sell (or 0 to cancel): ");
            string? input = Console.ReadLine();
            
            if (!string.IsNullOrEmpty(input) && int.TryParse(input, out int choice) && choice > 0 && choice <= _player.Inventory.Count)
            {
                var selectedItem = _player.Inventory[choice-1];
                
                // Check if item is equipped
                if ((selectedItem.Type == ItemType.Weapon && _player.EquippedWeapon == selectedItem) ||
                    (selectedItem.Type == ItemType.Armor && _player.EquippedArmor == selectedItem))
                {
                    // Ask for confirmation to sell equipped item
                    bool confirm = AnsiConsole.Confirm($"This item is equipped. Are you sure you want to sell it?");
                    if (!confirm)
                    {
                        AnsiConsole.MarkupLine("[gray]Sale canceled.[/]");
                        AnsiConsole.MarkupLine("[gray]Press any key to continue...[/]");
                        Console.ReadKey(true);
                        return;
                    }
                    
                    // Unequip the item
                    if (selectedItem.Type == ItemType.Weapon)
                        _player.EquippedWeapon = null;
                    else if (selectedItem.Type == ItemType.Armor)
                        _player.EquippedArmor = null;
                    
                    _player.UpdateStats();
                }
                
                int value = CalculateSellValue(selectedItem);
                _player.Gold += value;
                _player.Inventory.RemoveAt(choice-1);
                
                AnsiConsole.MarkupLine($"[green]You sold {selectedItem.Name} for {value} gold.[/]");
            }
            
            AnsiConsole.MarkupLine("[gray]Press any key to continue...[/]");
            Console.ReadKey(true);
        }
        
        private int CalculatePrice(Item item)
        {
            // Base price calculation based on item type and power
            int basePrice = item.Type switch
            {
                ItemType.Weapon => 10 + item.Power * 5,
                ItemType.Armor => 8 + item.Power * 6,
                ItemType.Consumable => 5 + item.Power,
                _ => 5
            };
            
            return basePrice;
        }
        
        private int CalculateSellValue(Item item)
        {
            // Sell value is less than buy price (about half)
            return Math.Max(BaseSellPrice, CalculatePrice(item) / 2);
        }
    }
} 