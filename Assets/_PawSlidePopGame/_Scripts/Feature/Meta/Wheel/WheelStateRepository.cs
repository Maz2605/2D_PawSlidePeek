using System;
using _PawSlidePopGame._Scripts.Data.SaveSystem;

namespace _PawSlidePopGame._Scripts.Feature.Meta.Wheel
{
    public sealed class WheelStateRepository
    {
        public const string DefaultSaveKey = "wheel_state";

        private readonly string _saveKey;
        private WheelStateSaveData _data;

        public WheelStateRepository(string saveKey = DefaultSaveKey)
        {
            _saveKey = string.IsNullOrWhiteSpace(saveKey) ? DefaultSaveKey : saveKey;
        }

        public DateTime? UtcNowOverride { get; set; }
        public WheelStateSaveData Data => _data ??= Load();

        public bool CanFreeSpin(int cooldownSeconds)
        {
            return GetRemainingCooldownSeconds(cooldownSeconds) <= 0;
        }

        public double GetRemainingCooldownSeconds(int cooldownSeconds)
        {
            cooldownSeconds = Math.Max(0, cooldownSeconds);
            if (cooldownSeconds == 0)
            {
                return 0;
            }

            DateTime lastSpin = GetLastSpinTime();
            if (lastSpin == DateTime.MinValue)
            {
                return 0;
            }

            double elapsed = (CurrentUtcNow - lastSpin).TotalSeconds;
            return Math.Max(0, cooldownSeconds - elapsed);
        }

        public void MarkFreeSpinUsed()
        {
            Data.lastFreeSpinUtc = CurrentUtcNow.ToString("o");
            Save();
        }

        public void ResetFreeSpinCooldown()
        {
            Data.lastFreeSpinUtc = string.Empty;
            Save();
        }

        public void Reload()
        {
            _data = Load();
        }

        public void DeleteSave()
        {
            _data = new WheelStateSaveData();
            SaveSystem.DeleteFile(_saveKey);
        }

        private DateTime GetLastSpinTime()
        {
            string raw = Data.lastFreeSpinUtc;
            if (string.IsNullOrWhiteSpace(raw) || !DateTime.TryParse(raw, out DateTime parsed))
            {
                return DateTime.MinValue;
            }

            return parsed.ToUniversalTime();
        }

        private DateTime CurrentUtcNow => UtcNowOverride ?? DateTime.UtcNow;

        private WheelStateSaveData Load()
        {
            WheelStateSaveData loaded = SaveSystem.Load<WheelStateSaveData>(_saveKey) ?? new WheelStateSaveData();
            loaded.Sanitize();
            return loaded;
        }

        private void Save()
        {
            Data.Sanitize();
            SaveSystem.Save(_saveKey, Data);
        }
    }

    [Serializable]
    public sealed class WheelStateSaveData
    {
        public int schemaVersion = 1;
        public string lastFreeSpinUtc = string.Empty;

        public void Sanitize()
        {
            schemaVersion = Math.Max(1, schemaVersion);
            lastFreeSpinUtc ??= string.Empty;
        }
    }
}
