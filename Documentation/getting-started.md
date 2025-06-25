 Getting Started with Fatal Odds

This guide will get you up and running with Fatal Odds in under 10 minutes.

## Installation

### Method 1: Unity Package Manager (Recommended)

1. Open Unity Package Manager (`Window → Package Manager`)
2. Click the `+` button in the top-left
3. Select `Add package from git URL`
4. Enter: `Need to make a seperate repo for this after it works.`
5. Click `Add`

The welcome window will appear automatically after installation.

### Method 2: .unitypackage File

1. Download the latest `FatalOdds_v0.1.0.unitypackage` from releases
2. In Unity: `Assets → Import Package → Custom Package`
3. Select the downloaded file
4. Click `Import All`

## First Setup

After installation, Fatal Odds will:
- Show a welcome window with setup instructions
- Create necessary folder structure automatically
- Initialize a clean StatRegistry for your project

## Creating Your First Item

### Step 1: Tag Your Stats

Add StatTag attributes to your player script:

```csharp
using FatalOdds.Runtime;

public class PlayerController : MonoBehaviour 
{
    [StatTag("Health", "Combat")]
    public float health = 100f;
    
    [StatTag("Movement Speed", "Movement")]
    public float moveSpeed = 5f;
    
    [StatTag("Jump Force", "Movement")]
    public float jumpForce = 10f;
    
    [StatTag("Attack Damage", "Combat")]
    public float attackDamage = 20f;
}
```

### Step 2: Scan for Stats

1. Open `Window → Fatal Odds → Item & Ability Creator`
2. Click `🔍 Scan for Stats`
3. You should see your tagged stats appear

### Step 3: Create a Speed Boost Item

1. In the Item & Ability Creator, go to the `🎒 Modifier Items` tab
2. Click `➕ New Modifier Item`
3. Set the name to "Sprint Boots"
4. Set description to "Increases movement speed by 20%. Stacks linearly."
5. Set rarity to "Common"
6. Add a modifier:
   - Target Stat: "Movement Speed"
   - Effect Type: "Percentage Additive"
   - Value: 0.2 (which equals +20%)
7. Click `💾 Generate Asset`

### Step 4: Test Your Item

1. Add a `ModifierManager` component to your player GameObject
2. In play mode, use the Item Spawner to spawn your item
3. Collect it and watch your movement speed increase!

## Basic Setup for Your Game

### 1. Player Setup

Your player needs:
```csharp
[RequireComponent(typeof(ModifierManager))]
public class PlayerController : MonoBehaviour 
{
    [StatTag("Movement Speed", "Player Movement")]
    public float moveSpeed = 5f;
    
    private ModifierManager modifierManager;
    
    void Start()
    {
        modifierManager = GetComponent();
    }
    
    void Update()
    {
        // Get current modified speed
        float currentSpeed = modifierManager.GetStatValue("MovementSpeed_GUID");
        
        // Use for movement
        transform.Translate(Vector3.forward * currentSpeed * Time.deltaTime);
    }
}
```

### 2. Item Collection

```csharp
public class ItemCollector : MonoBehaviour
{
    private void OnTriggerEnter(Collider other)
    {
        var pickup = other.GetComponent();
        if (pickup != null)
        {
            var itemDef = pickup.GetItemDefinition();
            itemDef.ApplyToTarget(gameObject);
            Destroy(other.gameObject);
        }
    }
}
```

### 3. Enemy Drops

Add to your enemies:
```csharp
public class Enemy : MonoBehaviour
{
    void Start()
    {
        // Add automatic item dropping when enemy dies
        var dropper = GetComponent();
        if (dropper == null)
        {
            dropper = gameObject.AddComponent();
        }
    }
}
```

## Next Steps

Now that you have the basics working:

1. **Learn about [Modifier Types](modifier-types.md)** - Understand all the ways to modify stats
2. **Create [Active Abilities](creating-abilities.md)** - Add cooldown-based abilities
3. **Explore [Stacking Behaviors](stacking-behaviors.md)** - Make items stack in interesting ways
4. **Check [Best Practices](best-practices.md)** - Tips for organizing your stats and items

## Common First Issues

### "No stats found after scanning"
- Make sure you have `using FatalOdds.Runtime;` at the top of your script
- Verify your fields are `public` or `[SerializeField] private`
- Check that you're using `[StatTag]` not `[StatTagAttribute]`

### "Modifiers not applying"
- Ensure your GameObject has a `ModifierManager` component
- Call `GetStatValue()` instead of accessing the field directly
- Check the console for any error messages

### "Items not spawning"
- Make sure you have a Universal Item Pickup prefab created
- Run `Window → Fatal Odds → 🎛️ Create Universal Prefab` if needed
- Verify your generated assets exist in the Generated folder

Need more help? Check the [Troubleshooting Guide](troubleshooting.md) or open the in-editor help system.
