using System;
using _PawSlidePopGame._Scripts.Core.System.DesignPattern.Singleton;
using _PawSlidePopGame._Scripts.Feature.Match3.Boosters;
using _PawSlidePopGame._Scripts.Gameplay.Meta.EconomyManager;

namespace _PawSlidePopGame._Scripts.Gameplay.Meta.Inventory
{
    public class BoosterInventory : Singleton<BoosterInventory>
    {
        private PlayerEconomyRepository Repository => PlayerEconomyRepository.Instance;

        public event Action<string, int, int, string> OnBoosterCountChanged;

        public int GetCount(string boosterId)
        {
            if (string.IsNullOrWhiteSpace(boosterId))
            {
                return 0;
            }

            return Repository.Data.boosterCounts.TryGetValue(boosterId, out int count)
                ? Math.Max(0, count)
                : 0;
        }

        public int GetCount(BoosterDefinitionSO definition)
        {
            return definition == null ? 0 : GetCount(definition.BoosterId);
        }

        public bool HasBooster(BoosterDefinitionSO definition)
        {
            return definition != null &&
                   (definition.IsUnlimitedForDev || GetCount(definition.BoosterId) > 0);
        }

        private readonly System.Collections.Generic.Dictionary<string, BoosterDefinitionSO> _definitions = new System.Collections.Generic.Dictionary<string, BoosterDefinitionSO>();

        private void EnsureDatabaseLoaded()
        {
            if (_definitions.Count > 0)
            {
                return;
            }

            var db = UnityEngine.Resources.Load<_PawSlidePopGame._Scripts.Feature.Meta.Reward.BoosterDatabaseSO>("Configs/BoosterDatabase");
            if (db != null && db.Boosters != null)
            {
                RegisterDefinitions(db.Boosters);
            }
        }

        public BoosterDefinitionSO GetDefinition(string boosterId)
        {
            if (string.IsNullOrWhiteSpace(boosterId))
            {
                return null;
            }

            EnsureDatabaseLoaded();
            _definitions.TryGetValue(boosterId, out var def);
            return def;
        }

        public void RegisterDefinitions(System.Collections.Generic.IEnumerable<BoosterDefinitionSO> definitions)
        {
            if (definitions != null)
            {
                foreach (var def in definitions)
                {
                    if (def != null && !string.IsNullOrWhiteSpace(def.BoosterId))
                    {
                        _definitions[def.BoosterId] = def;
                    }
                }
            }
            Repository.RegisterBoosterDefinitions(definitions);
        }

        public void AddBooster(string boosterId, int amount, string reason = null)
        {
            if (string.IsNullOrWhiteSpace(boosterId) || amount <= 0)
            {
                return;
            }

            int previous = GetCount(boosterId);
            int current = Math.Max(0, previous + amount);
            Repository.Data.boosterCounts[boosterId] = current;
            Repository.Save();
            OnBoosterCountChanged?.Invoke(boosterId, previous, current, reason);
        }

        public void AddBooster(BoosterDefinitionSO definition, int amount, string reason = null)
        {
            if (definition == null)
            {
                return;
            }

            AddBooster(definition.BoosterId, amount, reason);
        }

        public bool TryConsumeBooster(BoosterDefinitionSO definition, string reason = null)
        {
            if (definition == null)
            {
                return false;
            }

            if (definition.IsUnlimitedForDev)
            {
                return true;
            }

            string boosterId = definition.BoosterId;
            int previous = GetCount(boosterId);
            if (previous <= 0)
            {
                return false;
            }

            int current = previous - 1;
            Repository.Data.boosterCounts[boosterId] = current;
            Repository.Save();
            OnBoosterCountChanged?.Invoke(boosterId, previous, current, reason);
            return true;
        }

        public void Reload()
        {
            Repository.Reload();
        }
    }
}
