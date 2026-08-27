using UnityEngine;

namespace Game
{
    /// <summary>Runtime state for one buildable map tile.</summary>
    public sealed class TowerTile : MonoBehaviour
    {
        public Vector2Int Coordinate { get; private set; }
        public TowerInstance Occupant { get; private set; }
        public bool IsOccupied => Occupant != null;

        public void Initialize(Vector2Int coordinate)
        {
            Coordinate = coordinate;
        }

        public void SetOccupant(TowerInstance tower)
        {
            Occupant = tower;
        }

        public void ClearOccupant(TowerInstance tower)
        {
            if (Occupant == tower)
                Occupant = null;
        }
    }
}
