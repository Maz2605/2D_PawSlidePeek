using System;
using _PawSlidePopGame._Scripts.Core.System.DesignPattern.Singleton;
using UnityEngine;

namespace _PawSlidePopGame._Scripts.Gameplay.Meta.EconomyManager
{
    public class EconomyManager : Singleton<EconomyManager>
    {
        private PlayerEconomyRepository Repository => PlayerEconomyRepository.Instance;

        public int Coins => Repository.Data.coins;

        public event Action<int, int, string> OnCoinsChanged;

        public bool CanSpendCoins(int amount)
        {
            return amount >= 0 && Coins >= amount;
        }

        public void AddCoins(int amount, string reason = null)
        {
            if (amount <= 0)
            {
                return;
            }

            int previous = Coins;
            Repository.Data.coins = Mathf.Max(0, previous + amount);
            Repository.Save();
            OnCoinsChanged?.Invoke(previous, Repository.Data.coins, reason);
        }

        public bool TrySpendCoins(int amount, string reason = null)
        {
            if (amount < 0 || !CanSpendCoins(amount))
            {
                return false;
            }

            int previous = Coins;
            Repository.Data.coins = Mathf.Max(0, previous - amount);
            Repository.Save();
            OnCoinsChanged?.Invoke(previous, Repository.Data.coins, reason);
            return true;
        }

        public void Reload()
        {
            int previous = Coins;
            Repository.Reload();
            if (previous != Coins)
            {
                OnCoinsChanged?.Invoke(previous, Coins, "reload");
            }
        }
    }
}
