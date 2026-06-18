using UnityEngine;
using UnityEngine.InputSystem;
using _PawSlidePopGame._Scripts.Gameplay.Meta.EconomyManager;
using _PawSlidePopGame._Scripts.Feature.Match3.Boosters;
using _PawSlidePopGame._Scripts.Feature.Meta.Wheel;
using _PawSlidePopGame._Scripts.UI.Manager;
using _PawSlidePopGame._Scripts.UI.Screens.SubScreens;

namespace _PawSlidePopGame._Scripts.Utils.Dev
{
    [DisallowMultipleComponent]
    public sealed class DevTest : MonoBehaviour
    {
        [Header("Heart Options")]
        [SerializeField] private bool enableDevRefillHearts = true;
        [SerializeField] private Key devRefillHeartsKey = Key.Digit9;
        [SerializeField] private int refillHeartsAmount = 5;
        [SerializeField] private Key devInfiniteHeartsKey = Key.Digit7;
        [SerializeField] private Key devClearInfiniteHeartsKey = Key.Digit6;

        [Header("Coin Options")]
        [SerializeField] private bool enableDevAddCoin = true;
        [SerializeField] private Key devAddCoinKey = Key.Digit0;
        [SerializeField] private int devAddCoinAmount = 1000;

        [Header("Booster Options")]
        [SerializeField] private bool enableDevBoosterHotkeys = true;
        [SerializeField] private Key hammerKey = Key.Digit1;
        [SerializeField] private Key lineClearKey = Key.Digit2;
        [SerializeField] private Key rainbowKey = Key.Digit3;
        [SerializeField] private Key shuffleKey = Key.Digit4;

        [Header("Wheel Options")]
        [SerializeField] private bool enableDevWheelRestore = true;
        [SerializeField] private Key devWheelRestoreKey = Key.Digit8;

        [Header("Global Toggle")]
        [SerializeField] private bool enableDevMode = true;
        [SerializeField] private Key toggleDevModeKey = Key.F1;

#if UNITY_EDITOR || DEVELOPMENT_BUILD
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void InitializeDevTest()
        {
            if (FindFirstObjectByType<DevTest>() == null)
            {
                var go = new GameObject("[DevTest]");
                go.AddComponent<DevTest>();
                DontDestroyOnLoad(go);
            }
        }
#endif

        private void Update()
        {
#if !UNITY_EDITOR && !DEVELOPMENT_BUILD
            return;
#endif
            Keyboard keyboard = Keyboard.current;
            if (keyboard == null) return;

            // Toggle Dev Mode
            if (keyboard[toggleDevModeKey].wasPressedThisFrame)
            {
                enableDevMode = !enableDevMode;
                string stateMsg = enableDevMode ? "ENABLED" : "DISABLED";
                UIManager.Instance?.ShowToast($"Dev Hotkeys {stateMsg}");
                Debug.Log($"[DevTest] Dev Mode hotkeys {stateMsg}");
            }

            if (!enableDevMode) return;

            // 1. Refill Hearts
            if (enableDevRefillHearts && keyboard[devRefillHeartsKey].wasPressedThisFrame)
            {
                var heartManager = HeartManager.Instance;
                if (heartManager != null)
                {
                    heartManager.AddHearts(refillHeartsAmount, allowOverfill: false);
                    Debug.Log($"[DevTest] Refilled {refillHeartsAmount} hearts.");
                }
            }

            // 1b. Infinite Hearts (Add 30 Min)
            if (keyboard[devInfiniteHeartsKey].wasPressedThisFrame)
            {
                var heartManager = HeartManager.Instance;
                if (heartManager != null)
                {
                    heartManager.AddInfiniteHearts(1800); // 30 mins
                    Debug.Log("[DevTest] Activated 30 Min Infinite Hearts.");
                }
            }

            // 1c. Clear Infinite Hearts
            if (keyboard[devClearInfiniteHeartsKey].wasPressedThisFrame)
            {
                var heartManager = HeartManager.Instance;
                if (heartManager != null)
                {
                    heartManager.ClearInfiniteHearts();
                    Debug.Log("[DevTest] Cleared Infinite Hearts.");
                }
            }

            // 2. Add Coins
            if (enableDevAddCoin && keyboard[devAddCoinKey].wasPressedThisFrame)
            {
                var economyManager = EconomyManager.Instance;
                if (economyManager != null)
                {
                    economyManager.AddCoins(devAddCoinAmount, "dev_hotkey");
                    Debug.Log($"[DevTest] Added {devAddCoinAmount} coins.");
                }
            }

            // 3. Restore Wheel free spin
            if (enableDevWheelRestore && keyboard[devWheelRestoreKey].wasPressedThisFrame)
            {
                RestoreWheelFreeSpin();
            }

            // 4. Booster Hotkeys
            if (enableDevBoosterHotkeys)
            {
                if (keyboard[hammerKey].wasPressedThisFrame ||
                    keyboard[lineClearKey].wasPressedThisFrame ||
                    keyboard[rainbowKey].wasPressedThisFrame ||
                    keyboard[shuffleKey].wasPressedThisFrame ||
                    keyboard.escapeKey.wasPressedThisFrame)
                {
                    var boosterController = FindFirstObjectByType<BoosterController>(FindObjectsInactive.Include);
                    if (boosterController != null)
                    {
                        if (keyboard[hammerKey].wasPressedThisFrame)
                        {
                            boosterController.TrySelectBooster(BoosterType.Hammer);
                        }
                        else if (keyboard[lineClearKey].wasPressedThisFrame)
                        {
                            boosterController.TrySelectBooster(BoosterType.LineClear);
                        }
                        else if (keyboard[rainbowKey].wasPressedThisFrame)
                        {
                            boosterController.TrySelectBooster(BoosterType.RainbowPlacement);
                        }
                        else if (keyboard[shuffleKey].wasPressedThisFrame)
                        {
                            boosterController.TrySelectBooster(BoosterType.Shuffle);
                        }
                        else if (keyboard.escapeKey.wasPressedThisFrame)
                        {
                            boosterController.CancelSelection();
                        }
                    }
                }
            }
        }

        private static void RestoreWheelFreeSpin()
        {
            WheelStateRepository repository = new WheelStateRepository();
            repository.ResetFreeSpinCooldown();

            WheelSubScreen wheelScreen = FindFirstObjectByType<WheelSubScreen>(FindObjectsInactive.Include);
            if (wheelScreen != null)
            {
                wheelScreen.ResetFreeSpinCooldownForDev();
            }

            const string message = "Wheel free spin restored.";
            UIManager.Instance?.ShowToast(message);
            Debug.Log($"[DevTest] {message} Press Spin Wheel again.");
        }
    }
}
