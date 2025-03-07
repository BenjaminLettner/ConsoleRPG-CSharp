namespace RPG.Models
{
    public class Quest
    {
        public string Description { get; set; }
        public bool Completed { get; set; }
        public int RewardXP { get; set; }
        public string? Target { get; set; }
        public int TargetCount { get; set; }
        public int CurrentCount { get; set; }
        
        public Quest(string description, int rewardXP, string? target = null, int targetCount = 0)
        {
            Description = description;
            RewardXP = rewardXP;
            Target = target;
            TargetCount = targetCount;
            CurrentCount = 0;
            Completed = false;
        }
        
        public string GetProgress()
        {
            if (Completed)
                return "Completed";
                
            if (TargetCount > 0)
                return $"{CurrentCount}/{TargetCount}";
                
            return "In progress";
        }
        
        // Factory methods for creating common quest types
        public static Quest CreateKillQuest(string enemyType, int count, int rewardXP)
        {
            return new Quest($"Defeat {count} {enemyType}s", rewardXP, enemyType, count);
        }
        
        public static Quest CreateFetchQuest(string itemName, int rewardXP)
        {
            return new Quest($"Find the {itemName}", rewardXP);
        }
    }
} 