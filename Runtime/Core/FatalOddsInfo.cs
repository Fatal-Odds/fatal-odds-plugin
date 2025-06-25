using UnityEngine;

namespace FatalOdds.Runtime
{
    
    /// Basic info class to verify the runtime assembly is working
    
    public static class FatalOddsInfo
    {
        public const string PLUGIN_NAME = "Fatal Odds";
        public const string VERSION = "1.1.0";

        public static void LogInfo()
        {
            Debug.Log($"[{PLUGIN_NAME}] Version {VERSION} - Runtime assembly loaded successfully!");
        }
    }
}