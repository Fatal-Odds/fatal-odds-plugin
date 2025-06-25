# API Reference

Complete reference for Fatal Odds scripting API.

## Core Attributes

### StatTagAttribute

Mark fields as modifiable stats.

```csharp
[StatTag(string statName = null, string category = "General")]
[StatTag(string statName, string category, string description, bool showInUI = true)]
```

**Examples:**
```csharp
[StatTag("Health", "Combat")]
public float health = 100f;

[StatTag("Critical Chance", "Combat", "Chance to deal double damage", true)]
public float critChance = 0.1f;
```

## Runtime Classes

### ModifierManager

Manages all stat modifications for a GameObject.

```csharp
public class ModifierManager : MonoBehaviour
{
    // Properties
    public int ActiveModifierCount { get; }
    public IReadOnlyDictionary ItemStacks { get; }
    
    // Methods
    public void AddModifier(StatModifier modifier)
    public void RemoveModifier(StatModifier modifier)
    public void RemoveModifiersFromSource(string source)
    public float GetStatValue(string statGuid)
    public void ApplyCalculatedValue(string statGuid)
    public void SetItemStackCount(string itemName, int stacks)
    public int GetItemStackCount(string itemName)
    
    // Events
    public event Action OnModifierAdded;
    public event Action OnModifierRemoved;
    public event Action OnStatChanged;
}
```

### StatModifier

Represents a single stat modification.

```csharp
public class StatModifier
{
    // Static Creation
    public static StatModifierBuilder Create(string statGuid, ModifierType type, float value)
    
    // Properties
    public string StatGuid { get; }
    public ModifierType Type { get; }
    public float Value { get; }
    public string Source { get; }
    public bool IsTemporary { get; }
    public float Duration { get; }
    
    // Methods
    public float Apply(float baseValue, float currentValue)
    public bool IsExpired()
}
```

### ItemDefinition

ScriptableObject for items.

```csharp
[CreateAssetMenu(fileName = "New Item", menuName = "Fatal Odds/Item Definition")]
public class ItemDefinition : ScriptableObject
{
    // Properties
    public string itemName;
    public string description;
    public ItemRarity rarity;
    public ItemStackingType stackingType;
    public List modifiers;
    
    // Methods
    public void ApplyToTarget(GameObject target, int stackCount = 1)
    public void RemoveFromTarget(GameObject target)
    public float GetModifierForStat(string statGuid)
    public bool AffectsStat(string statGuid)
    public string GetFullDescription()
}
```

### AbilityDefinition

ScriptableObject for abilities.

```csharp
[CreateAssetMenu(fileName = "New Ability", menuName = "Fatal Odds/Ability Definition")]
public class AbilityDefinition : ScriptableObject
{
    // Properties
    public string abilityName;
    public string description;
    public AbilityTargetType targetType;
    public float cooldown;
    public float energyCost;
    public List modifiers;
    
    // Methods
    public bool CanUseOnTarget(GameObject caster, GameObject target)
    public bool IsInRange(Vector3 casterPosition, Vector3 targetPosition)
    public void ApplyToTarget(GameObject caster, GameObject target, Vector3 position)
    public string GetFullDescription()
}
```

## Enums

### ModifierType

```csharp
public enum ModifierType
{
    Flat,                     // +10 to stat
    Percentage,               // ×1.25 (multiply by value)
    PercentageAdditive,       // +25% (add percentage before applying)
    PercentageMultiplicative, // Multiply percentages together
    Override,                 // Set to exact value
    Minimum,                  // Enforce minimum value
    Maximum,                  // Enforce maximum value
    Hyperbolic,              // Diminishing returns
    Exponential,             // Exponential scaling
    Logarithmic              // Logarithmic scaling
}
```

### StackingBehavior

```csharp
public enum StackingBehavior
{
    Additive,        // Add all values together
    Multiplicative,  // Multiply all values together
    Override,        // Use only highest/latest value
    Average,         // Average all values
    Highest,         // Use only highest value
    Lowest          // Use only lowest value
}
```

### ItemRarity

```csharp
public enum ItemRarity
{
    Common,     // White
    Uncommon,   // Green
    Rare,       // Blue
    Epic,       // Purple
    Legendary,  // Yellow
    Artifact    // Red
}
```

## Utility Classes

### ItemSpawner

Spawns items in the world.

```csharp
public static class ItemSpawner
{
    // Main spawning method
    public static GameObject SpawnUniversalItem(ItemDefinition item, Vector3 position, 
        int stackCount = 1, Quaternion rotation = default, Transform parent = null)
    
    // Convenience methods
    public static GameObject SpawnItemOfRarity(ItemRarity rarity, Vector3 position)
    public static GameObject SpawnCommonItem(Vector3 position)
    public static GameObject SpawnLegendaryItem(Vector3 position)
    
    // Utility methods
    public static List GetAllItems()
    public static List GetItemsByRarity(ItemRarity rarity)
    public static ItemDefinition GetRandomItemOfRarity(ItemRarity rarity)
    public static void ClearAllPickups()
    public static void RefreshItemCache()
}
```

### StatRegistry

Central registry of all discovered stats.

```csharp
public class StatRegistry : ScriptableObject
{
    // Properties
    public IReadOnlyList RegisteredStats { get; }
    public int StatCount { get; }
    public string LastScanTime { get; }
    
    // Methods
    public void ScanForStats()
    public StatInfo GetStat(string guid)
    public List GetStatsByCategory(string category)
    public List GetCategories()
    public string GetDebugInfo()
}
```

## Events and Callbacks

### ModifierManager Events

```csharp
// Subscribe to stat changes
modifierManager.OnStatChanged += (statGuid) => {
    Debug.Log($"Stat {statGuid} changed!");
};

// Subscribe to modifier additions
modifierManager.OnModifierAdded += (modifier) => {
    Debug.Log($"Added modifier from {modifier.Source}");
};

// Subscribe to item stack changes
modifierManager.OnItemStackChanged += (itemName, newStackCount) => {
    Debug.Log($"{itemName} now has {newStackCount} stacks");
};
```

## Common Usage Patterns

### Creating Items Programmatically

```csharp
public ItemDefinition CreateSpeedBoost()
{
    var item = ScriptableObject.CreateInstance();
    item.itemName = "Speed Boost";
    item.description = "Increases movement speed by 25%";
    item.rarity = ItemRarity.Common;
    item.stackingType = ItemStackingType.Linear;
    
    item.modifiers = new List
    {
        new StatModifierData("MovementSpeed_GUID", ModifierType.PercentageAdditive, 0.25f, "Movement Speed")
    };
    
    return item;
}
```

### Custom Modifier Logic

```csharp
public void ApplyCustomModifier(GameObject target, string statGuid, float value, float duration)
{
    var modifier = StatModifier.Create(statGuid, ModifierType.Flat, value)
        .WithSource("CustomSource")
        .WithDuration(duration)
        .Build();
    
    var modifierManager = target.GetComponent();
    modifierManager.AddModifier(modifier);
}
```

### Reading Current Stat Values

```csharp
public void UpdatePlayerMovement()
{
    var modifierManager = GetComponent();
    
    // Get modified stat values
    float currentSpeed = modifierManager.GetStatValue("MovementSpeed_GUID");
    float currentJumpHeight = modifierManager.GetStatValue("JumpHeight_GUID");
    
    // Use in movement logic
    rigidbody.velocity = Vector3.forward * currentSpeed;
}
```

### Conditional Modifiers

```csharp
// Create modifier that only applies when health is low
var conditionalModifier = StatModifier.Create("AttackSpeed_GUID", ModifierType.Percentage, 2.0f)
    .WithSource("BerserkerRage")
    .WithCondition("health < 25")
    .Build();
```

## Editor Extension

### Custom Inspector Integration

```csharp
[CustomEditor(typeof(MyPlayerScript))]
public class MyPlayerScriptEditor : Editor
{
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();
        
        if (GUILayout.Button("Scan This Object for Stats"))
        {
            var statRegistry = Resources.Load("StatRegistry");
            statRegistry.ScanForStats();
        }
    }
}
```

### Creating Items in Editor

```csharp
[MenuItem("Tools/Create Speed Item")]
public static void CreateSpeedItem()
{
    var itemData = new ItemCreationData
    {
        itemName = "Swift Boots",
        description = "Increases movement speed",
        rarity = ItemRarity.Uncommon,
        modifiers = new List
        {
            new ModifierCreationData
            {
                statGuid = "MovementSpeed_GUID",
                modifierType = ModifierType.PercentageAdditive,
                value = 0.3f
            }
        }
    };
    
    AssetGenerator.GenerateItemAsset(itemData);
}
```

## Performance Considerations

### Best Practices

```csharp
public class PerformantPlayerController : MonoBehaviour
{
    private ModifierManager modifierManager;
    private float cachedSpeed;
    private bool speedDirty = true;
    
    void Start()
    {
        modifierManager = GetComponent();
        // Subscribe to changes to know when to recalculate
        modifierManager.OnStatChanged += OnStatChanged;
    }
    
    void Update()
    {
        if (speedDirty)
        {
            cachedSpeed = modifierManager.GetStatValue("MovementSpeed_GUID");
            speedDirty = false;
        }
        
        // Use cached value
        transform.Translate(Vector3.forward * cachedSpeed * Time.deltaTime);
    }
    
    private void OnStatChanged(string statGuid)
    {
        if (statGuid == "MovementSpeed_GUID")
        {
            speedDirty = true;
        }
    }
}
```

### Avoiding Common Pitfalls

```csharp
// ❌ DON'T: Call GetStatValue every frame
void Update()
{
    float speed = modifierManager.GetStatValue("Speed_GUID"); // Expensive!
    Move(speed);
}

// ✅ DO: Cache values and update on change
private float cachedSpeed;
void Start()
{
    modifierManager.OnStatChanged += (guid) => {
        if (guid == "Speed_GUID") {
            cachedSpeed = modifierManager.GetStatValue("Speed_GUID");
        }
    };
    cachedSpeed = modifierManager.GetStatValue("Speed_GUID");
}

void Update()
{
    Move(cachedSpeed); // Fast!
}
```

## Error Handling

### Common Issues and Solutions

```csharp
public void SafeModifierApplication()
{
    var modifierManager = GetComponent();
    if (modifierManager == null)
    {
        Debug.LogError("No ModifierManager found!");
        return;
    }
    
    try
    {
        var modifier = StatModifier.Create("InvalidGUID", ModifierType.Flat, 10f).Build();
        modifierManager.AddModifier(modifier);
    }
    catch (System.Exception e)
    {
        Debug.LogError($"Failed to add modifier: {e.Message}");
    }
}

public float GetStatValueSafely(string statGuid, float defaultValue = 0f)
{
    var modifierManager = GetComponent();
    if (modifierManager == null) return defaultValue;
    
    try
    {
        return modifierManager.GetStatValue(statGuid);
    }
    catch
    {
        return defaultValue;
    }
}
```

## Integration Examples

### With Health System

```csharp
public class HealthSystem : MonoBehaviour
{
    [StatTag("Max Health", "Player Combat")]
    public float maxHealth = 100f;
    
    [StatTag("Health Regeneration", "Player Combat")]
    public float healthRegen = 1f;
    
    private float currentHealth;
    private ModifierManager modifierManager;
    
    void Start()
    {
        modifierManager = GetComponent();
        currentHealth = modifierManager.GetStatValue("MaxHealth_GUID");
    }
    
    void Update()
    {
        float regenRate = modifierManager.GetStatValue("HealthRegen_GUID");
        currentHealth += regenRate * Time.deltaTime;
        
        float maxHP = modifierManager.GetStatValue("MaxHealth_GUID");
        currentHealth = Mathf.Min(currentHealth, maxHP);
    }
}
```

### With Weapon System

```csharp
public class WeaponSystem : MonoBehaviour
{
    [StatTag("Attack Damage", "Player Combat")]
    public float baseDamage = 20f;
    
    [StatTag("Attack Speed", "Player Combat")]
    public float attackSpeed = 1f;
    
    [StatTag("Critical Chance", "Player Combat")]
    public float critChance = 0.1f;
    
    public void Attack()
    {
        var modifierManager = GetComponent();
        
        float damage = modifierManager.GetStatValue("AttackDamage_GUID");
        float critRate = modifierManager.GetStatValue("CriticalChance_GUID");
        
        if (Random.value < critRate)
        {
            damage *= 2f; // Critical hit
        }
        
        // Apply damage...
    }
}
```

---

*This API reference covers Fatal Odds v0.1.0. For the most up-to-date information, check the in-editor help system.*