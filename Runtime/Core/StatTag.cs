using System;
using UnityEngine;

namespace FatalOdds.Runtime
{
    
    /// Attribute to mark fields that should be registered as modifiable stats
    
    [AttributeUsage(AttributeTargets.Field, AllowMultiple = false)]
    public class StatTagAttribute : PropertyAttribute
    {
        public string StatName { get; private set; }
        public string Category { get; private set; }
        public string Description { get; private set; }
        public bool ShowInUI { get; private set; }

        
        /// Mark a field as a modifiable stat
        
        /// <param name="statName">Display name for the stat. If null, uses field name</param>
        /// <param name="category">Category for organization (e.g., "Combat", "Movement")</param>
        public StatTagAttribute(string statName = null, string category = "General")
        {
            StatName = statName;
            Category = category;
            ShowInUI = true;
        }

        
        /// Mark a field as a modifiable stat with full options
        
        public StatTagAttribute(string statName, string category, string description, bool showInUI = true)
        {
            StatName = statName;
            Category = category;
            Description = description;
            ShowInUI = showInUI;
        }
    }
}