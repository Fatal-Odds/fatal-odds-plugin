namespace FatalOdds.Runtime
{
    
    /// Types of mathematical operations that can be applied to stats
    
    public enum ModifierType
    {
        // Basic operations
        Flat,           // Add/subtract a flat value
        Percentage,     // Multiply by a percentage (1.0 = 100%)

        // Advanced operations
        PercentageAdditive,  // Add percentages together before applying
        PercentageMultiplicative, // Multiply percentages together

        // Special operations
        Override,       // Set to a specific value (ignores other modifiers)
        Minimum,        // Set a minimum value
        Maximum,        // Set a maximum value

        // Complex operations
        Hyperbolic,     // Diminishing returns formula: value / (value + constant)
        Exponential,    // Exponential scaling: base ^ value
        Logarithmic,    // Logarithmic scaling: log(value)

        // Conditional operations
        ConditionalFlat,       // Apply flat value only if condition is met
        ConditionalPercentage, // Apply percentage only if condition is met
    }

    
    /// Determines how multiple modifiers of the same type stack
    
    public enum StackingBehavior
    {
        Additive,       // Add all values together
        Multiplicative, // Multiply all values together
        Override,       // Use only the highest/latest value
        Average,        // Average all values
        Highest,        // Use only the highest value
        Lowest,         // Use only the lowest value
    }

    
    /// Calculation priority (lower numbers calculate first)
    
    public enum CalculationPriority
    {
        Override = 0,        // Overrides calculate first
        Minimum = 100,       // Min/max constraints
        Maximum = 100,
        FlatAdditive = 200,  // Flat additions
        FlatSubtractive = 300, // Flat subtractions
        PercentageAdditive = 400,  // Additive percentages
        PercentageMultiplicative = 500, // Multiplicative percentages
        Hyperbolic = 600,    // Diminishing returns
        Exponential = 700,   // Exponential scaling
        Logarithmic = 800,   // Logarithmic scaling
        Conditional = 900,   // Conditional modifiers last
    }
}