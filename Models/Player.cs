using System;
using System.Collections.Generic;

namespace RPG.Models
{
    public class Player : Entity
    {
        public int Level { get; set; }
        public int XP { get; set; }
        public int XPThreshold { get; set; }
        public int BaseAttack { get; set; }
        public int BaseDefense { get; set; }
        public int Defense { get; set; }
        public int Gold { get; set; }
        public List<Item> Inventory { get; private set; }
        public List<string> Abilities { get; private set; }
        public Item? EquippedWeapon { get; set; }
        public Item? EquippedArmor { get; set; }
        public List<Quest> Quests { get; private set; }

        public Player(int x, int y, int hp = 100, int damage = 5) 
            : base(x, y, hp, damage, TimeSpan.FromMilliseconds(500))
        {
            Level = 1;
            XP = 0;
            XPThreshold = 100;
            BaseAttack = damage;
            BaseDefense = 2; // Base defense to reduce damage taken
            Defense = BaseDefense;
            Gold = 50; // Start with some gold
            Inventory = new List<Item>();
            Abilities = new List<string>();
            Quests = new List<Quest>();
        }

        public void GainXP(int amount)
        {
            XP += amount;
            CheckLevelUp();
        }

        private void CheckLevelUp()
        {
            if (XP >= XPThreshold)
            {
                Level++;
                XP -= XPThreshold;
                XPThreshold = (int)(XPThreshold * 1.5);
                MaxHP += 10;
                HP = MaxHP; // Heal to full on level up
                BaseAttack += 2;
                UpdateStats();
                
                // Check for new abilities based on level
                if (Level == 3 && !Abilities.Contains("Heal"))
                {
                    Abilities.Add("Heal");
                }
            }
        }

        public void UpdateStats()
        {
            // Update damage based on weapon and level
            Damage = BaseAttack;
            if (EquippedWeapon != null)
            {
                Damage += EquippedWeapon.Power;
            }

            // Update defense based on armor
            Defense = BaseDefense;
            if (EquippedArmor != null)
            {
                Defense += EquippedArmor.Power;
            }
        }

        public bool UseAbility(string abilityName)
        {
            if (!Abilities.Contains(abilityName))
                return false;

            switch (abilityName)
            {
                case "Heal":
                    int healAmount = 20;
                    HP = Math.Min(MaxHP, HP + healAmount);
                    return true;
                // Add more abilities here
                default:
                    return false;
            }
        }

        public void AddToInventory(Item item)
        {
            Inventory.Add(item);
        }

        public bool UseItem(Item item)
        {
            if (!Inventory.Contains(item))
                return false;

            switch (item.Type)
            {
                case ItemType.Consumable:
                    // Apply consumable effect (e.g., healing)
                    HP = Math.Min(MaxHP, HP + item.Power);
                    Inventory.Remove(item);
                    return true;
                    
                case ItemType.Weapon:
                    EquippedWeapon = item;
                    UpdateStats();
                    return true;
                    
                case ItemType.Armor:
                    EquippedArmor = item;
                    UpdateStats();
                    return true;
                    
                default:
                    return false;
            }
        }

        public void AddQuest(Quest quest)
        {
            if (!Quests.Exists(q => q.Description == quest.Description))
            {
                Quests.Add(quest);
            }
        }

        public override void Attack(Entity target)
        {
            base.Attack(target);
            
            // Check if the attack killed an enemy
            if (target is Enemy enemy && !enemy.IsAlive)
            {
                GainXP(enemy.XPValue);
                
                // Update quest progress if applicable
                foreach (var quest in Quests)
                {
                    if (!quest.Completed && quest.Target == enemy.Type)
                    {
                        quest.CurrentCount++;
                        if (quest.CurrentCount >= quest.TargetCount)
                        {
                            quest.Completed = true;
                            GainXP(quest.RewardXP);
                        }
                    }
                }
            }
        }
        
        // Override TakeDamage to account for player's defense
        public override void TakeDamage(int amount)
        {
            // Calculate actual damage after applying defense
            int actualDamage = Math.Max(1, amount - Defense);
            
            // Apply damage
            HP -= actualDamage;
        }
    }
} 