using System;
using System.Collections.Generic;
using _PawSlidePopGame._Scripts.Gameplay.Meta.MapManager;
using _PawSlidePopGame._Scripts.UI.Components;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace _PawSlidePopGame._Scripts.UI.Screens.SubScreens
{
    [DisallowMultipleComponent]
    public sealed class MapLevelNodeView : MonoBehaviour
    {
        [Header("Core")]
        [SerializeField] private Button button;
        [SerializeField] private TextMeshProUGUI levelText;
        [SerializeField] private Image nodeBackground;
        [SerializeField] private Image nodeFrame;

        [Header("State Roots")]
        [SerializeField] private GameObject lockedStateRoot;
        [SerializeField] private GameObject currentStateRoot;
        [SerializeField] private GameObject completedStateRoot;
        [SerializeField] private GameObject perfectStateRoot;
        [SerializeField] private GameObject hardStateRoot;

        [Header("Stars")]
        [SerializeField] private GameObject starsRoot;
        [SerializeField] private List<StarItemView> starViews = new List<StarItemView>();
        [SerializeField] private bool showLockedStarsForPlayedLevels;

        [Header("Node Colors")]
        [SerializeField] private Color lockedColor = new Color(0.55f, 0.58f, 0.62f, 1f);
        [SerializeField] private Color unplayedColor = new Color(0.43f, 0.88f, 0.91f, 1f);
        [SerializeField] private Color failedOrUnlockedColor = new Color(1f, 0.69f, 0.32f, 1f);
        [SerializeField] private Color currentColor = new Color(0.36f, 0.78f, 1f, 1f);
        [SerializeField] private Color completedColor = new Color(0.45f, 0.85f, 0.42f, 1f);
        [SerializeField] private Color perfectColor = new Color(0.24f, 0.78f, 0.96f, 1f);
        [SerializeField] private Color hardColor = new Color(0.92f, 0.38f, 0.48f, 1f);
        [SerializeField] private Color frameColor = Color.white;

        public Button Button => button;

        public void Setup(MapLevelEntry entry, Action<string> onPressed)
        {
            TryAutoBindReferences();

            if (levelText != null)
            {
                levelText.text = entry.DisplayLevelNumber.ToString();
            }

            SetState(entry);
            SetStars(entry.BestStars);

            if (button == null)
            {
                return;
            }

            button.onClick.RemoveAllListeners();
            button.interactable = entry.State != MapLevelState.Locked;
            string capturedLevelId = entry.LevelId;
            button.onClick.AddListener(() => onPressed?.Invoke(capturedLevelId));
        }

        private void SetState(MapLevelEntry entry)
        {
            MapLevelState state = entry.State;
            SetActive(lockedStateRoot, state == MapLevelState.Locked);
            SetActive(currentStateRoot, state == MapLevelState.Current);
            SetActive(completedStateRoot, state == MapLevelState.Completed);
            SetActive(perfectStateRoot, state == MapLevelState.Perfect);
            SetActive(hardStateRoot, state == MapLevelState.Hard || entry.IsHardLevel);

            if (nodeBackground != null)
            {
                nodeBackground.color = GetBackgroundColor(state);
            }

            if (nodeFrame != null)
            {
                nodeFrame.color = state == MapLevelState.Hard ? hardColor : frameColor;
            }
        }

        private void SetStars(int bestStars)
        {
            int clampedStars = Mathf.Max(0, bestStars);
            if (starsRoot != null)
            {
                starsRoot.SetActive(clampedStars > 0);
            }

            if (clampedStars <= 0)
            {
                for (int i = 0; i < starViews.Count; i++)
                {
                    if (starViews[i] != null)
                    {
                        starViews[i].gameObject.SetActive(false);
                    }
                }

                return;
            }

            StarVisualState reachedState = clampedStars >= 4
                ? StarVisualState.ReachedMax
                : StarVisualState.ReachedNormal;

            for (int i = 0; i < starViews.Count; i++)
            {
                StarItemView star = starViews[i];
                if (star == null)
                {
                    continue;
                }

                bool isReached = i < clampedStars;
                star.gameObject.SetActive(isReached || showLockedStarsForPlayedLevels);
                star.SetState(isReached ? reachedState : StarVisualState.Locked, true);
            }
        }

        private Color GetBackgroundColor(MapLevelState state)
        {
            switch (state)
            {
                case MapLevelState.Locked:
                    return lockedColor;
                case MapLevelState.FailedOrUnlocked:
                case MapLevelState.Available:
                    return failedOrUnlockedColor;
                case MapLevelState.Current:
                    return currentColor;
                case MapLevelState.Hard:
                    return hardColor;
                case MapLevelState.Completed:
                    return completedColor;
                case MapLevelState.Perfect:
                    return perfectColor;
                case MapLevelState.Unplayed:
                default:
                    return unplayedColor;
            }
        }

        private void OnValidate()
        {
            TryAutoBindReferences();
        }

        private void TryAutoBindReferences()
        {
            if (button == null)
            {
                button = GetComponent<Button>();
            }

            if (levelText == null)
            {
                levelText = GetComponentInChildren<TextMeshProUGUI>(true);
            }

            if (nodeBackground == null)
            {
                nodeBackground = GetComponent<Image>();
            }

            if (starViews.Count == 0)
            {
                starViews.AddRange(GetComponentsInChildren<StarItemView>(true));
            }
        }

        private static void SetActive(GameObject target, bool active)
        {
            if (target != null)
            {
                target.SetActive(active);
            }
        }
    }
}
