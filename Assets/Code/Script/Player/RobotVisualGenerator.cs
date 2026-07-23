using Synty.SidekickCharacters.API;
using Synty.SidekickCharacters.Database;
using Synty.SidekickCharacters.Database.DTO;
using Synty.SidekickCharacters.Enums;
using Synty.SidekickCharacters.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

/// <summary>
/// Recette d'apparence d'un robot : les presets Sidekick tirés au sort par
/// RobotArchetypeSO.Roll(). Volontairement séparée du mesh construit : l'écran de
/// CharacterSelect n'affiche que du texte (RobotCardView), donc on ne construit le
/// GameObject qu'une fois le robot réellement choisi (RobotLoader.ApplyVisual).
/// </summary>
public class RobotVisualRecipe
{
    public SidekickPartPreset HeadPreset;
    public SidekickPartPreset UpperBodyPreset;
    public SidekickPartPreset LowerBodyPreset;
    public SidekickColorPreset ColorPreset;
}

/// <summary>
/// Génère l'apparence des robots joueurs via l'API runtime Sidekick Characters, en piochant
/// au hasard parmi les presets de l'espèce ScifiRobots. La connexion DB + le SidekickRuntime
/// sont coûteux à ouvrir : on les garde en singleton statique pour tout le process plutôt que
/// d'en recréer un par robot (contrairement aux démos Synty qui en créent un par scène).
/// Limite connue : le SidekickRuntime partagé réutilise le même Material/Texture de base
/// (M_BaseMaterial), donc ses pixels de couleur sont réécrits à chaque BuildVisual() — sans
/// impact tant qu'un seul robot joueur est affiché à la fois.
/// </summary>
public static class RobotVisualGenerator
{
    private const string _OUTPUT_MODEL_NAME = "Robot Visual";

    private static DatabaseManager _dbManager;
    private static SidekickRuntime _sidekickRuntime;
    private static SidekickSpecies _scifiRobotsSpecies;

    private static void EnsureInitialized()
    {
        if (_sidekickRuntime != null)
        {
            return;
        }

        _dbManager = new DatabaseManager();

        GameObject baseModel = Resources.Load<GameObject>("Meshes/SK_BaseModel");
        Material baseMaterial = Resources.Load<Material>("Materials/M_BaseMaterial");

        _sidekickRuntime = new SidekickRuntime(baseModel, baseMaterial, null, _dbManager);
        SidekickRuntime.PopulateToolData(_sidekickRuntime);

        _scifiRobotsSpecies = SidekickSpecies.GetAll(_dbManager)
            .FirstOrDefault(species => species.Name.IndexOf("robot", StringComparison.OrdinalIgnoreCase) >= 0);

        if (_scifiRobotsSpecies == null)
        {
            Debug.LogError("[RobotVisualGenerator] Aucune espèce ScifiRobots trouvée dans la base Sidekick.");
        }
    }

    /// <summary>
    /// Pioche au hasard une combinaison de presets ScifiRobots (tête/torse/jambes + couleur).
    /// Ne construit aucun mesh : appelé à chaque Roll() d'archétype, donc doit rester léger.
    /// </summary>
    public static RobotVisualRecipe RollRandomRecipe()
    {
        EnsureInitialized();

        if (_scifiRobotsSpecies == null)
        {
            return null;
        }

        return new RobotVisualRecipe
        {
            HeadPreset = RandomPreset(PartGroup.Head),
            UpperBodyPreset = RandomPreset(PartGroup.UpperBody),
            LowerBodyPreset = RandomPreset(PartGroup.LowerBody),
            ColorPreset = RandomColorPreset()
        };
    }

    /// <summary>
    /// Assemble la recette en un GameObject riggé (non parenté). Appelé une seule fois par
    /// robot réellement appliqué (RobotLoader), jamais pour les cartes de CharacterSelect.
    /// </summary>
    public static GameObject BuildVisual(RobotVisualRecipe recipe)
    {
        EnsureInitialized();

        if (recipe == null)
        {
            return null;
        }

        List<SkinnedMeshRenderer> partsToUse = new List<SkinnedMeshRenderer>();
        foreach (SidekickPartPreset preset in new[] { recipe.HeadPreset, recipe.UpperBodyPreset, recipe.LowerBodyPreset })
        {
            if (preset == null)
            {
                continue;
            }

            foreach (SidekickPartPresetRow row in SidekickPartPresetRow.GetAllByPreset(_dbManager, preset))
            {
                if (string.IsNullOrEmpty(row.PartName))
                {
                    continue;
                }

                CharacterPartType type = Enum.Parse<CharacterPartType>(CharacterPartTypeUtils.GetTypeNameFromShortcode(row.PartType));
                if (!_sidekickRuntime.MappedPartDictionary.TryGetValue(type, out Dictionary<string, SidekickPart> partsOfType))
                {
                    continue;
                }

                if (!partsOfType.TryGetValue(row.PartName, out SidekickPart part))
                {
                    continue;
                }

                GameObject partModel = part.GetPartModel();
                SkinnedMeshRenderer mesh = partModel != null ? partModel.GetComponentInChildren<SkinnedMeshRenderer>() : null;
                if (mesh != null)
                {
                    partsToUse.Add(mesh);
                }
            }
        }

        if (partsToUse.Count == 0)
        {
            return null;
        }

        if (recipe.ColorPreset != null)
        {
            foreach (SidekickColorPresetRow row in SidekickColorPresetRow.GetAllByPreset(_dbManager, recipe.ColorPreset))
            {
                SidekickColorRow colorRow = SidekickColorRow.CreateFromPresetColorRow(row);
                foreach (ColorType property in Enum.GetValues(typeof(ColorType)))
                {
                    _sidekickRuntime.UpdateColor(property, colorRow);
                }
            }
        }

        return _sidekickRuntime.CreateCharacter(_OUTPUT_MODEL_NAME, partsToUse, false, true);
    }

    private static SidekickPartPreset RandomPreset(PartGroup group)
    {
        List<SidekickPartPreset> presets = SidekickPartPreset.GetAllBySpeciesAndGroup(_dbManager, _scifiRobotsSpecies, group)
            .Where(preset => preset.HasAllPartsAvailable(_dbManager))
            .ToList();

        return presets.Count > 0 ? presets[UnityEngine.Random.Range(0, presets.Count)] : null;
    }

    private static SidekickColorPreset RandomColorPreset()
    {
        List<SidekickColorPreset> presets = SidekickColorPreset.GetAllByColorGroupAndSpecies(_dbManager, ColorGroup.Species, _scifiRobotsSpecies);
        if (presets.Count == 0)
        {
            presets = SidekickColorPreset.GetAllByColorGroupAndSpecies(_dbManager, ColorGroup.Outfits, _scifiRobotsSpecies);
        }

        return presets.Count > 0 ? presets[UnityEngine.Random.Range(0, presets.Count)] : null;
    }
}
