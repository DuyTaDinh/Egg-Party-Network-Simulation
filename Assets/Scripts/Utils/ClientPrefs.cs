using UnityEngine;

namespace Utils
{
    public static class ClientPrefs
    {
        public static string READED_TUTORIAL_KEY = "READ_TUTORIAL_KEY";
        
        enum PrefType
        {
            STRING,
            INT,
            FLOAT,
            BOOL
        }
        
        private static bool HasKey(string key)
        {
            return PlayerPrefs.HasKey(key);
        }

        private static void SaveKey(string key, string value, PrefType type)
        {
            // encryption can be added here if needed
        }
        
        public static void SetBool(string key, bool value)
        {
            PlayerPrefs.SetInt(key, value ? 1 : 0);
            
        }
        
        public static bool GetBool(string key, bool defaultValue = false)
        {
            if (HasKey(key))
            {
                int defaultInt = defaultValue ? 1 : 0;
                int intValue = PlayerPrefs.GetInt(key, defaultInt);
                return intValue != 0;
            }
            return defaultValue;
        }
        
    }
}