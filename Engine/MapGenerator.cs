using System;
using System.Collections.Generic;
using RPG.Models;

namespace RPG.Engine
{
    public enum TileType
    {
        Floor,
        Wall
    }
    
    public class MapGenerator
    {
        private readonly Random _random;
        private readonly int _width;
        private readonly int _height;
        private TileType[,] _map;
        
        public TileType[,] Map => _map;
        public int Width => _width;
        public int Height => _height;
        
        public MapGenerator(int width, int height, Random? random = null)
        {
            _width = width;
            _height = height;
            _random = random ?? new Random();
            _map = new TileType[width, height];
        }
        
        public TileType[,] GenerateCaveDungeon(int wallPercent = 28, int smoothingIterations = 4)
        {
            // 1. Initialize with random walls
            RandomFill(wallPercent);
            
            // 2. Smooth the map using cellular automata
            for (int i = 0; i < smoothingIterations; i++)
            {
                SmoothMap();
            }
            
            // 3. Ensure borders are walls
            AddBorders();
            
            // 4. Create features for larger maps
            CreateDungeonFeatures();
            
            // 5. Return the generated map
            return _map;
        }
        
        private void RandomFill(int wallPercent)
        {
            for (int y = 0; y < _height; y++)
            {
                for (int x = 0; x < _width; x++)
                {
                    // The borders are always walls
                    if (x == 0 || x == _width - 1 || y == 0 || y == _height - 1)
                    {
                        _map[x, y] = TileType.Wall;
                    }
                    else
                    {
                        // Random walls or floor
                        _map[x, y] = _random.Next(100) < wallPercent ? TileType.Wall : TileType.Floor;
                    }
                }
            }
        }
        
        private void SmoothMap()
        {
            TileType[,] newMap = new TileType[_width, _height];
            
            // Apply the 4-5 rule: A cell becomes a wall if it was a wall and 4+ neighbors are walls,
            // or if it was a floor and 5+ neighbors are walls
            for (int y = 0; y < _height; y++)
            {
                for (int x = 0; x < _width; x++)
                {
                    int wallCount = CountWallNeighbors(x, y);
                    
                    if (_map[x, y] == TileType.Wall)
                    {
                        // Use 5 instead of 4 to create slightly fewer walls
                        newMap[x, y] = wallCount >= 5 ? TileType.Wall : TileType.Floor;
                    }
                    else
                    {
                        // Use 6 instead of 5 to create more open space
                        newMap[x, y] = wallCount >= 6 ? TileType.Wall : TileType.Floor;
                    }
                }
            }
            
            _map = newMap;
        }
        
        private int CountWallNeighbors(int x, int y)
        {
            int count = 0;
            
            for (int ny = y - 1; ny <= y + 1; ny++)
            {
                for (int nx = x - 1; nx <= x + 1; nx++)
                {
                    // Skip the current cell itself
                    if (nx == x && ny == y)
                        continue;
                        
                    // If out of bounds, count as wall
                    if (nx < 0 || ny < 0 || nx >= _width || ny >= _height)
                    {
                        count++;
                    }
                    else if (_map[nx, ny] == TileType.Wall)
                    {
                        count++;
                    }
                }
            }
            
            return count;
        }
        
        private void AddBorders()
        {
            for (int x = 0; x < _width; x++)
            {
                _map[x, 0] = TileType.Wall;
                _map[x, _height - 1] = TileType.Wall;
            }
            
            for (int y = 0; y < _height; y++)
            {
                _map[0, y] = TileType.Wall;
                _map[_width - 1, y] = TileType.Wall;
            }
        }
        
        // Enhanced version of CreatePathways with more interesting features
        private void CreateDungeonFeatures()
        {
            // Create primary horizontal and vertical corridors
            CreateMainCorridors();
            
            // Add random rooms throughout the map
            CreateRooms(3 + _width / 20); // Scale rooms with map size
            
            // Create some cave-like areas
            CreateCaveAreas(2 + _width / 30);
            
            // Add some water or lava pools (represented as special floor tiles)
            CreatePools(1 + _width / 40);
            
            // Ensure map has good connectivity
            ConnectIsolatedAreas();
        }
        
        private void CreateMainCorridors()
        {
            // Create a primary horizontal path
            int pathY = _height / 2;
            CreateCorridor(1, pathY, _width - 2, pathY, _random.Next(2, 4));
            
            // Create a primary vertical path
            int pathX = _width / 2;
            CreateCorridor(pathX, 1, pathX, _height - 2, _random.Next(2, 4));
            
            // Create a few diagonal corridors
            for (int i = 0; i < _random.Next(2, 5); i++)
            {
                int startX = _random.Next(_width / 4, 3 * _width / 4);
                int startY = _random.Next(_height / 4, 3 * _height / 4);
                int endX = _random.Next(1, _width - 1);
                int endY = _random.Next(1, _height - 1);
                
                CreateWindingCorridor(startX, startY, endX, endY, _random.Next(1, 3));
            }
        }
        
        private void CreateCorridor(int startX, int startY, int endX, int endY, int width)
        {
            // Create a corridor between two points with specified width
            if (startX == endX) // Vertical corridor
            {
                int minY = Math.Min(startY, endY);
                int maxY = Math.Max(startY, endY);
                
                for (int y = minY; y <= maxY; y++)
                {
                    for (int x = startX - width / 2; x <= startX + width / 2; x++)
                    {
                        if (x > 0 && x < _width - 1 && y > 0 && y < _height - 1)
                        {
                            _map[x, y] = TileType.Floor;
                        }
                    }
                }
            }
            else if (startY == endY) // Horizontal corridor
            {
                int minX = Math.Min(startX, endX);
                int maxX = Math.Max(startX, endX);
                
                for (int x = minX; x <= maxX; x++)
                {
                    for (int y = startY - width / 2; y <= startY + width / 2; y++)
                    {
                        if (x > 0 && x < _width - 1 && y > 0 && y < _height - 1)
                        {
                            _map[x, y] = TileType.Floor;
                        }
                    }
                }
            }
        }
        
        private void CreateWindingCorridor(int startX, int startY, int endX, int endY, int width)
        {
            int x = startX;
            int y = startY;
            
            while (x != endX || y != endY)
            {
                // Decide whether to move in X or Y direction
                bool moveX = _random.Next(2) == 0 || y == endY;
                
                if (moveX && x != endX)
                {
                    x += Math.Sign(endX - x);
                }
                else if (y != endY)
                {
                    y += Math.Sign(endY - y);
                }
                
                // Create a segment of corridor
                for (int dx = -width / 2; dx <= width / 2; dx++)
                {
                    for (int dy = -width / 2; dy <= width / 2; dy++)
                    {
                        int nx = x + dx;
                        int ny = y + dy;
                        if (nx > 0 && nx < _width - 1 && ny > 0 && ny < _height - 1)
                        {
                            _map[nx, ny] = TileType.Floor;
                        }
                    }
                }
                
                // Occasionally add a small room off the corridor
                if (_random.Next(20) == 0)
                {
                    int roomSize = _random.Next(3, 6);
                    CreateRoom(x, y, roomSize, roomSize);
                }
            }
        }
        
        private void CreateRooms(int count)
        {
            for (int i = 0; i < count; i++)
            {
                int roomX = _random.Next(5, _width - 10);
                int roomY = _random.Next(5, _height - 10);
                int roomWidth = _random.Next(5, 10);
                int roomHeight = _random.Next(5, 10);
                
                CreateRoom(roomX, roomY, roomWidth, roomHeight);
                
                // Sometimes add special features inside rooms
                if (_random.Next(3) == 0)
                {
                    AddRoomFeature(roomX, roomY, roomWidth, roomHeight);
                }
            }
        }
        
        private void CreateRoom(int x, int y, int width, int height)
        {
            for (int rx = 0; rx < width; rx++)
            {
                for (int ry = 0; ry < height; ry++)
                {
                    int nx = x + rx;
                    int ny = y + ry;
                    if (nx > 0 && nx < _width - 1 && ny > 0 && ny < _height - 1)
                    {
                        _map[nx, ny] = TileType.Floor;
                    }
                }
            }
        }
        
        private void AddRoomFeature(int roomX, int roomY, int roomWidth, int roomHeight)
        {
            int featureType = _random.Next(3);
            
            switch (featureType)
            {
                case 0: // Pillars
                    int centerX = roomX + roomWidth / 2;
                    int centerY = roomY + roomHeight / 2;
                    
                    // Add 1-4 pillars
                    for (int i = 0; i < _random.Next(1, 5); i++)
                    {
                        int pillarX = centerX + (_random.Next(3) - 1) * (roomWidth / 3);
                        int pillarY = centerY + (_random.Next(3) - 1) * (roomHeight / 3);
                        
                        if (pillarX > roomX && pillarX < roomX + roomWidth - 1 &&
                            pillarY > roomY && pillarY < roomY + roomHeight - 1)
                        {
                            _map[pillarX, pillarY] = TileType.Wall;
                        }
                    }
                    break;
                    
                case 1: // Inner chamber
                    int innerX = roomX + roomWidth / 4;
                    int innerY = roomY + roomHeight / 4;
                    int innerWidth = roomWidth / 2;
                    int innerHeight = roomHeight / 2;
                    
                    // Create walls for inner chamber
                    for (int x = innerX; x < innerX + innerWidth; x++)
                    {
                        for (int y = innerY; y < innerY + innerHeight; y++)
                        {
                            if ((x == innerX || x == innerX + innerWidth - 1 || 
                                 y == innerY || y == innerY + innerHeight - 1) &&
                                x > roomX && x < roomX + roomWidth - 1 &&
                                y > roomY && y < roomY + roomHeight - 1)
                            {
                                _map[x, y] = TileType.Wall;
                            }
                        }
                    }
                    
                    // Add door to inner chamber
                    int doorSide = _random.Next(4);
                    int doorPos;
                    
                    switch (doorSide)
                    {
                        case 0: // North
                            doorPos = innerX + _random.Next(innerWidth);
                            if (doorPos >= innerX && doorPos < innerX + innerWidth)
                                _map[doorPos, innerY] = TileType.Floor;
                            break;
                        case 1: // East
                            doorPos = innerY + _random.Next(innerHeight);
                            if (doorPos >= innerY && doorPos < innerY + innerHeight)
                                _map[innerX + innerWidth - 1, doorPos] = TileType.Floor;
                            break;
                        case 2: // South
                            doorPos = innerX + _random.Next(innerWidth);
                            if (doorPos >= innerX && doorPos < innerX + innerWidth)
                                _map[doorPos, innerY + innerHeight - 1] = TileType.Floor;
                            break;
                        case 3: // West
                            doorPos = innerY + _random.Next(innerHeight);
                            if (doorPos >= innerY && doorPos < innerY + innerHeight)
                                _map[innerX, doorPos] = TileType.Floor;
                            break;
                    }
                    break;
                
                case 2: // Checkered floor
                    for (int x = roomX; x < roomX + roomWidth; x++)
                    {
                        for (int y = roomY; y < roomY + roomHeight; y++)
                        {
                            if ((x + y) % 2 == 0 && _random.Next(3) == 0 && 
                                x > roomX && x < roomX + roomWidth - 1 &&
                                y > roomY && y < roomY + roomHeight - 1)
                            {
                                _map[x, y] = TileType.Wall;
                            }
                        }
                    }
                    break;
            }
        }
        
        private void CreateCaveAreas(int count)
        {
            for (int i = 0; i < count; i++)
            {
                int centerX = _random.Next(_width / 4, 3 * _width / 4);
                int centerY = _random.Next(_height / 4, 3 * _height / 4);
                int radius = _random.Next(5, 10);
                
                // Create a rough circular cave
                for (int x = centerX - radius; x <= centerX + radius; x++)
                {
                    for (int y = centerY - radius; y <= centerY + radius; y++)
                    {
                        if (x > 0 && x < _width - 1 && y > 0 && y < _height - 1)
                        {
                            // Calculate distance from center
                            double dist = Math.Sqrt(Math.Pow(x - centerX, 2) + Math.Pow(y - centerY, 2));
                            
                            // Create a rough circle with some noise
                            if (dist < radius + _random.NextDouble() * 2 - 1)
                            {
                                _map[x, y] = TileType.Floor;
                            }
                        }
                    }
                }
            }
        }
        
        private void CreatePools(int count)
        {
            for (int i = 0; i < count; i++)
            {
                int centerX = _random.Next(_width / 5, 4 * _width / 5);
                int centerY = _random.Next(_height / 5, 4 * _height / 5);
                int poolSize = _random.Next(3, 8);
                
                // Create an irregular pool shape
                for (int attempts = 0; attempts < 50; attempts++)
                {
                    int dx = _random.Next(-poolSize, poolSize + 1);
                    int dy = _random.Next(-poolSize, poolSize + 1);
                    
                    int x = centerX + dx;
                    int y = centerY + dy;
                    
                    if (x > 1 && x < _width - 2 && y > 1 && y < _height - 2)
                    {
                        double dist = Math.Sqrt(dx * dx + dy * dy);
                        if (dist <= poolSize)
                        {
                            // Mark as floor to ensure the pool is accessible
                            _map[x, y] = TileType.Floor;
                            
                            // Also clear some surrounding tiles
                            if (_random.Next(100) < 70)
                            {
                                for (int nx = -1; nx <= 1; nx++)
                                {
                                    for (int ny = -1; ny <= 1; ny++)
                                    {
                                        int px = x + nx;
                                        int py = y + ny;
                                        
                                        if (px > 0 && px < _width - 1 && py > 0 && py < _height - 1)
                                        {
                                            _map[px, py] = TileType.Floor;
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
            }
        }
        
        private void ConnectIsolatedAreas()
        {
            // Use flood fill to identify isolated areas
            int[,] regions = new int[_width, _height];
            int regionCount = 0;
            
            // First pass: identify regions
            for (int y = 1; y < _height - 1; y++)
            {
                for (int x = 1; x < _width - 1; x++)
                {
                    if (_map[x, y] == TileType.Floor && regions[x, y] == 0)
                    {
                        // Found a new region
                        regionCount++;
                        FloodFill(x, y, regionCount, regions);
                    }
                }
            }
            
            // If there's only one region, we're already connected
            if (regionCount <= 1)
                return;
            
            // Find connections between regions
            for (int i = 1; i < regionCount; i++)
            {
                int nextRegion = i + 1;
                
                // Find a point in each region
                (int X, int Y) regionAPoint = FindPointInRegion(i, regions);
                (int X, int Y) regionBPoint = FindPointInRegion(nextRegion, regions);
                
                // Create a path between the regions
                CreateWindingCorridor(regionAPoint.X, regionAPoint.Y, regionBPoint.X, regionBPoint.Y, _random.Next(1, 3));
            }
        }
        
        private void FloodFill(int startX, int startY, int regionId, int[,] regions)
        {
            Queue<(int X, int Y)> queue = new Queue<(int X, int Y)>();
            queue.Enqueue((startX, startY));
            regions[startX, startY] = regionId;
            
            while (queue.Count > 0)
            {
                var current = queue.Dequeue();
                
                // Check adjacent tiles
                int[] dx = { -1, 0, 1, 0 };
                int[] dy = { 0, 1, 0, -1 };
                
                for (int i = 0; i < 4; i++)
                {
                    int nx = current.X + dx[i];
                    int ny = current.Y + dy[i];
                    
                    if (nx > 0 && nx < _width - 1 && ny > 0 && ny < _height - 1 &&
                        _map[nx, ny] == TileType.Floor && regions[nx, ny] == 0)
                    {
                        queue.Enqueue((nx, ny));
                        regions[nx, ny] = regionId;
                    }
                }
            }
        }
        
        private (int X, int Y) FindPointInRegion(int regionId, int[,] regions)
        {
            for (int y = 1; y < _height - 1; y++)
            {
                for (int x = 1; x < _width - 1; x++)
                {
                    if (regions[x, y] == regionId)
                    {
                        return (x, y);
                    }
                }
            }
            
            // Fallback if region not found (should not happen)
            return (_width / 2, _height / 2);
        }
        
        public List<Enemy> PlaceEnemies(int count, Player player, string[] enemyTypes)
        {
            var enemies = new List<Enemy>();
            int minDistance = 5; // Minimum distance from player
            
            for (int i = 0; i < count; i++)
            {
                int tries = 0;
                int maxTries = 100;
                bool placed = false;
                
                // Try to find a valid position
                while (!placed && tries < maxTries)
                {
                    int x = _random.Next(1, _width - 1);
                    int y = _random.Next(1, _height - 1);
                    
                    // Check if position is a floor tile and not too close to player
                    if (_map[x, y] == TileType.Floor && 
                        Math.Abs(x - player.X) + Math.Abs(y - player.Y) >= minDistance)
                    {
                        // Select a random enemy type
                        string enemyType = enemyTypes[_random.Next(enemyTypes.Length)];
                        
                        // Create and add enemy
                        var enemy = new Enemy(x, y, enemyType);
                        enemies.Add(enemy);
                        placed = true;
                    }
                    
                    tries++;
                }
            }
            
            return enemies;
        }
        
        public Dictionary<(int, int), Item> PlaceItems(int count, List<Func<Item>> itemFactoryMethods)
        {
            var items = new Dictionary<(int, int), Item>();
            
            for (int i = 0; i < count; i++)
            {
                int tries = 0;
                int maxTries = 100;
                bool placed = false;
                
                // Try to find a valid position
                while (!placed && tries < maxTries)
                {
                    int x = _random.Next(1, _width - 1);
                    int y = _random.Next(1, _height - 1);
                    
                    // Check if position is a floor tile and not already occupied by an item
                    if (_map[x, y] == TileType.Floor && !items.ContainsKey((x, y)))
                    {
                        // Select a random item factory method
                        var itemFactory = itemFactoryMethods[_random.Next(itemFactoryMethods.Count)];
                        
                        // Create and add item
                        items.Add((x, y), itemFactory());
                        placed = true;
                    }
                    
                    tries++;
                }
            }
            
            return items;
        }
        
        public (int, int) FindPlayerStartPosition()
        {
            // First try to find a position in one of the rooms
            int centerX = _width / 2;
            int centerY = _height / 2;
            
            // Check if the center area is open (it's likely to be since we create pathways there)
            if (_map[centerX, centerY] == TileType.Floor &&
                _map[centerX+1, centerY] == TileType.Floor &&
                _map[centerX, centerY+1] == TileType.Floor)
            {
                return (centerX, centerY);
            }
            
            // Try to find an open area by looking for multiple adjacent floor tiles
            for (int attempts = 0; attempts < 100; attempts++)
            {
                int x = _random.Next(3, _width - 3);
                int y = _random.Next(3, _height - 3);
                
                if (_map[x, y] == TileType.Floor)
                {
                    // Check if this position has at least 5 floor tiles around it (including itself)
                    int floorCount = 0;
                    for (int nx = -1; nx <= 1; nx++)
                    {
                        for (int ny = -1; ny <= 1; ny++)
                        {
                            if (_map[x + nx, y + ny] == TileType.Floor)
                            {
                                floorCount++;
                            }
                        }
                    }
                    
                    if (floorCount >= 5)
                    {
                        return (x, y);
                    }
                }
            }
            
            // If all else fails, just find any floor tile
            int margin = 3;
            for (int i = 0; i < 100; i++)
            {
                int x = _random.Next(margin, _width - margin);
                int y = _random.Next(margin, _height - margin);
                
                if (_map[x, y] == TileType.Floor)
                {
                    // Make sure there's at least one free tile around this position
                    for (int nx = -1; nx <= 1; nx++)
                    {
                        for (int ny = -1; ny <= 1; ny++)
                        {
                            if (nx == 0 && ny == 0) continue;
                            
                            int checkX = x + nx;
                            int checkY = y + ny;
                            
                            if (checkX >= 0 && checkX < _width && checkY >= 0 && checkY < _height &&
                                _map[checkX, checkY] == TileType.Floor)
                            {
                                // Create a small clear area around the player
                                for (int cx = -1; cx <= 1; cx++)
                                {
                                    for (int cy = -1; cy <= 1; cy++)
                                    {
                                        int clearX = x + cx;
                                        int clearY = y + cy;
                                        
                                        if (clearX >= 0 && clearX < _width && clearY >= 0 && clearY < _height)
                                        {
                                            _map[clearX, clearY] = TileType.Floor;
                                        }
                                    }
                                }
                                
                                return (x, y);
                            }
                        }
                    }
                }
            }
            
            // Last resort: force an open area in the center
            int middleX = _width / 2;
            int middleY = _height / 2;
            
            // Create a 3x3 open area
            for (int nx = -1; nx <= 1; nx++)
            {
                for (int ny = -1; ny <= 1; ny++)
                {
                    _map[middleX + nx, middleY + ny] = TileType.Floor;
                }
            }
            
            return (middleX, middleY);
        }
        
        public (int, int) FindRandomFloorTile(TileType[,] existingMap)
        {
            int width = existingMap.GetLength(0);
            int height = existingMap.GetLength(1);
            int margin = 3;
            
            // Try to find a good floor tile away from the edges
            for (int attempts = 0; attempts < 100; attempts++)
            {
                int x = _random.Next(margin, width - margin);
                int y = _random.Next(margin, height - margin);
                
                if (existingMap[x, y] == TileType.Floor)
                {
                    // Check if it has some open space around it
                    int floorCount = 0;
                    for (int nx = -1; nx <= 1; nx++)
                    {
                        for (int ny = -1; ny <= 1; ny++)
                        {
                            if (x + nx >= 0 && x + nx < width && y + ny >= 0 && y + ny < height &&
                                existingMap[x + nx, y + ny] == TileType.Floor)
                            {
                                floorCount++;
                            }
                        }
                    }
                    
                    if (floorCount >= 5) // Has open space around it
                    {
                        return (x, y);
                    }
                }
            }
            
            // Fallback to any floor tile
            for (int x = margin; x < width - margin; x++)
            {
                for (int y = margin; y < height - margin; y++)
                {
                    if (existingMap[x, y] == TileType.Floor)
                    {
                        return (x, y);
                    }
                }
            }
            
            // Last resort
            return (width / 2, height / 2);
        }
    }
} 