 Fatal Odds - Modifier System

![Unity Version](https://img.shields.io/badge/Unity-2021.3%2B-blue)
![Version](https://img.shields.io/badge/Version-0.1.0-green)
![License](https://img.shields.io/badge/License-MIT-orange)

A comprehensive Unity plugin for creating stacking modifier items and abilities, designed for action roguelike games like Risk of Rain.

## ✨ Features

- 🏷️ **Automatic Stat Discovery** - Tag fields with `[StatTag]` and let the system find them
- 🎒 **Stacking Modifier Items** - Items stack in effect, not inventory (Risk of Rain style)
- ⏰ **Active Abilities** - Cooldown-based abilities with temporary effects
- 🎨 **Visual Editor** - Intuitive GUI for creating items and abilities
- 🔧 **Runtime Debugging** - Live stat monitoring and calculation breakdowns
- 📦 **Cross-Project Ready** - Easy deployment across multiple Unity projects

## 🚀 Quick Start

### 1. Installation

**Via Unity Package Manager:**
1. Open `Window → Package Manager`
2. Click `+` → `Add package from git URL`
3. Enter: `https://github.com/your-org/fatal-odds.git`

**Via .unitypackage:**
1. Download the latest release
2. Import via `Assets → Import Package → Custom Package`

### 2. Tag Your Stats

```csharp
using FatalOdds.Runtime;

public class PlayerController : MonoBehaviour 
{
    [StatTag("Health", "Combat")]
    public float health = 100f;
    
    [StatTag("Movement Speed", "Movement")]
    public float moveSpeed = 5f;
    
    [StatTag("Jump Height", "Movement")]
    public float jumpHeight = 2f;
}
```

### 3. Create Your First Item

1. Open `Window → Fatal Odds → Item & Ability Creator`
2. Click `Scan for Stats` to discover your tagged fields
3. Create a new modifier item
4. Add stat modifiers (e.g., +15% Movement Speed)
5. Generate the asset and test!

### 4. Use in Game

```csharp
// Apply item to player
public void CollectItem(ItemDefinition item, GameObject player)
{
    item.ApplyToTarget(player);
}

// Check current stat value
var modifierManager = player.GetComponent();
float currentSpeed = modifierManager.GetStatValue("MovementSpeed_GUID");
```

## 📚 Documentation

- **Getting Started**: [Documentation~/getting-started.md](Documentation~/getting-started.md)
- **API Reference**: [Documentation~/api-reference.md](Documentation~/api-reference.md)
- **In-Editor Help**: `Window → Fatal Odds → Help & Documentation`

## 🎮 Perfect For

- Action Roguelikes (Risk of Rain, Dead Cells style)
- RPG Character Progression
- Bullet Hell Power-ups
- Any game needing flexible stat modification

## 🔧 Requirements

- Unity 2021.3 or later
- .NET Standard 2.1
- TextMeshPro (automatically included)

## 📄 License

MIT License - see [LICENSE](LICENSE) file for details.

## 🏫 Credits

Created by Ryan, Jandre, William  
Media Design School - Game Engine Development

---

**Fatal Odds** - *Because every modifier should stack, and every stack should matter.*