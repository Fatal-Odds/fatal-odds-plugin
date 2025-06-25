# Changelog

All notable changes to Fatal Odds will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.0.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [0.1.0] - 2024-01-15

### Added
- Initial release of Fatal Odds modifier system
- StatTag attribute system for automatic stat discovery
- Visual item and ability creator with intuitive GUI
- Comprehensive modifier system supporting:
  - Flat value modifications
  - Percentage multipliers
  - Additive percentage stacking
  - Override values
  - Min/max constraints
  - Hyperbolic diminishing returns
  - Exponential scaling
- Stacking behavior options:
  - Linear stacking (Risk of Rain style)
  - Diminishing returns
  - Override (highest wins)
  - Average values
- Runtime modifier management system
- Real-time stat debugging and visualization
- Universal item pickup system with rarity-based effects
- Cross-project deployment and validation tools
- Comprehensive documentation and help system

### Features
- 🏷️ Automatic stat discovery through reflection
- 🎒 Stacking modifier items (items affect stats, not inventory)
- ⏰ Active abilities with cooldowns and temporary effects
- 🎨 Visual editor with tooltips and real-time validation
- 🔧 Runtime modifier manager with live debugging
- 📊 Statistics and documentation generation
- 🎮 Package management for cross-project use

### Technical
- Separate Runtime and Editor assemblies for optimal builds
- Unity Package Manager (UPM) compatible
- Clean state management for cross-project deployment
- Performance optimized with cached reflection calls
- Garbage collection friendly modifier system
- Network-ready serialization support

### Documentation
- Complete in-editor help system
- API reference documentation
- Step-by-step tutorials
- Best practices guide
- Troubleshooting documentation
- Migration guide for cross-project use

## [Unreleased]

### Planned for 0.2.0
- Visual node-based editor using GraphView
- Additional modifier calculation types
- Save/load preset systems
- Enhanced material system for item visuals
- Performance profiling tools

### Planned for 0.3.0
- Integration with popular roguelike frameworks
- Multiplayer networking support
- Visual effects integration
- Audio system integration

### Planned for 1.0.0
- Asset Store release
- Complete video tutorial series
- Sample game demonstrating all features
- Advanced documentation

## [0.0.1] - 2024-01-01

### Added
- Initial project setup
- Basic StatTag attribute
- Simple modifier system prototype