using Unity.Entities;

namespace Game.DOTS
{
    /// <summary>Shared player currency used for tower purchases and enemy kill rewards.</summary>
    public struct PlayerCurrency : IComponentData
    {
        public int Amount;
        public int TowerCost;
        public int KillCount;
    }
}
