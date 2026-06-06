using System;
using System.Collections.Generic;
using _PawSlidePopGame._Scripts.Feature.Match3.Boosters;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using UnityEngine;

namespace _PawSlidePopGame._Scripts.Feature.Meta.Wheel
{
    public static class WheelJsonConfigProvider
    {
        public static bool TryCreateSnapshot(
            TextAsset jsonAsset,
            IReadOnlyList<BoosterDefinitionSO> boosterCatalog,
            out WheelConfigSnapshot snapshot,
            out string error)
        {
            snapshot = null;
            error = null;

            if (jsonAsset == null || string.IsNullOrWhiteSpace(jsonAsset.text))
            {
                error = "Missing wheel JSON.";
                return false;
            }

            WheelConfigJson json;
            try
            {
                json = JsonConvert.DeserializeObject<WheelConfigJson>(
                    jsonAsset.text,
                    new JsonSerializerSettings
                    {
                        Converters = new List<JsonConverter> { new StringEnumConverter() }
                    });
            }
            catch (Exception exception)
            {
                error = exception.Message;
                return false;
            }

            if (json == null || json.rewards == null || json.rewards.Count == 0)
            {
                error = "Wheel JSON has no rewards.";
                return false;
            }

            Dictionary<string, BoosterDefinitionSO> boostersById = BuildBoosterLookup(boosterCatalog);
            List<WheelRewardEntryData> rewards = new List<WheelRewardEntryData>(json.rewards.Count);

            for (int i = 0; i < json.rewards.Count; i++)
            {
                WheelRewardEntryData reward = WheelRewardEntryData.FromJson(
                    json.rewards[i],
                    boosterId => boostersById.TryGetValue(boosterId, out BoosterDefinitionSO definition) ? definition : null,
                    Resources.Load<Sprite>);
                rewards.Add(reward);
            }

            snapshot = new WheelConfigSnapshot(
                rewards,
                json.spinDuration,
                json.minimumFullTurns,
                json.cooldownSeconds,
                json.segmentLandingPaddingDegrees);
            return true;
        }

        private static Dictionary<string, BoosterDefinitionSO> BuildBoosterLookup(IReadOnlyList<BoosterDefinitionSO> boosterCatalog)
        {
            Dictionary<string, BoosterDefinitionSO> lookup = new Dictionary<string, BoosterDefinitionSO>();
            if (boosterCatalog == null)
            {
                return lookup;
            }

            for (int i = 0; i < boosterCatalog.Count; i++)
            {
                BoosterDefinitionSO definition = boosterCatalog[i];
                if (definition == null || string.IsNullOrWhiteSpace(definition.BoosterId))
                {
                    continue;
                }

                lookup[definition.BoosterId] = definition;
            }

            return lookup;
        }
    }

    [Serializable]
    public sealed class WheelConfigJson
    {
        public List<WheelRewardEntryJson> rewards = new List<WheelRewardEntryJson>();
        public float spinDuration = 3.2f;
        public int minimumFullTurns = 5;
        public int cooldownSeconds = 86400;
        public float segmentLandingPaddingDegrees = 4f;
    }

    [Serializable]
    public sealed class WheelRewardEntryJson
    {
        public string id;
        public WheelRewardKind kind;
        public int amount = 1;
        public int weight = 1;
        public bool enabled = true;
        public string displayName;
        public string iconResourcesPath;
        public string boosterId;
    }
}
