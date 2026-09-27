using System.Collections.Generic;
using Game;
using UnityEngine;

namespace Game.Editor
{
    internal enum TowerCatalogValidationSeverity
    {
        Warning,
        Error
    }

    internal readonly struct TowerCatalogValidationIssue
    {
        public readonly TowerCatalogValidationSeverity Severity;
        public readonly string Message;

        public TowerCatalogValidationIssue(TowerCatalogValidationSeverity severity, string message)
        {
            Severity = severity;
            Message = message;
        }
    }

    /// <summary>Editor-only validation for tower catalog entries and their combat data.</summary>
    internal static class TowerCatalogValidator
    {
        public static void Validate(TowerDefinitionCatalog catalog, List<TowerCatalogValidationIssue> issues)
        {
            issues.Clear();
            if (catalog == null)
            {
                issues.Add(new TowerCatalogValidationIssue(TowerCatalogValidationSeverity.Warning,
                    "Assign a Tower Definition Catalog to validate its spawn candidates."));
                return;
            }

            var seenDefinitions = new HashSet<TowerDefinition>();
            for (var index = 0; index < catalog.towerDefinitions.Count; index++)
            {
                var definition = catalog.towerDefinitions[index];
                var entryName = $"Entry {index + 1}";
                if (definition == null)
                {
                    issues.Add(new TowerCatalogValidationIssue(TowerCatalogValidationSeverity.Error,
                        $"{entryName}: missing Tower Definition."));
                    continue;
                }

                if (!seenDefinitions.Add(definition))
                {
                    issues.Add(new TowerCatalogValidationIssue(TowerCatalogValidationSeverity.Warning,
                        $"{entryName}: '{definition.name}' is registered more than once."));
                }

                ValidateDefinition(definition, issues);
            }

            if (catalog.towerDefinitions.Count == 0)
            {
                issues.Add(new TowerCatalogValidationIssue(TowerCatalogValidationSeverity.Error,
                    "Catalog has no tower definitions."));
            }
        }

        private static void ValidateDefinition(TowerDefinition definition, List<TowerCatalogValidationIssue> issues)
        {
            var label = string.IsNullOrWhiteSpace(definition.displayName) ? definition.name : definition.displayName;
            if (string.IsNullOrWhiteSpace(definition.displayName))
            {
                issues.Add(new TowerCatalogValidationIssue(TowerCatalogValidationSeverity.Warning,
                    $"'{definition.name}': Display Name is empty."));
            }

            if (definition.baseDamage < 0f || definition.baseFireInterval <= 0f ||
                definition.baseAttackRange <= 0f || definition.projectileSpeed <= 0f ||
                definition.projectileHitRadius <= 0f || definition.baseProjectileCount < 1 ||
                definition.rankDamageMultiplier < 1f)
            {
                issues.Add(new TowerCatalogValidationIssue(TowerCatalogValidationSeverity.Error,
                    $"'{label}': one or more required combat values are invalid."));
            }

            if ((definition.explosionRadius > 0f) != (definition.explosionDamage > 0f))
            {
                issues.Add(new TowerCatalogValidationIssue(TowerCatalogValidationSeverity.Warning,
                    $"'{label}': Explosion Radius and Explosion Damage must both be greater than zero."));
            }

            if (definition.slowDuration > 0f && definition.slowMultiplier >= 1f)
            {
                issues.Add(new TowerCatalogValidationIssue(TowerCatalogValidationSeverity.Warning,
                    $"'{label}': Slow Duration is set but Slow Multiplier does not reduce speed."));
            }

            if (definition.slowDuration <= 0f && definition.slowMultiplier < 1f)
            {
                issues.Add(new TowerCatalogValidationIssue(TowerCatalogValidationSeverity.Warning,
                    $"'{label}': Slow Multiplier is set but Slow Duration is zero."));
            }

            if ((definition.poisonDuration > 0f) != (definition.poisonDamagePerTick > 0f))
            {
                issues.Add(new TowerCatalogValidationIssue(TowerCatalogValidationSeverity.Warning,
                    $"'{label}': Poison Duration and Damage per Tick must both be greater than zero."));
            }

            if (definition.poisonDuration > 0f && definition.poisonTickInterval <= 0f)
            {
                issues.Add(new TowerCatalogValidationIssue(TowerCatalogValidationSeverity.Warning,
                    $"'{label}': Poison Tick Interval must be greater than zero."));
            }

            if (string.IsNullOrWhiteSpace(definition.description) &&
                (definition.explosionDamage > 0f || definition.slowDuration > 0f || definition.poisonDuration > 0f || definition.projectileCountPerRank > 0))
            {
                issues.Add(new TowerCatalogValidationIssue(TowerCatalogValidationSeverity.Warning,
                    $"'{label}': add a Description so its special behavior is visible in the selection UI."));
            }
        }
    }
}
