using System;
using System.Collections.Generic;
using RPG.Engine;

namespace RPG.Models
{
    public enum GameProgress
    {
        Dungeon,
        Shop,
        NextLevel,
        Victory
    }

    public class GameWorld
    {
        public int CurrentLevel { get; private set; }
        public int MaxLevels { get; private set; }
        public GameProgress CurrentState { get; set; }
        public Player? Player { get; private set; }
        public Dictionary<int, TileType[,]> Maps { get; private set; }
        public Dictionary<int, List<Enemy>> EnemiesByLevel { get; private set; }
        public Dictionary<int, Dictionary<(int, int), Item>> ItemsByLevel { get; private set; }
        public Dictionary<string, Item> ShopInventory { get; private set; }
        
        private (int X, int Y) _exitPosition;
        private (int X, int Y) _shopPosition;
        private readonly Random _random;

        public GameWorld(int maxLevels = 3, Random? random = null)
        {
            _random = random ?? new Random();
            MaxLevels = maxLevels;
            CurrentLevel = 1;
            CurrentState = GameProgress.Dungeon;
            
            Maps = new Dictionary<int, TileType[,]>();
            EnemiesByLevel = new Dictionary<int, List<Enemy>>();
            ItemsByLevel = new Dictionary<int, Dictionary<(int, int), Item>>();
            ShopInventory = new Dictionary<string, Item>();
            
            InitializeShopInventory();
        }

        public void Initialize(Player player)
        {
            Player = player;
            GenerateAllLevels();
        }

        private void GenerateAllLevels()
        {
            for (int level = 1; level <= MaxLevels; level++)
            {
                // Create much larger maps for each level to support camera scrolling
                int width = 80 + (level - 1) * 10; // Significantly larger maps
                int height = 40 + (level - 1) * 5; 
                
                var mapGenerator = new MapGenerator(width, height, _random);
                
                // Modified wall percentage for more interesting terrain
                int wallPercent = 30 + (level - 1) * 2;
                Maps[level] = mapGenerator.GenerateCaveDungeon(wallPercent);
                
                // Place player at start position only for level 1
                if (level == 1 && Player != null)
                {
                    // Place the player in the center of the map
                    Player.X = width / 2;
                    Player.Y = height / 2;
                    
                    // Ensure the starting area is clear
                    ClearArea(level, Player.X, Player.Y, 5);
                }
                
                // Add more enemies in higher levels
                string[] enemyTypes = level switch
                {
                    1 => new[] { "Goblin", "Rat" },
                    2 => new[] { "Goblin", "Skeleton", "Rat" },
                    3 => new[] { "Skeleton", "Orc", "Undead" },
                    _ => new[] { "Demon", "Orc", "Undead" }
                };
                
                // Enemies become stronger in higher levels
                int baseHP = 20 + (level - 1) * 10;
                int baseDamage = 5 + (level - 1) * 2;
                
                // Scale enemy count with map size
                int enemyCount = 10 + level * 3;
                
                EnemiesByLevel[level] = new List<Enemy>();
                for (int i = 0; i < enemyCount; i++)
                {
                    string enemyType = enemyTypes[_random.Next(enemyTypes.Length)];
                    int x, y;
                    
                    // Find a suitable position away from player
                    do
                    {
                        x = _random.Next(1, width - 1);
                        y = _random.Next(1, height - 1);
                    } while (Maps[level][x, y] != TileType.Floor || 
                             (level == 1 && Math.Abs(x - Player.X) + Math.Abs(y - Player.Y) < 10));
                    
                    var enemy = new Enemy(x, y, enemyType, baseHP, baseDamage, 10 * level);
                    EnemiesByLevel[level].Add(enemy);
                }
                
                // Add items - more items for larger maps
                ItemsByLevel[level] = new Dictionary<(int, int), Item>();
                List<Func<Item>> itemFactories = GetItemFactoriesForLevel(level);
                
                int itemCount = 10 + level * 5;
                
                for (int i = 0; i < itemCount; i++)
                {
                    int x, y;
                    do
                    {
                        x = _random.Next(1, width - 1);
                        y = _random.Next(1, height - 1);
                    } while (Maps[level][x, y] != TileType.Floor || 
                             ItemsByLevel[level].ContainsKey((x, y)) ||
                             (level == 1 && Math.Abs(x - Player.X) + Math.Abs(y - Player.Y) < 5));
                    
                    var item = itemFactories[_random.Next(itemFactories.Count)]();
                    ItemsByLevel[level][(x, y)] = item;
                }
                
                // Add exit and shop
                PlaceExitAndShop(level, width, height);
            }
        }
        
        // Clears an area around a position to ensure it's walkable
        private void ClearArea(int level, int centerX, int centerY, int radius)
        {
            for (int x = centerX - radius; x <= centerX + radius; x++)
            {
                for (int y = centerY - radius; y <= centerY + radius; y++)
                {
                    if (x >= 0 && x < Maps[level].GetLength(0) && 
                        y >= 0 && y < Maps[level].GetLength(1))
                    {
                        Maps[level][x, y] = TileType.Floor;
                    }
                }
            }
        }
        
        private void PlaceExitAndShop(int level, int width, int height)
        {
            if (level < MaxLevels)
            {
                // For larger maps, place the exit at a more distant location
                int exitX = width * 2 / 3;
                int exitY = height * 2 / 3;
                
                // Safety bounds check
                exitX = Math.Min(Math.Max(5, exitX), width - 5);
                exitY = Math.Min(Math.Max(5, exitY), height - 5);
                
                // Ensure the area around the exit is completely clear (7x7 area)
                for (int x = exitX - 3; x <= exitX + 3; x++)
                {
                    for (int y = exitY - 3; y <= exitY + 3; y++)
                    {
                        if (x >= 0 && x < width && y >= 0 && y < height)
                        {
                            Maps[level][x, y] = TileType.Floor;
                        }
                    }
                }
                
                _exitPosition = (exitX, exitY);
                
                // Place shop at a 1/3 position to be clearly separated from exit
                int shopX = width / 3;
                int shopY = height / 3;
                
                // Safety bounds check
                shopX = Math.Min(Math.Max(5, shopX), width - 5);
                shopY = Math.Min(Math.Max(5, shopY), height - 5);
                
                // Clear area around shop
                for (int x = shopX - 2; x <= shopX + 2; x++)
                {
                    for (int y = shopY - 2; y <= shopY + 2; y++)
                    {
                        if (x >= 0 && x < width && y >= 0 && y < height)
                        {
                            Maps[level][x, y] = TileType.Floor;
                        }
                    }
                }
                
                _shopPosition = (shopX, shopY);
                
                // Create a straight path from the player's starting position to the exit
                // For level 1, start from player position, for other levels from center
                int startX = (level == 1 && Player != null) ? Player.X : width / 2;
                int startY = (level == 1 && Player != null) ? Player.Y : height / 2;
                
                CreateStraightPath(level, startX, startY, exitX, exitY);
                
                // Also create a path to the shop
                CreateStraightPath(level, startX, startY, shopX, shopY);
            }
        }
        
        // Create a direct path between two points
        private void CreateStraightPath(int level, int startX, int startY, int endX, int endY)
        {
            int x = startX;
            int y = startY;
            
            // First move horizontally
            while (x != endX)
            {
                x += Math.Sign(endX - x);
                
                // Create a 3-tile wide corridor
                for (int dy = -1; dy <= 1; dy++)
                {
                    int ny = y + dy;
                    if (ny >= 0 && ny < Maps[level].GetLength(1))
                    {
                        Maps[level][x, ny] = TileType.Floor;
                    }
                }
            }
            
            // Then move vertically
            while (y != endY)
            {
                y += Math.Sign(endY - y);
                
                // Create a 3-tile wide corridor
                for (int dx = -1; dx <= 1; dx++)
                {
                    int nx = x + dx;
                    if (nx >= 0 && nx < Maps[level].GetLength(0))
                    {
                        Maps[level][nx, y] = TileType.Floor;
                    }
                }
            }
        }
        
        private void InitializeShopInventory()
        {
            ShopInventory = new Dictionary<string, Item>
            {
                {"Sword", new Item("Sword", ItemType.Weapon, 5, "A basic sword")},
                {"Battle Axe", new Item("Battle Axe", ItemType.Weapon, 8, "A heavy battle axe")},
                {"Magic Staff", new Item("Magic Staff", ItemType.Weapon, 12, "A staff imbued with magical energy")},
                {"Leather Armor", new Item("Leather Armor", ItemType.Armor, 2, "Basic leather armor")},
                {"Chain Mail", new Item("Chain Mail", ItemType.Armor, 5, "Strong chain mail armor")},
                {"Plate Armor", new Item("Plate Armor", ItemType.Armor, 10, "Heavy plate armor")},
                {"Health Potion", new Item("Health Potion", ItemType.Consumable, 20, "Restores 20 HP")},
                {"Greater Health Potion", new Item("Greater Health Potion", ItemType.Consumable, 50, "Restores 50 HP")},
                {"Strength Potion", new Item("Strength Potion", ItemType.Consumable, 5, "Temporarily increases attack power")}
            };
        }
        
        private List<Func<Item>> GetItemFactoriesForLevel(int level)
        {
            var itemFactories = new List<Func<Item>>();
            
            // Basic items available on all levels
            itemFactories.Add(Item.CreateHealthPotion);
            itemFactories.Add(Item.CreateHealthPotion); // Add twice for higher frequency
            
            // Level-specific items
            if (level == 1)
            {
                itemFactories.Add(Item.CreateSword);
                itemFactories.Add(Item.CreateLeatherArmor);
            }
            else if (level == 2)
            {
                itemFactories.Add(Item.CreateSword);
                itemFactories.Add(Item.CreateAxe);
                itemFactories.Add(Item.CreateLeatherArmor);
                itemFactories.Add(Item.CreateChainMail);
            }
            else
            {
                itemFactories.Add(Item.CreateAxe);
                itemFactories.Add(Item.CreateChainMail);
                itemFactories.Add(() => new Item("Magic Staff", ItemType.Weapon, 12, "A staff imbued with magical energy"));
                itemFactories.Add(() => new Item("Plate Armor", ItemType.Armor, 10, "Heavy plate armor"));
            }
            
            return itemFactories;
        }
        
        public bool CheckSpecialTiles(int playerX, int playerY)
        {
            // Check if player is on or adjacent to the exit (to make it easier to find)
            if (CurrentLevel < MaxLevels && 
                Math.Abs(playerX - _exitPosition.X) <= 1 && 
                Math.Abs(playerY - _exitPosition.Y) <= 1)
            {
                CurrentState = GameProgress.NextLevel;
                return true;
            }
            
            // We no longer check for shop here; it's handled in GameEngine with cooldown
            
            return false;
        }
        
        public void AdvanceToNextLevel()
        {
            if (CurrentLevel < MaxLevels && Player != null)
            {
                CurrentLevel++;
                CurrentState = GameProgress.Dungeon;
                
                // Place player at a good starting position in the new level
                var mapGenerator = new MapGenerator(0, 0); // Just for using FindPlayerStartPosition
                var startPos = mapGenerator.FindRandomFloorTile(Maps[CurrentLevel]);
                Player.X = startPos.Item1;
                Player.Y = startPos.Item2;
                
                // Full heal when advancing levels
                Player.HP = Player.MaxHP;
            }
            else
            {
                CurrentState = GameProgress.Victory;
            }
        }
        
        public TileType[,] GetCurrentMap()
        {
            return Maps[CurrentLevel];
        }
        
        public List<Enemy> GetCurrentEnemies()
        {
            return EnemiesByLevel[CurrentLevel];
        }
        
        public Dictionary<(int, int), Item> GetCurrentItems()
        {
            return ItemsByLevel[CurrentLevel];
        }
        
        public (int X, int Y) GetExitPosition()
        {
            return _exitPosition;
        }
        
        public (int X, int Y) GetShopPosition()
        {
            return _shopPosition;
        }
        
        public void ReturnFromShop()
        {
            CurrentState = GameProgress.Dungeon;
        }
        
        // Get direction to exit (for UI compass/hint) with improved accuracy
        public string GetDirectionToExit(int playerX, int playerY)
        {
            int xDiff = _exitPosition.X - playerX;
            int yDiff = _exitPosition.Y - playerY;
            
            // Use more precise cardinal + ordinal directions
            string xDirection = xDiff > 0 ? "east" : (xDiff < 0 ? "west" : "");
            string yDirection = yDiff > 0 ? "south" : (yDiff < 0 ? "north" : "");
            
            // Combined direction (e.g., "northeast", "southwest")
            if (xDirection != "" && yDirection != "")
            {
                return yDirection + xDirection;
            }
            
            // Single direction
            return xDirection != "" ? xDirection : (yDirection != "" ? yDirection : "nearby");
        }
    }
} 