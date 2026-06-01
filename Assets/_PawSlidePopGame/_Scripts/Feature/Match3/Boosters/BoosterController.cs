using System;
using System.Collections.Generic;
using _PawSlidePopGame._Scripts.Feature.Match3.Flow;
using _PawSlidePopGame._Scripts.Feature.Match3.Logic.Move;
using _PawSlidePopGame._Scripts.Feature.Match3.Model.Board;
using _PawSlidePopGame._Scripts.Feature.Match3.Presentation;
using _PawSlidePopGame._Scripts.Gameplay.Meta.EconomyManager;
using _PawSlidePopGame._Scripts.Gameplay.Meta.Inventory;
using UnityEngine;

namespace _PawSlidePopGame._Scripts.Feature.Match3.Boosters
{
    [DisallowMultipleComponent]
    public sealed class BoosterController : MonoBehaviour
    {
        [SerializeField] private List<BoosterDefinitionSO> boosterDefinitions = new List<BoosterDefinitionSO>();
        [SerializeField] private bool enableDevHotkeys = true;
        [SerializeField] private UnityEngine.InputSystem.Key hammerKey = UnityEngine.InputSystem.Key.Digit1;
        [SerializeField] private UnityEngine.InputSystem.Key lineClearKey = UnityEngine.InputSystem.Key.Digit2;
        [SerializeField] private UnityEngine.InputSystem.Key rainbowKey = UnityEngine.InputSystem.Key.Digit3;
        [SerializeField] private UnityEngine.InputSystem.Key shuffleKey = UnityEngine.InputSystem.Key.Digit4;
        [SerializeField] private bool logDebugMessages = true;

        private BoosterDefinitionSO _activeBooster;
        private BoosterPaymentSource _activePaymentSource;

        public BoosterDefinitionSO ActiveBooster => _activeBooster;
        public bool HasActiveTargetingBooster => _activeBooster != null && _activeBooster.TargetingMode != BoosterTargetingMode.Immediate;

        public event Action<Func<BoardMoveExecutionResult>> OnExecutionRequested;
        public event Action<BoosterDefinitionSO> OnActiveBoosterChanged;
        public event Action<BoosterDefinitionSO> OnBoosterUseRejected;

        private void Awake()
        {
            BoosterInventory.Instance.RegisterDefinitions(boosterDefinitions);
        }

        private void Update()
        {
            UnityEngine.InputSystem.Keyboard keyboard = UnityEngine.InputSystem.Keyboard.current;
            if (!enableDevHotkeys || keyboard == null)
            {
                return;
            }

            if (WasPressedThisFrame(keyboard, hammerKey))
            {
                TrySelectBooster(BoosterType.Hammer);
            }
            else if (WasPressedThisFrame(keyboard, lineClearKey))
            {
                TrySelectBooster(BoosterType.LineClear);
            }
            else if (WasPressedThisFrame(keyboard, rainbowKey))
            {
                TrySelectBooster(BoosterType.RainbowPlacement);
            }
            else if (WasPressedThisFrame(keyboard, shuffleKey))
            {
                TrySelectBooster(BoosterType.Shuffle);
            }
            else if (keyboard.escapeKey.wasPressedThisFrame)
            {
                CancelSelection();
            }
        }

        public bool SelectBooster(BoosterType boosterType)
        {
            return TrySelectBooster(boosterType);
        }

        public bool TrySelectBooster(BoosterType boosterType)
        {
            BoosterDefinitionSO definition = GetDefinition(boosterType);
            if (definition == null)
            {
                Debug.LogWarning($"[BoosterController] Missing booster definition for {boosterType}.", this);
                return false;
            }

            if (!CanSelectBooster(definition))
            {
                LogDebug($"Rejected {definition.BoosterType}. Count={BoosterInventory.Instance.GetCount(definition)}, Coins={EconomyManager.Instance.Coins}, Price={definition.CoinPrice}.");
                OnBoosterUseRejected?.Invoke(definition);
                return false;
            }

            if (definition.TargetingMode == BoosterTargetingMode.Immediate)
            {
                CancelSelection();
                RequestImmediateExecution(definition);
                return true;
            }

            _activeBooster = definition;
            _activePaymentSource = ResolvePaymentSource(definition);
            LogDebug($"Selected {definition.BoosterType}. PaymentSource={_activePaymentSource}.");
            OnActiveBoosterChanged?.Invoke(_activeBooster);
            return true;
        }

        public bool CanSelectBooster(BoosterDefinitionSO definition)
        {
            if (definition == null)
            {
                return false;
            }

            if (definition.IsUnlimitedForDev)
            {
                return true;
            }

            if (BoosterInventory.Instance.GetCount(definition) > 0)
            {
                return true;
            }

            return EconomyManager.Instance.CanSpendCoins(definition.CoinPrice);
        }

        public void CancelSelection()
        {
            if (_activeBooster == null)
            {
                return;
            }

            _activeBooster = null;
            _activePaymentSource = BoosterPaymentSource.None;
            OnActiveBoosterChanged?.Invoke(null);
        }

        public bool TryHandleTileTap(CellModel cell)
        {
            if (_activeBooster == null || _activeBooster.TargetingMode != BoosterTargetingMode.TapCell)
            {
                return false;
            }

            if (cell == null || GameFlowManager.Instance == null || !GameFlowManager.Instance.CanUseBoosterAt(_activeBooster, cell.X, cell.Y))
            {
                LogDebug($"Tap booster rejected. Booster={_activeBooster.BoosterType}, Cell={cell?.X},{cell?.Y}, State={GameFlowManager.Instance?.CurrentInGameSubState}.");
                return true;
            }

            BoosterDefinitionSO selectedBooster = _activeBooster;
            BoosterPaymentSource paymentSource = _activePaymentSource;
            int x = cell.X;
            int y = cell.Y;
            CancelSelection();
            OnExecutionRequested?.Invoke(() => GameFlowManager.Instance != null
                ? ExecuteAndConsume(selectedBooster, paymentSource, () => GameFlowManager.Instance.RequestBoosterAt(selectedBooster, x, y))
                : new BoardMoveExecutionResult());
            return true;
        }

        public bool TryHandleLineSwipe(BoardMoveRequest request)
        {
            if (_activeBooster == null || _activeBooster.TargetingMode != BoosterTargetingMode.SwipeLine)
            {
                return false;
            }

            if (GameFlowManager.Instance == null || !GameFlowManager.Instance.CanUseBoosterLine(_activeBooster, request.Axis, request.LineIndex))
            {
                LogDebug($"Line booster rejected. Booster={_activeBooster.BoosterType}, Axis={request.Axis}, Line={request.LineIndex}, State={GameFlowManager.Instance?.CurrentInGameSubState}, CanCommands={GameFlowManager.Instance?.CanAcceptGameplayCommands}.");
                return true;
            }

            BoosterDefinitionSO selectedBooster = _activeBooster;
            BoosterPaymentSource paymentSource = _activePaymentSource;
            CancelSelection();
            OnExecutionRequested?.Invoke(() => GameFlowManager.Instance != null
                ? ExecuteAndConsume(selectedBooster, paymentSource, () => GameFlowManager.Instance.RequestBoosterLine(selectedBooster, request.Axis, request.LineIndex))
                : new BoardMoveExecutionResult());
            return true;
        }

        private void RequestImmediateExecution(BoosterDefinitionSO definition)
        {
            if (definition == null || GameFlowManager.Instance == null || !CanSelectBooster(definition) || !GameFlowManager.Instance.CanUseImmediateBooster(definition))
            {
                LogDebug($"Immediate booster rejected. Booster={definition?.BoosterType}, State={GameFlowManager.Instance?.CurrentInGameSubState}, CanCommands={GameFlowManager.Instance?.CanAcceptGameplayCommands}.");
                OnBoosterUseRejected?.Invoke(definition);
                return;
            }

            OnExecutionRequested?.Invoke(() => GameFlowManager.Instance != null
                ? ExecuteAndConsume(definition, ResolvePaymentSource(definition), () => GameFlowManager.Instance.RequestImmediateBooster(definition))
                : new BoardMoveExecutionResult());
        }

        private static BoardMoveExecutionResult ExecuteAndConsume(BoosterDefinitionSO definition, BoosterPaymentSource paymentSource, Func<BoardMoveExecutionResult> execute)
        {
            BoardMoveExecutionResult result = execute != null ? execute.Invoke() : new BoardMoveExecutionResult();
            if (result.IsAccepted)
            {
                if (paymentSource == BoosterPaymentSource.Inventory)
                {
                    BoosterInventory.Instance.TryConsumeBooster(definition, "booster_used");
                }
                else if (paymentSource == BoosterPaymentSource.Coins)
                {
                    EconomyManager.Instance.TrySpendCoins(definition.CoinPrice, "booster_used");
                }

                UnityEngine.Debug.Log($"[BoosterController] Used {definition.BoosterType}. PaymentSource={paymentSource}, Coins={EconomyManager.Instance.Coins}, Count={BoosterInventory.Instance.GetCount(definition)}.");
            }

            return result;
        }

        private BoosterDefinitionSO GetDefinition(BoosterType boosterType)
        {
            for (int i = 0; i < boosterDefinitions.Count; i++)
            {
                BoosterDefinitionSO definition = boosterDefinitions[i];
                if (definition != null && definition.BoosterType == boosterType)
                {
                    return definition;
                }
            }

            return null;
        }

        private static bool WasPressedThisFrame(UnityEngine.InputSystem.Keyboard keyboard, UnityEngine.InputSystem.Key key)
        {
            UnityEngine.InputSystem.Controls.KeyControl control = keyboard[key];
            return control != null && control.wasPressedThisFrame;
        }

        private static BoosterPaymentSource ResolvePaymentSource(BoosterDefinitionSO definition)
        {
            if (definition == null)
            {
                return BoosterPaymentSource.None;
            }

            if (definition.IsUnlimitedForDev)
            {
                return BoosterPaymentSource.Free;
            }

            return BoosterInventory.Instance.GetCount(definition) > 0
                ? BoosterPaymentSource.Inventory
                : BoosterPaymentSource.Coins;
        }

        private void LogDebug(string message)
        {
            if (logDebugMessages)
            {
                Debug.Log($"[BoosterController] {message}", this);
            }
        }

        private enum BoosterPaymentSource
        {
            None = 0,
            Free = 1,
            Inventory = 2,
            Coins = 3
        }
    }
}
