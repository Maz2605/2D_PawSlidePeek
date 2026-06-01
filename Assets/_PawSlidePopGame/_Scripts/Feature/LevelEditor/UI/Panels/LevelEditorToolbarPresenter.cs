using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using _PawSlidePopGame._Scripts.Feature.LevelEditor.UI;

namespace _PawSlidePopGame._Scripts.Feature.LevelEditor.UI.Panels
{
    public sealed class LevelEditorToolbarPresenter : MonoBehaviour
    {
        [SerializeField] private Button newButton;
        [SerializeField] private Button saveButton;
        [SerializeField] private Button loadButton;
        [SerializeField] private TMP_InputField movesInput;
        [SerializeField] private TMP_InputField widthInput;
        [SerializeField] private TMP_InputField heightInput;
        [SerializeField] private LevelEditorLevelSelectorWidget levelSelectorWidget;
        [SerializeField] private LevelEditorValidationStatusWidget validationStatusWidget;
        [SerializeField] private LevelEditorTargetWidget targetWidget;

        private LevelEditorUIController _service;

        public void Bind(LevelEditorUIController service)
        {
            _service = service;

            BindButton(newButton, HandleNewClicked);
            BindButton(saveButton, HandleSaveClicked);
            BindButton(loadButton, HandleLoadClicked);
            levelSelectorWidget?.Bind();
            targetWidget?.Bind(service);
        }

        public void Refresh()
        {
            if (_service?.Session == null)
            {
                return;
            }

            if (movesInput != null)
            {
                movesInput.SetTextWithoutNotify(_service.Session.movesLimit.ToString());
            }

            if (widthInput != null)
            {
                widthInput.SetTextWithoutNotify(_service.Session.board.width.ToString());
            }

            if (heightInput != null)
            {
                heightInput.SetTextWithoutNotify(_service.Session.board.height.ToString());
            }

            levelSelectorWidget?.SetLevelId(_service.Session.levelId);
            levelSelectorWidget?.Refresh();
            validationStatusWidget?.Refresh(_service.Session.lastValidationResult);
            targetWidget?.Refresh();
        }

        private void HandleNewClicked()
        {
            string levelId = ReadLevelId();
            _service?.NewLevel(levelId, ResolveDisplayLevelNumber(levelId), ReadInt(widthInput, 8), ReadInt(heightInput, 8), ReadInt(movesInput, 26));
        }

        private void HandleSaveClicked()
        {
            string levelId = ReadLevelId();
            _service?.SetMetadata(levelId, ResolveDisplayLevelNumber(levelId), ReadInt(movesInput, 26));
            _service?.SaveLevel(true);
        }

        private void HandleLoadClicked()
        {
            _service?.LoadLevel(ReadLevelId());
        }

        private string ReadLevelId()
        {
            return levelSelectorWidget != null ? levelSelectorWidget.CurrentLevelId : "Level_001";
        }

        private int ResolveDisplayLevelNumber(string levelId)
        {
            if (string.IsNullOrWhiteSpace(levelId))
            {
                return _service?.Session != null ? Mathf.Max(1, _service.Session.displayLevelNumber) : 1;
            }

            int digitStart = levelId.Length;
            for (int i = levelId.Length - 1; i >= 0; i--)
            {
                if (!char.IsDigit(levelId[i]))
                {
                    break;
                }

                digitStart = i;
            }

            if (digitStart < levelId.Length && int.TryParse(levelId.Substring(digitStart), out int parsedLevel))
            {
                return Mathf.Max(1, parsedLevel);
            }

            return _service?.Session != null ? Mathf.Max(1, _service.Session.displayLevelNumber) : 1;
        }

        private static int ReadInt(TMP_InputField inputField, int fallback)
        {
            return inputField != null && int.TryParse(inputField.text, out int value) ? value : fallback;
        }

        private static void BindButton(Button button, Action callback)
        {
            if (button == null)
            {
                return;
            }

            button.onClick.RemoveAllListeners();
            button.onClick.AddListener(() => callback?.Invoke());
        }
    }
}
