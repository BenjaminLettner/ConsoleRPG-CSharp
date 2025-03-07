using System;

namespace RPG.Models
{
    public abstract class Entity
    {
        // Basic properties
        public int X { get; set; }
        public int Y { get; set; }
        
        // Health properties
        private int _hp;
        private int _maxHp;
        
        public int HP 
        { 
            get => _hp;
            set => _hp = Math.Max(0, Math.Min(value, _maxHp)); // Clamp between 0 and MaxHP
        }
        
        public int MaxHP 
        { 
            get => _maxHp;
            set 
            {
                _maxHp = Math.Max(1, value); // Ensure max HP is at least 1
                if (_hp > _maxHp) _hp = _maxHp;
            }
        }
        
        // Combat properties
        public int Damage { get; set; }
        public DateTime LastAttackTime { get; set; }
        public TimeSpan AttackCooldown { get; set; }
        
        // Computed properties
        public bool IsAlive => HP > 0;

        protected Entity(int x, int y, int hp, int damage, TimeSpan? attackCooldown = null)
        {
            X = x;
            Y = y;
            _maxHp = Math.Max(1, hp);
            _hp = _maxHp;
            Damage = Math.Max(1, damage);
            LastAttackTime = DateTime.MinValue;
            AttackCooldown = attackCooldown ?? TimeSpan.FromMilliseconds(500);
        }

        public bool CanAttack()
        {
            return (DateTime.Now - LastAttackTime) > AttackCooldown;
        }

        public virtual void Attack(Entity target)
        {
            // Basic safety checks
            if (!IsAlive || !target.IsAlive || !CanAttack()) 
                return;
            
            // Apply damage
            target.TakeDamage(Damage);
            
            // Update last attack time
            LastAttackTime = DateTime.Now;
        }

        public virtual void TakeDamage(int amount)
        {
            // Apply damage to HP
            HP -= amount;
        }
    }
} 