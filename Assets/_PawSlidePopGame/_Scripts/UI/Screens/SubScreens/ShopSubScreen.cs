using System.Collections.Generic;
using _PawSlidePopGame._Scripts.Feature.Meta.Shop;
using _PawSlidePopGame._Scripts.Gameplay.Meta.EconomyManager;
using _PawSlidePopGame._Scripts.UI.Base;
using _PawSlidePopGame._Scripts.UI.Manager;
using UnityEngine;

namespace _PawSlidePopGame._Scripts.UI.Screens.SubScreens
{
    /// <summary>
    /// Màn hình con Shop – hiển thị danh sách vật phẩm và xử lý tương tác mua hàng.
    /// Hỗ trợ 2 cách bố trí UI:
    ///   1. staticItemViews: Kéo thả sẵn các ShopItemView trên scene (phù hợp layout cố định).
    ///   2. itemPrefab + contentRoot: Sinh động các ShopItemView từ prefab (phù hợp danh sách dài).
    /// </summary>
    public class ShopSubScreen : BaseSubScreen
    {
        [Header("Dữ Liệu")]
        [SerializeField] private ShopCatalogSO catalog;

        [Header("UI Tĩnh (Ưu tiên sử dụng)")]
        [Tooltip("Kéo thả trực tiếp các ShopItemView đã có sẵn trên scene vào đây (theo thứ tự).")]
        [SerializeField] private List<ShopItemView> staticItemViews = new List<ShopItemView>();

        [Header("UI Động (Dùng khi không đủ ô tĩnh)")]
        [Tooltip("Prefab ShopItemView để sinh tự động nếu không dùng staticItemViews.")]
        [SerializeField] private ShopItemView itemPrefab;
        [Tooltip("Transform cha chứa các ô sinh động.")]
        [SerializeField] private Transform contentRoot;

        private readonly List<ShopItemView> _activeViews = new List<ShopItemView>();

        // ───── Lifecycle ─────

        public override void Init()
        {
            if (isInitialized)
                return;

            base.Init();
            BindViews();
            SubscribeCoinChanges();
        }

        public override void Show()
        {
            base.Show();
            // Làm mới trạng thái nút mỗi lần mở Shop (Coin có thể đã thay đổi)
            RefreshAllAffordability();
        }

        public override void Hide()
        {
            base.Hide();
        }

        private void OnDestroy()
        {
            UnsubscribeCoinChanges();
            ShopService.OnItemPurchased  -= HandlePurchaseSuccess;
            ShopService.OnPurchaseFailed -= HandlePurchaseFailed;
        }

        // ───── Setup ─────

        private void BindViews()
        {
            if (catalog == null)
            {
                catalog = Resources.Load<ShopCatalogSO>("Shop/DefaultShopCatalog");
            }

            if (catalog == null)
            {
                Debug.LogWarning("[ShopSubScreen] Không có ShopCatalogSO nào được gán hoặc tìm thấy trong Resources/Shop/DefaultShopCatalog.", this);
                return;
            }

            bool useStatic = staticItemViews != null && staticItemViews.Count > 0;

            for (int i = 0; i < catalog.Items.Count; i++)
            {
                ShopItemSO item = catalog.Items[i];
                if (item == null)
                    continue;

                ShopItemView view = useStatic
                    ? GetStaticView(i)
                    : SpawnDynamicView();

                if (view == null)
                    continue;

                bool canAfford = CanPlayerAfford(item);
                view.Bind(item, HandleItemPurchaseClicked, canAfford);
                _activeViews.Add(view);
            }

            // Ẩn các ô tĩnh thừa
            if (useStatic)
            {
                for (int i = catalog.Items.Count; i < staticItemViews.Count; i++)
                {
                    if (staticItemViews[i] != null)
                        staticItemViews[i].gameObject.SetActive(false);
                }
            }

            // Đăng ký lắng nghe kết quả mua hàng
            ShopService.OnItemPurchased  += HandlePurchaseSuccess;
            ShopService.OnPurchaseFailed += HandlePurchaseFailed;
        }

        private ShopItemView GetStaticView(int index)
        {
            if (index < 0 || index >= staticItemViews.Count)
            {
                Debug.LogWarning($"[ShopSubScreen] Không có đủ staticItemViews cho index {index}. Hãy thêm ô vào scene hoặc dùng itemPrefab.", this);
                return null;
            }

            return staticItemViews[index];
        }

        private ShopItemView SpawnDynamicView()
        {
            if (itemPrefab == null || contentRoot == null)
            {
                Debug.LogWarning("[ShopSubScreen] itemPrefab hoặc contentRoot chưa được gán.", this);
                return null;
            }

            return Instantiate(itemPrefab, contentRoot);
        }

        // ───── Affordability ─────

        private void RefreshAllAffordability()
        {
            if (catalog == null)
                return;

            for (int i = 0; i < _activeViews.Count && i < catalog.Items.Count; i++)
            {
                ShopItemSO item = catalog.Items[i];
                if (item == null)
                    continue;

                _activeViews[i]?.RefreshAffordability(CanPlayerAfford(item));
            }
        }

        private static bool CanPlayerAfford(ShopItemSO item)
        {
            return EconomyManager.Instance != null && EconomyManager.Instance.CanSpendCoins(item.PriceCoins);
        }

        // ───── Event Subscriptions ─────

        private void SubscribeCoinChanges()
        {
            if (EconomyManager.Instance != null)
                EconomyManager.Instance.OnCoinsChanged += HandleCoinsChanged;
        }

        private void UnsubscribeCoinChanges()
        {
            if (EconomyManager.Instance != null)
                EconomyManager.Instance.OnCoinsChanged -= HandleCoinsChanged;
        }

        private void HandleCoinsChanged(int previous, int current, string reason)
        {
            RefreshAllAffordability();
        }

        // ───── Purchase Handlers ─────

        private void HandleItemPurchaseClicked(ShopItemSO item)
        {
            ShopService.TryPurchaseItem(item);
        }

        private void HandlePurchaseSuccess(ShopItemSO item)
        {
            UIManager.Instance?.ShowToast($"Mua thành công: {item.DisplayName}!");
            RefreshAllAffordability();
        }

        private void HandlePurchaseFailed(ShopItemSO item, PurchaseFailReason reason)
        {
            string message = reason switch
            {
                PurchaseFailReason.InsufficientCoins    => "Không đủ Coin!",
                PurchaseFailReason.InvalidRewardConfig  => "Cấu hình phần thưởng bị lỗi. Liên hệ GD!",
                PurchaseFailReason.SystemError          => "Lỗi hệ thống. Vui lòng thử lại.",
                _                                       => "Không thể mua vật phẩm này."
            };

            UIManager.Instance?.ShowToast(message);
        }
    }
}