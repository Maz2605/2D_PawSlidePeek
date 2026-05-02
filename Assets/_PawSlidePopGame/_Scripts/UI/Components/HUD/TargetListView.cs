using System.Collections.Generic;
using _PawSlidePopGame._Scripts.Data.Events.Payloads;
using DG.Tweening;
using UnityEngine;

namespace _PawSlidePopGame._Scripts.UI.Components.HUD
{
    public class TargetListView : MonoBehaviour
    {
        [SerializeField] private Transform contentRoot;
        [SerializeField] private TargetItemView targetItemPrefab;

        private readonly List<TargetItemView> _spawnedItems = new List<TargetItemView>();

        private void Awake()
        {
            BootstrapTemplateFromScene();
        }

        private void OnValidate()
        {
            if (contentRoot == null)
            {
                contentRoot = transform;
            }

            BootstrapTemplateFromScene();
        }

        public void SetTargets(IReadOnlyList<TargetProgressData> targets)
        {
            int count = targets != null ? targets.Count : 0;
            EnsureItemCount(count);

            for (int i = 0; i < _spawnedItems.Count; i++)
            {
                TargetItemView item = _spawnedItems[i];
                if (item == null)
                {
                    continue;
                }

                if (i < count)
                {
                    item.SetData(targets[i]);
                }
                else
                {
                    item.SetData(null);
                }
            }
        }

        public void PlayProgressFx(TargetProgressChangedPayload payload)
        {
            for (int i = 0; i < _spawnedItems.Count; i++)
            {
                _spawnedItems[i]?.PlayProgressFx(payload);
            }
        }

        public void PlayCompletedFx()
        {
            transform.DOKill();
            transform.localScale = Vector3.one;
            transform.DOPunchScale(Vector3.one * 0.06f, 0.22f, 4)
                .SetLink(gameObject, LinkBehaviour.KillOnDisable);
        }

        private void BootstrapTemplateFromScene()
        {
            if (targetItemPrefab == null)
            {
                targetItemPrefab = contentRoot.GetComponentInChildren<TargetItemView>(true);
            }

            if (targetItemPrefab == null)
            {
                Debug.LogWarning("[TargetListView] Missing target item prefab/template.", this);
                return;
            }

            List<TargetItemView> existingItems = new List<TargetItemView>(contentRoot.GetComponentsInChildren<TargetItemView>(true));
            for (int i = 0; i < existingItems.Count; i++)
            {
                TargetItemView item = existingItems[i];
                if (item == null || item == targetItemPrefab)
                {
                    continue;
                }

                Destroy(item.gameObject);
            }

            targetItemPrefab.gameObject.SetActive(false);
        }

        private void EnsureItemCount(int count)
        {
            if (targetItemPrefab == null)
            {
                return;
            }

            while (_spawnedItems.Count < count)
            {
                TargetItemView instance = Instantiate(targetItemPrefab, contentRoot);
                instance.name = $"{targetItemPrefab.name}_{_spawnedItems.Count}";
                instance.gameObject.SetActive(true);
                _spawnedItems.Add(instance);
            }

            for (int i = 0; i < _spawnedItems.Count; i++)
            {
                if (_spawnedItems[i] != null)
                {
                    _spawnedItems[i].gameObject.SetActive(i < count);
                }
            }
        }
    }
}
