using System.Collections.Generic;
using UnityEngine;

namespace Game
{
    /// <summary>Central list of tower definitions available to the placement system.</summary>
    [CreateAssetMenu(menuName = "Game/Towers/Tower Definition Catalog")]
    public sealed class TowerDefinitionCatalog : ScriptableObject
    {
        public List<TowerDefinition> towerDefinitions = new();

        /// <summary>Returns one valid definition without allocating a temporary candidate list.</summary>
        public TowerDefinition GetRandomDefinition()
        {
            var validCount = 0;
            foreach (var definition in towerDefinitions)
            {
                if (definition != null)
                    validCount++;
            }

            if (validCount == 0)
                return null;

            var selectedIndex = Random.Range(0, validCount);
            foreach (var definition in towerDefinitions)
            {
                if (definition == null)
                    continue;

                if (selectedIndex-- == 0)
                    return definition;
            }

            return null;
        }
    }
}
