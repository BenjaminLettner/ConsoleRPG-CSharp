using System;

namespace RPG.Models
{
    public enum EnemyState
    {
        Patrolling,
        Chasing
    }
    
    public class Enemy : Entity
    {
        public string Type { get; set; }
        public EnemyState State { get; set; }
        public int XPValue { get; set; }
        public int DetectionRange { get; set; }
        
        public Enemy(int x, int y, string type, int hp = 20, int damage = 5, int xpValue = 10, int detectionRange = 5) 
            : base(x, y, hp, damage, TimeSpan.FromMilliseconds(800))
        {
            Type = type;
            State = EnemyState.Patrolling;
            XPValue = xpValue;
            DetectionRange = detectionRange;
        }
        
        // Override to add enemy-specific attack behavior
        public override void Attack(Entity target)
        {
            // Use the base attack logic
            base.Attack(target);
        }
        
        public void UpdateState(int playerX, int playerY)
        {
            if (!IsAlive) return;
            
            int distX = playerX - X;
            int distY = playerY - Y;
            int manhattanDist = Math.Abs(distX) + Math.Abs(distY);
            
            if (manhattanDist <= DetectionRange)
            {
                State = EnemyState.Chasing;
            }
            else
            {
                State = EnemyState.Patrolling;
            }
        }
        
        public void Move(int playerX, int playerY, Random random, Func<int, int, bool> isWalkable)
        {
            if (!IsAlive) return;
            
            int newX = X;
            int newY = Y;
            
            if (State == EnemyState.Chasing)
            {
                // Move toward the player
                int distX = playerX - X;
                int distY = playerY - Y;
                
                // Prioritize X or Y movement randomly to add some variation
                if (random.Next(2) == 0)
                {
                    if (distX != 0) newX += distX > 0 ? 1 : -1;
                    else if (distY != 0) newY += distY > 0 ? 1 : -1;
                }
                else
                {
                    if (distY != 0) newY += distY > 0 ? 1 : -1;
                    else if (distX != 0) newX += distX > 0 ? 1 : -1;
                }
            }
            else // Patrolling
            {
                // Random movement with low probability to move
                if (random.Next(100) < 20) // 20% chance to move
                {
                    int direction = random.Next(4);
                    switch (direction)
                    {
                        case 0: newY--; break; // Up
                        case 1: newY++; break; // Down
                        case 2: newX--; break; // Left
                        case 3: newX++; break; // Right
                    }
                }
            }
            
            // Check if the new position is walkable
            if (isWalkable(newX, newY))
            {
                X = newX;
                Y = newY;
            }
        }
    }
} 