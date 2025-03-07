using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using RPG.Models;
using RPG.UI;
using Spectre.Console;

namespace RPG.Engine
{
    public class GameEngine
    {
        private readonly Random _random;
        private readonly InputHandler _inputHandler;
        private readonly GameRenderer _renderer;
        private readonly GameWorld _gameWorld;
        private ShopUI _shopUI;
        
        private Player _player;
        private List<Enemy> _enemies;
        private Dictionary<(int, int), Item> _itemsOnGround;
        private TileType[,] _map;
        
        private bool _isRunning;
        private bool _gameOver;
        private int _mapWidth;
        private int _mapHeight;
        private readonly int _targetFrameRate;
        private readonly int _frameDelayMs;
        
        private List<string> _messageLog;
        private const int MaxLogMessages = 5;
        
        // Multithreading-related fields
        private readonly object _gameLock = new object();
        private readonly object _renderLock = new object();
        private readonly object _inputLock = new object();
        private readonly object _messageLock = new object();
        private bool _needsRender = true;
        private ConsoleKey? _lastKeyPressed;
        private CancellationTokenSource _cancellationTokenSource;
        
        // Shop cooldown mechanism
        private DateTime _lastShopInteraction = DateTime.MinValue;
        private const int ShopCooldownSeconds = 5;
        private bool _shopCooldownActive => (DateTime.Now - _lastShopInteraction).TotalSeconds < ShopCooldownSeconds;
        
        public GameEngine(int targetFrameRate = 20)
        {
            _targetFrameRate = targetFrameRate;
            _frameDelayMs = 1000 / targetFrameRate;
            _random = new Random();
            _inputHandler = new InputHandler();
            _renderer = new GameRenderer();
            _gameWorld = new GameWorld(maxLevels: 5, random: _random);
            _messageLog = new List<string>();
            _isRunning = true;
            _gameOver = false;
            
            // Initialize with default values to be replaced in Initialize()
            _player = null!; // Will be set in Initialize
            _enemies = new List<Enemy>();
            _itemsOnGround = new Dictionary<(int, int), Item>();
            _map = new TileType[0, 0]; // Empty map as placeholder
            _shopUI = null!; // Will be set in Initialize
            
            // Initialize cancellation token source
            _cancellationTokenSource = new CancellationTokenSource();
        }
        
        public void Initialize()
        {
            // Create a larger map size
            _mapWidth = 80;  // Increased from default
            _mapHeight = 40; // Increased from default
            
            // Initialize player in the center of the map
            _player = new Player(
                x: _mapWidth / 2,
                y: _mapHeight / 2,
                hp: 100,
                damage: 5
            );
            
            // Initialize the game world with the player
            _gameWorld.Initialize(_player);
            
            // Add welcome message for the game
            AddMessage("[yellow]Welcome to the Dungeon RPG![/]");
            AddMessage("[cyan]Use WASD or arrow keys to move.[/]");
            AddMessage("[magenta]Find the blinking exit > to proceed to the next level.[/]");
            AddMessage($"[yellow]The exit is to the {_gameWorld.GetDirectionToExit(_player.X, _player.Y)}.[/]");

            // Update the current level data (map, enemies, items)
            UpdateCurrentLevelData();
            
            // Initialize the shop UI
            _shopUI = new ShopUI(_renderer, _player, _gameWorld.ShopInventory);
        }
        
        private void UpdateCurrentLevelData()
        {
            lock (_gameLock)
            {
                _map = _gameWorld.GetCurrentMap();
                _enemies = _gameWorld.GetCurrentEnemies();
                _itemsOnGround = _gameWorld.GetCurrentItems();
                
                _mapWidth = _map.GetLength(0);
                _mapHeight = _map.GetLength(1);
            }
        }
        
        public void Run()
        {
            // Initialize the cancellation token source
            _cancellationTokenSource = new CancellationTokenSource();
            var token = _cancellationTokenSource.Token;
            
            // Start the input thread
            Task inputTask = Task.Run(() => InputThread(token), token);
            
            // Start the update thread
            Task updateTask = Task.Run(() => UpdateThread(token), token);
            
            // Start the render thread
            Task renderTask = Task.Run(() => RenderThread(token), token);
            
            // Main game loop - just waits for all tasks to complete
            try
            {
                Task.WaitAll(new[] { inputTask, updateTask, renderTask });
            }
            catch (AggregateException ae)
            {
                foreach (var ex in ae.InnerExceptions)
                {
                    Console.WriteLine($"Thread error: {ex.Message}");
                }
            }
            
            // Cleanup
            _cancellationTokenSource.Dispose();
        }
        
        private void InputThread(CancellationToken token)
        {
            try
            {
                while (_isRunning && !_gameOver && !token.IsCancellationRequested)
                {
                    // Non-blocking key check
                    if (Console.KeyAvailable)
                    {
                        var key = Console.ReadKey(true).Key;
                        lock (_inputLock)
                        {
                            _lastKeyPressed = key;
                        }
                    }
                    
                    // Sleep to prevent CPU overuse
                    Thread.Sleep(10);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Input thread error: {ex.Message}");
            }
        }
        
        private void UpdateThread(CancellationToken token)
        {
            try
            {
                while (_isRunning && !_gameOver && !token.IsCancellationRequested)
                {
                    lock (_gameLock)
                    {
                        // Process input if available
                        ConsoleKey? keyPressed = null;
                        lock (_inputLock)
                        {
                            if (_lastKeyPressed.HasValue)
                            {
                                keyPressed = _lastKeyPressed;
                                _lastKeyPressed = null;
                            }
                        }
                        
                        if (keyPressed.HasValue)
                        {
                            ProcessInput(keyPressed.Value);
                        }
                        
                        // Update game state
                        Update();
                        
                        // Handle state transitions
                        HandleStateTransitions();
                        
                        // Flag that we need a render update
                        lock (_renderLock)
                        {
                            _needsRender = true;
                        }
                    }
                    
                    // Control update rate
                    Thread.Sleep(_frameDelayMs);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Update thread error: {ex.Message}");
            }
        }
        
        // Separated state transition handling to keep update cleaner
        private void HandleStateTransitions()
        {
            // Check game state transitions
            if (_gameWorld.CurrentState == GameProgress.NextLevel)
            {
                TransitionToNextLevel();
            }
            else if (_gameWorld.CurrentState == GameProgress.Shop && !_shopCooldownActive)
            {
                // Signal render thread to pause while in shop
                lock (_renderLock)
                {
                    _needsRender = false;
                }
                
                // Release the game lock while showing shop to avoid deadlock
                Monitor.Exit(_gameLock);
                try
                {
                    // Show shop outside of any locks to prevent deadlocks
                    ShowShop();
                    
                    // Force state back to dungeon
                    _gameWorld.CurrentState = GameProgress.Dungeon;
                }
                finally
                {
                    // Re-acquire the game lock
                    Monitor.Enter(_gameLock);
                }
                
                // Resume rendering
                lock (_renderLock)
                {
                    _needsRender = true;
                }
            }
            else if (_gameWorld.CurrentState == GameProgress.Victory)
            {
                // Signal render thread to pause for victory screen
                lock (_renderLock)
                {
                    _needsRender = false;
                }
                
                ShowVictoryScreen();
                _isRunning = false;
            }
        }
        
        private void RenderThread(CancellationToken token)
        {
            try
            {
                while (_isRunning && !token.IsCancellationRequested)
                {
                    bool shouldRender = false;
                    
                    lock (_renderLock)
                    {
                        shouldRender = _needsRender;
                        if (shouldRender)
                        {
                            _needsRender = false;
                        }
                    }
                    
                    if (shouldRender)
                    {
                        // Create local copies of game state to avoid locking during rendering
                        Player playerCopy;
                        List<Enemy> enemiesCopy;
                        Dictionary<(int, int), Item> itemsCopy;
                        TileType[,] mapCopy;
                        List<string> messagesCopy;
                        (int X, int Y)? exitPos;
                        (int X, int Y)? shopPos;
                        
                        lock (_gameLock)
                        {
                            playerCopy = _player;
                            enemiesCopy = new List<Enemy>(_enemies);
                            itemsCopy = new Dictionary<(int, int), Item>(_itemsOnGround);
                            mapCopy = _map;
                            exitPos = _gameWorld.GetExitPosition();
                            shopPos = _gameWorld.GetShopPosition();
                        }
                        
                        lock (_messageLock)
                        {
                            messagesCopy = new List<string>(_messageLog);
                        }
                        
                        string exitDirection = _gameWorld.GetDirectionToExit(playerCopy.X, playerCopy.Y);
                        
                        // Render the game
                        _renderer.Render(
                            playerCopy,
                            enemiesCopy,
                            itemsCopy,
                            mapCopy,
                            messagesCopy,
                            exitPos,
                            _gameWorld.CurrentState == GameProgress.Dungeon ? shopPos : null,
                            exitDirection
                        );
                    }
                    
                    // Render at a smoother rate
                    Thread.Sleep(_frameDelayMs / 2);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Render thread error: {ex.Message}");
            }
        }
        
        private void ProcessInput(ConsoleKey key)
        {
            // Convert key press to movement or action
            switch (key)
            {
                // Movement keys
                case ConsoleKey.W:
                case ConsoleKey.UpArrow:
                    TryMovePlayer(0, -1);
                    break;
                case ConsoleKey.S:
                case ConsoleKey.DownArrow:
                    TryMovePlayer(0, 1);
                    break;
                case ConsoleKey.A:
                case ConsoleKey.LeftArrow:
                    TryMovePlayer(-1, 0);
                    break;
                case ConsoleKey.D:
                case ConsoleKey.RightArrow:
                    TryMovePlayer(1, 0);
                    break;
                    
                // Combat key (space)
                case ConsoleKey.Spacebar:
                    PlayerAttack();
                    break;
                    
                // Inventory key
                case ConsoleKey.I:
                    OpenInventory();
                    break;
                    
                // Quest log key
                case ConsoleKey.Q:
                    OpenQuestLog();
                    break;
                    
                // Help key
                case ConsoleKey.H:
                    _renderer.ShowHelpScreen();
                    break;
                    
                // Quit key
                case ConsoleKey.Escape:
                    if (_renderer.ConfirmQuit())
                    {
                        _isRunning = false;
                    }
                    break;
            }
        }
        
        private bool TryMovePlayer(int dx, int dy)
        {
            int newX = _player.X + dx;
            int newY = _player.Y + dy;
            
            // Check if the new position is within bounds and walkable
            if (newX >= 0 && newX < _mapWidth && newY >= 0 && newY < _mapHeight &&
                IsWalkable(newX, newY))
            {
                // Check for enemies at the new position
                var enemy = _enemies.FirstOrDefault(e => e.IsAlive && e.X == newX && e.Y == newY);
                if (enemy != null)
                {
                    // Attack enemy instead of moving
                    int playerHpBefore = _player.HP;
                    _player.Attack(enemy);
                    
                    // Show attack result
                    if (!enemy.IsAlive)
                    {
                        AddMessage($"You defeated the {enemy.Type}! (+{enemy.XPValue} XP)");
                        
                        // Award gold for defeating enemy
                        int goldReward = 5 + _random.Next(1, 5) * _gameWorld.CurrentLevel;
                        _player.Gold += goldReward;
                        AddMessage($"You found {goldReward} gold!");
                    }
                    else
                    {
                        AddMessage($"You hit the {enemy.Type} for {_player.Damage} damage!");
                    }
                    
                    return true; // Attack counts as an action
                }
                
                // Check for shop - handle before moving to ensure it works properly
                var shopPos = _gameWorld.GetShopPosition();
                bool movingToShop = (newX == shopPos.X && newY == shopPos.Y);
                
                if (movingToShop)
                {
                    // Check cooldown before moving
                    if (_shopCooldownActive)
                    {
                        // Show cooldown message
                        int remainingSeconds = ShopCooldownSeconds - (int)(DateTime.Now - _lastShopInteraction).TotalSeconds;
                        AddMessage($"[yellow]Shop is busy. Try again in {remainingSeconds} seconds.[/]");
                        return false; // Don't move to shop during cooldown
                    }
                    else
                    {
                        // We're moving to shop and cooldown is not active, set state directly
                        _player.X = newX;
                        _player.Y = newY;
                        _gameWorld.CurrentState = GameProgress.Shop;
                        return true;
                    }
                }
                
                // Actually move player
                _player.X = newX;
                _player.Y = newY;
                
                // Check for items at the new position
                if (_itemsOnGround.TryGetValue((newX, newY), out var item))
                {
                    _player.AddToInventory(item);
                    _itemsOnGround.Remove((newX, newY));
                    
                    string itemTypeText = item.Type switch
                    {
                        ItemType.Weapon => "weapon",
                        ItemType.Armor => "armor",
                        ItemType.Consumable => "consumable",
                        _ => "item"
                    };
                    
                    AddMessage($"You picked up {item.Name}! ({itemTypeText})");
                }
                
                // Check for exit (handled by GameWorld.CheckSpecialTiles())
                _gameWorld.CheckSpecialTiles(newX, newY);
                
                return true;
            }
            
            return false;
        }
        
        private void PlayerAttack(int? targetX = null, int? targetY = null)
        {
            if (!_player.CanAttack())
            {
                return;
            }
            
            // Find adjacent enemies
            bool hitEnemy = false;
            foreach (var enemy in _enemies)
            {
                if (!enemy.IsAlive) continue;
                
                // If coordinates are specified, only attack at those coordinates
                if (targetX.HasValue && targetY.HasValue)
                {
                    if (enemy.X == targetX.Value && enemy.Y == targetY.Value)
                    {
                        _player.Attack(enemy);
                        AddMessage($"You attack the {enemy.Type} for {_player.Damage} damage!");
                        
                        if (!enemy.IsAlive)
                        {
                            AddMessage($"You defeated the {enemy.Type}!");
                            // Award some gold for defeating enemies
                            int goldReward = 5 + _random.Next(1, 5) * _gameWorld.CurrentLevel;
                            _player.Gold += goldReward;
                            AddMessage($"You found {goldReward} gold!");
                        }
                        
                        hitEnemy = true;
                        break;
                    }
                }
                else
                {
                    // Otherwise attack any adjacent enemy
                    if (Math.Abs(enemy.X - _player.X) <= 1 && Math.Abs(enemy.Y - _player.Y) <= 1)
                    {
                        _player.Attack(enemy);
                        AddMessage($"You attack the {enemy.Type} for {_player.Damage} damage!");
                        
                        if (!enemy.IsAlive)
                        {
                            AddMessage($"You defeated the {enemy.Type}!");
                            // Award some gold for defeating enemies
                            int goldReward = 5 + _random.Next(1, 5) * _gameWorld.CurrentLevel;
                            _player.Gold += goldReward;
                            AddMessage($"You found {goldReward} gold!");
                        }
                        
                        hitEnemy = true;
                        break; // Only attack one enemy
                    }
                }
            }
            
            if (!hitEnemy)
            {
                AddMessage("You swing but hit nothing.");
                _player.LastAttackTime = DateTime.Now; // Still use the attack cooldown
            }
        }
        
        private void Update()
        {
            // Update enemies
            foreach (var enemy in _enemies.Where(e => e.IsAlive))
            {
                // Enemies act every other turn (50% chance)
                if (_random.Next(2) == 0)
                {
                    // If enemy is near player (within 5 tiles), move toward player
                    int distanceToPlayer = Math.Abs(enemy.X - _player.X) + Math.Abs(enemy.Y - _player.Y);
                    
                    if (distanceToPlayer <= 5)
                    {
                        // If enemy is adjacent to player, always attack
                        if (distanceToPlayer <= 1)
                        {
                            // Direct attack when adjacent
                            DirectAttackFromEnemy(enemy);
                        }
                        else
                        {
                            // Get direction toward player
                            int dx = Math.Sign(_player.X - enemy.X);
                            int dy = Math.Sign(_player.Y - enemy.Y);
                            
                            // Try to move toward player, if can't then try other directions
                            if (!TryMoveEnemy(enemy, dx, dy))
                            {
                                // Try horizontal then vertical
                                if (dx != 0 && !TryMoveEnemy(enemy, dx, 0))
                                {
                                    TryMoveEnemy(enemy, 0, dy);
                                }
                                // Try vertical then horizontal
                                else if (dy != 0 && !TryMoveEnemy(enemy, 0, dy))
                                {
                                    TryMoveEnemy(enemy, dx, 0);
                                }
                            }
                        }
                    }
                    else
                    {
                        // Random movement
                        int randomDirection = _random.Next(4);
                        int dx = randomDirection == 0 ? -1 : randomDirection == 1 ? 1 : 0;
                        int dy = randomDirection == 2 ? -1 : randomDirection == 3 ? 1 : 0;
                        
                        TryMoveEnemy(enemy, dx, dy);
                    }
                }
            }
            
            // Get direction to exit for the status display
            string exitDirection = _gameWorld.GetDirectionToExit(_player.X, _player.Y);
            
            // Create a periodic reminder about the exit (roughly 5% chance per update)
            if (_random.Next(20) == 0)
            {
                AddMessage($"[yellow]Exit is to the {exitDirection}[/]");
            }
            
            // Add a notification when getting close to the exit
            var exitPos = _gameWorld.GetExitPosition();
            int distanceToExit = Math.Abs(_player.X - exitPos.X) + Math.Abs(_player.Y - exitPos.Y);
            
            if (distanceToExit <= 8)
            {
                if (distanceToExit <= 3)
                {
                    if (_random.Next(2) == 0) // 50% chance when very close
                    {
                        AddMessage("[bold magenta]You're very close to the exit![/]");
                    }
                }
                else if (_random.Next(4) == 0) // 25% chance when moderately close
                {
                    AddMessage("[magenta]You're getting close to the exit![/]");
                }
            }
            
            // Check for special tiles (exit only, shop is handled in movement)
            _gameWorld.CheckSpecialTiles(_player.X, _player.Y);
            
            // NOTE: Shop detection is now handled directly in TryMovePlayer
            // This avoids duplicate handling that could cause conflicts
        }
        
        private bool TryMoveEnemy(Enemy enemy, int dx, int dy)
        {
            int newX = enemy.X + dx;
            int newY = enemy.Y + dy;
            
            // Check if player is at the target position
            bool playerAtTarget = (newX == _player.X && newY == _player.Y);
            
            // Check if the new position is within bounds and walkable
            if (newX >= 0 && newX < _mapWidth && newY >= 0 && newY < _mapHeight &&
                IsWalkable(newX, newY))
            {
                // Check if there's a player at the new position
                if (playerAtTarget)
                {
                    // Store player's HP before attack
                    int playerHpBefore = _player.HP;
                    
                    // Force attack cooldown reset to ensure attack happens
                    enemy.LastAttackTime = DateTime.MinValue;
                    
                    // Attack the player
                    enemy.Attack(_player);
                    
                    // Calculate actual damage dealt
                    int damageDealt = playerHpBefore - _player.HP;
                    
                    // Log the attack and damage
                    if (damageDealt > 0)
                    {
                        AddMessage($"[red]The {enemy.Type} attacks you for {damageDealt} damage! ({_player.HP}/{_player.MaxHP} HP)[/]");
                    }
                    else
                    {
                        // If no damage was dealt, force damage application
                        int forcedDamage = Math.Max(1, enemy.Damage - _player.Defense);
                        _player.HP -= forcedDamage;
                        AddMessage($"[red]The {enemy.Type} attacks you for {forcedDamage} damage! ({_player.HP}/{_player.MaxHP} HP)[/]");
                    }
                    
                    // Check for player death
                    if (!_player.IsAlive)
                    {
                        AddMessage("[red bold]You have been defeated![/]");
                        _gameOver = true;
                    }
                    
                    return true; // Successfully attacked
                }
                
                // Check if there's another enemy at the new position
                if (_enemies.Any(e => e.IsAlive && e.X == newX && e.Y == newY))
                {
                    return false; // Position occupied by another enemy
                }
                
                // Move the enemy to the new position
                enemy.X = newX;
                enemy.Y = newY;
                return true;
            }
            
            return false; // Could not move
        }
        
        private bool IsWalkable(int x, int y)
        {
            // Check map bounds and walls
            if (x < 0 || x >= _mapWidth || y < 0 || y >= _mapHeight || _map[x, y] == TileType.Wall)
            {
                return false;
            }
            
            // Check for player (entities can't walk through each other)
            if (x == _player.X && y == _player.Y)
            {
                return false;
            }
            
            // Check for other enemies
            foreach (var enemy in _enemies)
            {
                if (enemy.IsAlive && enemy.X == x && enemy.Y == y)
                {
                    return false;
                }
            }
            
            // Check for special tiles
            var exitPos = _gameWorld.GetExitPosition();
            var shopPos = _gameWorld.GetShopPosition();
            
            if ((exitPos.X == x && exitPos.Y == y) || (shopPos.X == x && shopPos.Y == y))
            {
                return false;
            }
            
            return true;
        }
        
        private void OpenInventory()
        {
            Item? selectedItem = _renderer.ShowInventory(_player.Inventory);
            
            if (selectedItem != null)
            {
                if (_player.UseItem(selectedItem))
                {
                    switch (selectedItem.Type)
                    {
                        case ItemType.Consumable:
                            AddMessage($"You used {selectedItem.Name} and restored health.");
                            break;
                            
                        case ItemType.Weapon:
                            AddMessage($"You equipped {selectedItem.Name}.");
                            break;
                            
                        case ItemType.Armor:
                            AddMessage($"You equipped {selectedItem.Name}.");
                            break;
                    }
                }
            }
        }
        
        private void OpenQuestLog()
        {
            _renderer.ShowQuestLog(_player.Quests);
        }
        
        private void AddMessage(string message)
        {
            lock (_messageLock)
            {
                _messageLog.Add(message);
                
                // Trim message log to max size
                while (_messageLog.Count > MaxLogMessages)
                {
                    _messageLog.RemoveAt(0);
                }
            }
        }
        
        private void ShowShop()
        {
            // Set the shop interaction time for cooldown
            _lastShopInteraction = DateTime.Now;
            
            Console.Clear();
            
            // Let the player know about the shop cooldown system
            AnsiConsole.MarkupLine("[yellow]Note: After leaving, the shop will be unavailable for 5 seconds.[/]");
            Thread.Sleep(1000);
            
            // Display shop UI
            _shopUI.ShowShop();
            
            // Add a small delay before returning to dungeon
            Thread.Sleep(500);
            
            // Clear any pending input
            while (Console.KeyAvailable)
            {
                Console.ReadKey(true);
            }
            
            // Set shop cooldown message
            AddMessage($"[yellow]Shop cooldown: {ShopCooldownSeconds} seconds.[/]");
            _gameWorld.ReturnFromShop();
        }
        
        private void ShowVictoryScreen()
        {
            Console.Clear();
            
            AnsiConsole.Write(
                new FigletText("Victory!")
                    .Centered()
                    .Color(Color.Gold1));
            
            AnsiConsole.MarkupLine("\n[green]Congratulations! You have conquered all dungeons![/]");
            AnsiConsole.MarkupLine($"\nYou reached player level [yellow]{_player.Level}[/].");
            AnsiConsole.MarkupLine($"You have [yellow]{_player.Gold}[/] gold.");
            AnsiConsole.MarkupLine($"You completed [yellow]{_player.Quests.Count(q => q.Completed)}[/] quests.");
            
            AnsiConsole.MarkupLine("\n[gray]Press any key to exit...[/]");
            Console.ReadKey(true);
        }
        
        private void TransitionToNextLevel()
        {
            Console.Clear();
            
            // Show level completion screen
            AnsiConsole.Write(
                new FigletText($"Level {_gameWorld.CurrentLevel} Complete!")
                    .Centered()
                    .Color(Color.Gold1));
            
            AnsiConsole.MarkupLine("\n[green]You found the exit to the next level![/]");
            AnsiConsole.MarkupLine($"\nYou are at level [yellow]{_player.Level}[/] with [yellow]{_player.XP}/{_player.XPThreshold}[/] XP.");
            AnsiConsole.MarkupLine($"You have [yellow]{_player.Gold}[/] gold.");
            AnsiConsole.MarkupLine("\n[gray]Press any key to continue to the next level...[/]");
            
            Console.ReadKey(true);
            
            // Advance to next level
            _gameWorld.AdvanceToNextLevel();
            UpdateCurrentLevelData();
            
            // Show welcome message for new level
            AddMessage($"Welcome to Dungeon Level {_gameWorld.CurrentLevel}!");
            if (_gameWorld.CurrentLevel > 1)
            {
                AddMessage("The enemies here seem stronger than before...");
            }
        }
        
        // Method to force a direct attack from enemy to player
        private void DirectAttackFromEnemy(Enemy enemy)
        {
            // Store player's HP before attack
            int playerHpBefore = _player.HP;
            
            // Force attack cooldown reset to ensure attack happens
            enemy.LastAttackTime = DateTime.MinValue;
            
            // Execute the attack
            enemy.Attack(_player);
            
            // Calculate damage dealt
            int damageDealt = playerHpBefore - _player.HP;
            
            // Add message about the attack
            if (damageDealt > 0)
            {
                AddMessage($"[red]The {enemy.Type} attacks you for {damageDealt} damage! ({_player.HP}/{_player.MaxHP} HP)[/]");
            }
            else
            {
                // If no damage was dealt, force damage application
                int forcedDamage = Math.Max(1, enemy.Damage - _player.Defense);
                _player.HP -= forcedDamage;
                AddMessage($"[red]The {enemy.Type} attacks you for {forcedDamage} damage! ({_player.HP}/{_player.MaxHP} HP)[/]");
            }
            
            // Check for player death
            if (!_player.IsAlive)
            {
                AddMessage("[red bold]You have been defeated![/]");
                _gameOver = true;
            }
        }
    }
} 