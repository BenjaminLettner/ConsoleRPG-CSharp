# Console-Based RPG Game in C#

A fully-featured, text-based dungeon crawler RPG built with C# and Spectre.Console. 

## Features

- **Smooth Movement System**: Navigate through dungeons using WASD or arrow keys
- **Engaging Combat**: Battle various enemy types with a turn-based combat system
- **Character Progression**: Gain XP and level up your character
- **Equipment System**: Find, equip, and manage weapons and armor
- **Shop System**: Buy and sell items at the shop to improve your gear
- **Quest System**: Complete quests for rewards
- **Multiple Levels**: Progress through increasingly difficult dungeon levels
- **Camera System**: Viewport that follows the player through large dungeon maps

## Game Elements

- **Player**: Represented by '@', the player can explore, fight, and interact with the game world
- **Enemies**: Various enemy types with different behaviors and strengths
- **Items**: Weapons, armor, and consumables to find and use
- **Shop**: Purchase upgrades and supplies, marked with a '$' on the map
- **Exit**: Find the exit ('>') to progress to the next level

## Controls

- **Movement**: WASD or Arrow keys
- **Attack**: Spacebar (when adjacent to enemies)
- **Inventory**: I
- **Quest Log**: Q
- **Help**: H
- **Quit**: Escape

## Technical Features

- **Multithreaded Architecture**: Separate threads for input, rendering, and game logic
- **Enhanced Rendering**: Uses Spectre.Console for colored text and structured layouts
- **Map Generation**: Procedurally generated levels with varied terrain
- **Combat System**: Balanced combat with attack cooldowns and defense calculations
- **State Management**: Clean state transitions between dungeon exploration, combat, and shop interfaces

## Requirements

- .NET 8.0 or higher
- Spectre.Console package

## Building and Running

```bash
# Clone the repository
git clone https://github.com/BenjaminLettner/ConsoleRPG-CSharp.git

# Navigate to the project directory
cd ConsoleRPG-CSharp/RPG

# Build the project
dotnet build

# Run the game
dotnet run
```

## Screenshots

(Screenshots will be added here)

## Future Enhancements

- Saving and loading game progress
- More enemy types and behaviors
- Additional dungeon features and traps
- Extended quest system
- More item variety
- Boss battles

## Credits

Built with C# and [Spectre.Console](https://spectreconsole.net/) for terminal rendering.

## License

MIT License 