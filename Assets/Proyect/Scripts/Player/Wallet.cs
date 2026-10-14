using System;
using UnityEngine;

namespace InventorySystem
{
    /// <summary>Saldo simple del jugador. La persistencia puede añadirse encima sin tocar tiendas.</summary>
    public class Wallet : MonoBehaviour
    {
        [SerializeField, Min(0)] private int balance;
        public int Balance => balance;
        public event Action<int> OnBalanceChanged;

        public void AddFunds(int amount)
        {
            if (amount <= 0) return;
            balance += amount;
            OnBalanceChanged?.Invoke(balance);
        }

        public bool TrySpend(int amount)
        {
            if (amount < 0 || amount > balance) return false;
            balance -= amount;
            OnBalanceChanged?.Invoke(balance);
            return true;
        }
    }
}
