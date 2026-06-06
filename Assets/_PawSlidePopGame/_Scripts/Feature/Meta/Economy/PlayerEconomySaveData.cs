using System;
using System.Collections.Generic;

namespace _PawSlidePopGame._Scripts.Gameplay.Meta.EconomyManager
{
    [Serializable]
    public sealed class PlayerEconomySaveData
    {
        public int schemaVersion = 1;
        public int coins;
        public Dictionary<string, int> boosterCounts = new Dictionary<string, int>();
        public int hearts = 5;
        public string lastHeartRegenTime = string.Empty;
        public bool isMatchActive = false;

        public void Sanitize()
        {
            schemaVersion = Math.Max(1, schemaVersion);
            coins = Math.Max(0, coins);
            boosterCounts ??= new Dictionary<string, int>();
            hearts = Math.Max(0, hearts);
            lastHeartRegenTime ??= string.Empty;

            List<string> keys = new List<string>(boosterCounts.Keys);
            for (int i = 0; i < keys.Count; i++)
            {
                string key = keys[i];
                if (string.IsNullOrWhiteSpace(key))
                {
                    boosterCounts.Remove(key);
                    continue;
                }

                boosterCounts[key] = Math.Max(0, boosterCounts[key]);
            }
        }
    }
}
