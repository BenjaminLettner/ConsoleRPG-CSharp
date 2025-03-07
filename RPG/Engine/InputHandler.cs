using System;

namespace RPG.Engine
{
    public enum GameAction
    {
        None,
        MoveUp,
        MoveDown,
        MoveLeft,
        MoveRight,
        Attack,
        UseAbility,
        OpenInventory,
        OpenQuestLog,
        Quit
    }
    
    public class InputHandler
    {
        // Map from ConsoleKey to GameAction
        private readonly static ConsoleKey[] _moveUpKeys = { ConsoleKey.W, ConsoleKey.UpArrow };
        private readonly static ConsoleKey[] _moveDownKeys = { ConsoleKey.S, ConsoleKey.DownArrow };
        private readonly static ConsoleKey[] _moveLeftKeys = { ConsoleKey.A, ConsoleKey.LeftArrow };
        private readonly static ConsoleKey[] _moveRightKeys = { ConsoleKey.D, ConsoleKey.RightArrow };
        
        public GameAction GetAction(out ConsoleKey key)
        {
            if (!Console.KeyAvailable)
            {
                key = ConsoleKey.NoName;
                return GameAction.None;
            }
            
            // Get key press without displaying it
            var keyInfo = Console.ReadKey(intercept: true);
            key = keyInfo.Key;
            
            // Map key to action
            if (Array.IndexOf(_moveUpKeys, key) >= 0)
                return GameAction.MoveUp;
            
            if (Array.IndexOf(_moveDownKeys, key) >= 0)
                return GameAction.MoveDown;
            
            if (Array.IndexOf(_moveLeftKeys, key) >= 0)
                return GameAction.MoveLeft;
            
            if (Array.IndexOf(_moveRightKeys, key) >= 0)
                return GameAction.MoveRight;
            
            if (key == ConsoleKey.Spacebar)
                return GameAction.Attack;
            
            if (key == ConsoleKey.H)
                return GameAction.UseAbility;
            
            if (key == ConsoleKey.I)
                return GameAction.OpenInventory;
            
            if (key == ConsoleKey.Q)
                return GameAction.OpenQuestLog;
            
            if (key == ConsoleKey.Escape)
                return GameAction.Quit;
            
            return GameAction.None;
        }
        
        public static string GetActionDescription(GameAction action)
        {
            return action switch
            {
                GameAction.MoveUp => "Move Up (W/Up Arrow)",
                GameAction.MoveDown => "Move Down (S/Down Arrow)",
                GameAction.MoveLeft => "Move Left (A/Left Arrow)",
                GameAction.MoveRight => "Move Right (D/Right Arrow)",
                GameAction.Attack => "Attack (Spacebar)",
                GameAction.UseAbility => "Use Ability (H)",
                GameAction.OpenInventory => "Open Inventory (I)",
                GameAction.OpenQuestLog => "Open Quest Log (Q)",
                GameAction.Quit => "Quit Game (Escape)",
                _ => string.Empty
            };
        }
        
        public static string GetAllControls()
        {
            return 
                "W/↑: Move Up\n" +
                "S/↓: Move Down\n" +
                "A/←: Move Left\n" +
                "D/→: Move Right\n" +
                "Space: Attack\n" +
                "H: Use Heal Ability (when available)\n" +
                "I: Open Inventory\n" +
                "Q: View Quest Log\n" +
                "ESC: Quit Game";
        }
    }
} 