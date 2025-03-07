namespace RPG.Models
{
    public enum ItemType
    {
        Weapon,
        Armor,
        Consumable
    }
    
    public class Item
    {
        public string Name { get; set; }
        public ItemType Type { get; set; }
        public int Power { get; set; }
        public string Description { get; set; }
        
        public Item(string name, ItemType type, int power, string description = "")
        {
            Name = name;
            Type = type;
            Power = power;
            Description = description;
        }
        
        public override string ToString()
        {
            return Name;
        }
        
        // Factory methods for creating common items
        public static Item CreateSword()
        {
            return new Item("Sword", ItemType.Weapon, 5, "A sharp sword that deals +5 damage.");
        }
        
        public static Item CreateAxe()
        {
            return new Item("Battle Axe", ItemType.Weapon, 8, "A heavy battle axe that deals +8 damage.");
        }
        
        public static Item CreateLeatherArmor()
        {
            return new Item("Leather Armor", ItemType.Armor, 2, "Basic leather armor that gives +2 defense.");
        }
        
        public static Item CreateChainMail()
        {
            return new Item("Chain Mail", ItemType.Armor, 5, "Strong chain mail that gives +5 defense.");
        }
        
        public static Item CreateHealthPotion()
        {
            return new Item("Health Potion", ItemType.Consumable, 20, "A potion that restores 20 HP when consumed.");
        }
        
        public static Item CreateStrengthPotion()
        {
            return new Item("Strength Potion", ItemType.Consumable, 5, "Temporarily increases attack power by 5.");
        }
    }
} 