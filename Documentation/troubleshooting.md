# Troubleshooting Guide

Common issues and their solutions when using Fatal Odds.

## Installation Issues

### Package Won't Import
**Symptoms:** Package import fails or shows errors
**Solutions:**
1. Check Unity version (requires 2021.3+)
2. Ensure TextMeshPro is installed
3. Try importing to a fresh project first
4. Clear Unity's package cache: `%APPDATA%\Unity\Asset Store-5.x`

### Welcome Window Doesn't Appear
**Symptoms:** No welcome window after installation
**Solutions:**
1. Manually open: `Window → Fatal Odds → 📊 Project Overview`
2. Reset installation: `Window → Fatal Odds → 🔄 Reset Package Installation`
3. Check console for compilation errors

## Stat Discovery Issues

### "No Stats Found" After Scanning
**Symptoms:** Stat scan finds 0 stats despite having [StatTag] attributes
**Solutions:**
1. Check you have `using FatalOdds.Runtime;` at the top of your script
2. Ensure fields are `public` or have `[SerializeField]`
3. Verify you're using `[StatTag]` not `[StatTagAttribute]`
4. Make sure the script compiles without errors
5. Try adding StatTag to a simple test script:

```csharp
using FatalOdds.Runtime;
using UnityEngine;

public class TestStats : MonoBehaviour 
{
    [StatTag("Test Stat", "Test")]
    public float testValue = 10f;
}
```

### Stats Found But Can't Select in Editor
**Symptoms:** Stats appear in scan but not in dropdown
**Solutions:**
1. Refresh the editor window
2. Check category filters aren't hiding the stats
3. Reset category filter to "All Categories"
4. Restart Unity if stats still don't appear

### Assembly Reference Errors
**Symptoms:** Can't find FatalOdds.Runtime namespace
**Solutions:**
1. Check assembly definition references
2. Add FatalOdds.Runtime to your script's assembly references
3. Validate package: `Window → Fatal Odds → 🔍 Validate Package`

## Runtime Issues

### Modifiers Not Applying
**Symptoms:** Items collected but stats don't change
**Solutions:**
1. Ensure GameObject has `ModifierManager` component
2. Use `GetStatValue()` instead of accessing field directly:

```csharp
// ❌ Wrong - reads original field value
float speed = moveSpeed;

// ✅ Correct - reads modified value
float speed = modifierManager.GetStatValue("MoveSpeed_GUID");
```

3. Check item actually has modifiers configured
4. Verify stat GUID matches between item and code

### Performance Issues
**Symptoms:** Frame drops when using modifiers
**Solutions:**
1. Cache stat values instead of calling `GetStatValue()` every frame:

```csharp
private float cachedSpeed;

void Start()
{
    modifierManager.OnStatChanged += (guid) => {
        if (guid == "Speed_GUID") {
            cachedSpeed = modifierManager.GetStatValue("Speed_GUID");
        }
    };
}
```

2. Reduce frequency of stat calculations
3. Use events to update UI instead of polling

### Items Not Spawning
**Symptoms:** ItemSpawner methods don't create items
**Solutions:**
1. Check Universal Pickup prefab exists
2. Create prefab: `Window → Fatal Odds → 🎛️ Create Universal Prefab`
3. Verify generated item assets exist in Resources folder
4. Check console for spawn errors

## Editor Window Issues

### Windows Won't Open
**Symptoms:** Menu items don't open editor windows
**Solutions:**
1. Check for compilation errors
2. Reset layout: `Window → Layouts → Default`
3. Clear Unity preferences (will reset all Unity settings)
4. Reimport the package

### Missing UI Elements
**Symptoms:** Buttons or fields missing from windows
**Solutions:**
1. Update Unity to latest 2021.3 LTS version
2. Check UI toolkit is properly installed
3. Reset window layout
4. Close and reopen the problematic window

### Preview Not Updating
**Symptoms:** Item/ability previews show outdated information
**Solutions:**
1. Click "Refresh" button if available
2. Close and reopen the window
3. Re-scan for stats
4. Check console for preview calculation errors

## Cross-Project Issues

### Package Breaks When Moving Projects
**Symptoms:** Errors when importing package into new project
**Solutions:**
1. Use clean export: `Window → Fatal Odds → 📦 Export Package`
2. Import to fresh project first to test
3. Use project migration tools: `Window → Fatal Odds → 📥 Import Project Configuration`
4. Reset package completely: `Window → Fatal Odds → 🧹 Clean All Data`

### Generated Assets Missing
**Symptoms:** Items/abilities don't transfer between projects
**Solutions:**
1. Generated assets are project-specific and shouldn't be included in package
2. Use export/import configuration to recreate items
3. Create template items as part of package samples

### GUID Conflicts
**Symptoms:** Script references break when moving projects
**Solutions:**
1. Use package validation: `Window → Fatal Odds → 🔍 Validate Cross-Project Compatibility`
2. Reset package installation in new project
3. Avoid copying Generated folders between projects

## Build Issues

### Build Errors with Fatal Odds
**Symptoms:** Game builds fail or have runtime errors
**Solutions:**
1. Ensure Runtime assembly doesn't reference Editor code
2. Check all generated assets are properly referenced
3. Verify assembly definitions are correctly configured
4. Test in Development build first

### Missing References in Build
**Symptoms:** NullReference exceptions in built game
**Solutions:**
1. Ensure all required prefabs are in Resources folder or referenced in scenes
2. Check that StatRegistry.asset is included in build
3. Verify assembly definitions include all necessary references

## Common Error Messages

### "StatRegistry not found"
**Cause:** Missing or corrupted StatRegistry asset
**Solution:**
```
Window → Fatal Odds → 🔄 Reset Package Installation
```

### "Field not found for stat"
**Cause:** StatTag field was renamed or removed
**Solution:**
1. Re-scan for stats to update registry
2. Update items that reference the old field
3. Check field accessibility (public or SerializeField)

### "Modifier calculation failed"
**Cause:** Invalid modifier configuration
**Solution:**
1. Check modifier values are reasonable (not NaN or Infinity)
2. Verify stat GUID exists
3. Check for circular modifier dependencies

### "Assembly 'FatalOdds.Runtime' not found"
**Cause:** Assembly definition issues
**Solution:**
1. Validate package structure
2. Reimport package
3. Check Unity version compatibility

## Getting Help

### If You're Still Stuck

1. **Check In-Editor Help**: Most comprehensive and up-to-date
   ```
   Window → Fatal Odds → 📚 Help & Documentation
   ```

2. **Run Package Validation**: Automated diagnostics
   ```
   Window → Fatal Odds → 🔍 Validate Package
   ```

3. **Check Console**: Look for red error messages

4. **Create Minimal Reproduction**: Test with simple setup

5. **Report Issues**: Include Unity version, Fatal Odds version, and console output

### Useful Debug Information

When reporting issues, include:
- Unity version
- Fatal Odds version (`Window → Fatal Odds → ℹ️ About`)
- Console error messages
- Steps to reproduce
- Whether it happens in fresh project

### Reset Everything

If all else fails, complete reset:
```
Window → Fatal Odds → 🧹 Clean All Data (Fresh Start)
```

This clears all Fatal Odds data and gives you a completely fresh installation.

---

*For additional support: Ryan.IngenKal@mds.ac.nz*