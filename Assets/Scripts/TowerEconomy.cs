using Game.DOTS;
using Unity.Entities;
using UnityEngine;

namespace Game
{
    /// <summary>Initializes and spends the player currency owned by the ECS world.</summary>
    public sealed class TowerEconomy : MonoBehaviour
    {
        [Min(0)] public int initialMoney = 50;
        [Min(0)] public int towerCost = 10;
        [Range(0f, 1f)] public float sellRefundRatio = 0.7f;

        private EntityManager entityManager;
        private EntityQuery currencyQuery;
        private bool isInitialized;

        public int CurrentMoney => isInitialized && !currencyQuery.IsEmptyIgnoreFilter
            ? currencyQuery.GetSingleton<PlayerCurrency>().Amount
            : 0;

        public int KillCount => isInitialized && !currencyQuery.IsEmptyIgnoreFilter
            ? currencyQuery.GetSingleton<PlayerCurrency>().KillCount
            : 0;

        public int TowerCost => InitializeCurrency() && !currencyQuery.IsEmptyIgnoreFilter
            ? currencyQuery.GetSingleton<PlayerCurrency>().TowerCost
            : towerCost;

        private void Awake()
        {
            InitializeCurrency();
        }

        public bool TrySpendTowerCost()
        {
            if (!InitializeCurrency() || currencyQuery.IsEmptyIgnoreFilter)
                return false;

            var currencyEntity = currencyQuery.GetSingletonEntity();
            var currency = entityManager.GetComponentData<PlayerCurrency>(currencyEntity);
            if (currency.Amount < currency.TowerCost)
                return false;

            currency.Amount -= currency.TowerCost;
            entityManager.SetComponentData(currencyEntity, currency);
            return true;
        }

        public int GetSellValue(TowerInstance tower)
        {
            return tower == null
                ? 0
                : Mathf.FloorToInt(tower.InvestedAmount * sellRefundRatio);
        }

        public bool TrySellTower(TowerInstance tower, out int sellValue)
        {
            sellValue = 0;
            if (tower == null || !InitializeCurrency() || currencyQuery.IsEmptyIgnoreFilter)
                return false;

            sellValue = GetSellValue(tower);
            var currencyEntity = currencyQuery.GetSingletonEntity();
            var currency = entityManager.GetComponentData<PlayerCurrency>(currencyEntity);
            currency.Amount += sellValue;
            entityManager.SetComponentData(currencyEntity, currency);
            return true;
        }

        private bool InitializeCurrency()
        {
            if (isInitialized)
                return true;

            var world = World.DefaultGameObjectInjectionWorld;
            if (world == null || !world.IsCreated)
                return false;

            entityManager = world.EntityManager;
            currencyQuery = entityManager.CreateEntityQuery(ComponentType.ReadOnly<PlayerCurrency>());
            if (currencyQuery.IsEmptyIgnoreFilter)
            {
                var currencyEntity = entityManager.CreateEntity();
                entityManager.AddComponentData(currencyEntity, new PlayerCurrency
                {
                    Amount = initialMoney,
                    TowerCost = towerCost
                });
            }

            isInitialized = true;
            return true;
        }
    }
}
