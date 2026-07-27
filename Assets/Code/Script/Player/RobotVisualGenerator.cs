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
    // Null = coque/jointures laissées dans leur teinte d'origine (tirage possible, voir RollRandomRecipe).
    public Color? PrimaryColor;
    public Color? SecondaryColor;
    public Color GlowColor;
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

    // La base Sidekick ne fournit aucun ColorPreset pour l'espèce Robots (Species/Outfits/
    // Attachments/Materials/Elements sont tous vides pour elle) : impossible de piocher une
    // palette pré-faite comme pour les espèces humanoïdes. On peint donc nous-mêmes deux teintes :
    // une pour la coque principale (Metal 02, la zone dominante sur le mesh) et une autre pour
    // les jointures/pièces intérieures (Metal 01/03/04, visibles aux épaules/poignets/chevilles
    // et sur les liserés). Les zones Materials restantes (Glow, Digital Screen, Glass,
    // Jewellery...) ne sont jamais touchées : ce sont des détails avec leur propre couleur
    // d'origine (confirmé visuellement sur un robot non repeint). Les couleurs elles-mêmes
    // viennent de RobotColorPaletteSO (Resources/RobotColorPalette.asset), éditable dans
    // l'Inspector plutôt que codée en dur ici — même convention que NodeRarityPaletteSO.
    private const string _COLOR_PALETTE_RESOURCE_PATH = "RobotColorPalette";

    // Couleur des yeux (zones "Glow 0X"), tirée indépendamment du primaire/secondaire. Le
    // Shader Sidekick_ShaderGraph n'a aucune sortie PBR câblée (Metallic/Smoothness/Emission
    // à 0 edge dans le graphe) — plutôt que de toucher à ce shader partagé par tous les
    // personnages, on découpe les triangles de la zone "Glow" dans un second submesh (voir
    // SplitOffGlowSubmesh) et on leur assigne un Material distinct, isolé, avec une vraie
    // émission URP Lit — donc un vrai glow/bloom, sans risque pour les autres personnages.
    private static RobotColorPaletteSO _colorPalette;

    private static DatabaseManager _dbManager;
    private static SidekickRuntime _sidekickRuntime;
    private static SidekickSpecies _scifiRobotsSpecies;
    private static List<SidekickColorProperty> _primaryColorProperties;
    private static List<SidekickColorProperty> _secondaryColorProperties;
    private static List<SidekickColorProperty> _eyeGlowColorProperties;
    private static Texture2D _colorMapTexture;
    private static Color[] _pristineColorMapPixels;
    private static HashSet<Vector2Int> _glowTexelKeys;

    private static void EnsureInitialized()
    {
        if (_sidekickRuntime != null)
        {
            return;
        }

        _dbManager = new DatabaseManager();

        _colorPalette = Resources.Load<RobotColorPaletteSO>(_COLOR_PALETTE_RESOURCE_PATH);
        if (_colorPalette == null)
        {
            Debug.LogError("[RobotVisualGenerator] RobotColorPalette introuvable dans Resources — les robots resteront non peints.");
        }

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

        List<SidekickColorProperty> metalProperties = SidekickColorProperty.GetAllByGroup(_dbManager, ColorGroup.Materials)
            .Where(property => property.Name.StartsWith("Metal ", StringComparison.OrdinalIgnoreCase))
            .ToList();

        // Repéré en isolant chaque zone Metal 0X sur un robot test : "Metal 02" est la coque
        // principale (bras/jambes/torse/tête), les trois autres ne couvrent que les jointures
        // (épaule, poignet, cheville) et les liserés (taille, cuisse, pied).
        _primaryColorProperties = metalProperties
            .Where(property => property.Name.Equals("Metal 02", StringComparison.OrdinalIgnoreCase))
            .ToList();
        _secondaryColorProperties = metalProperties
            .Where(property => !property.Name.Equals("Metal 02", StringComparison.OrdinalIgnoreCase))
            .ToList();

        // Repéré via le même test visuel : "Glow 01/02" est la zone de la lentille de l'œil
        // (visible sur les presets de tête qui ont un visor/lentille, pas tous).
        _eyeGlowColorProperties = SidekickColorProperty.GetAllByGroup(_dbManager, ColorGroup.Materials)
            .Where(property => property.Name.StartsWith("Glow", StringComparison.OrdinalIgnoreCase))
            .ToList();

        // Le SidekickRuntime partage un seul Material/Texture entre tous les BuildVisual() :
        // sans ce cliché des pixels d'origine, un robot "non peint" (PrimaryColor == null)
        // hériterait simplement de la dernière couleur peinte par le robot précédent au lieu
        // de revenir à sa vraie teinte de base.
        _colorMapTexture = baseMaterial.GetTexture(_COLOR_MAP_PROPERTY) as Texture2D;
        _pristineColorMapPixels = _colorMapTexture != null ? _colorMapTexture.GetPixels() : null;

        // Coordonnées en texels (dans le petit atlas "_ColorMap") des blocs 2x2 assignés aux
        // zones "Glow 0X" — vérifié sur un mesh de tête réel : ses UV pointent bien pile sur
        // ces texels. Sert à repérer, pour n'importe quelle tête, quels triangles appartiennent
        // à l'œil plutôt qu'à la coque.
        _glowTexelKeys = new HashSet<Vector2Int>();
        if (_colorMapTexture != null)
        {
            foreach (SidekickColorProperty property in _eyeGlowColorProperties)
            {
                int u = property.U * 2;
                int v = property.V * 2;
                _glowTexelKeys.Add(new Vector2Int(u, v));
                _glowTexelKeys.Add(new Vector2Int(u + 1, v));
                _glowTexelKeys.Add(new Vector2Int(u, v + 1));
                _glowTexelKeys.Add(new Vector2Int(u + 1, v + 1));
            }
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

        // La secondaire n'est tirée que si la principale l'est aussi : un robot "non peint"
        // ne doit pas se retrouver avec des jointures colorées au hasard sur une coque grise.
        Color? primary = RandomSolidColor();
        Color? secondary = primary.HasValue ? RandomSolidColor(primary.Value) : null;

        return new RobotVisualRecipe
        {
            HeadPreset = RandomPreset(PartGroup.Head),
            UpperBodyPreset = RandomPreset(PartGroup.UpperBody),
            LowerBodyPreset = RandomPreset(PartGroup.LowerBody),
            PrimaryColor = primary,
            SecondaryColor = secondary,
            GlowColor = RandomGlowColor()
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

        // Repart toujours d'un colormap vierge avant de peindre : sinon un robot "non peint"
        // (PrimaryColor == null) hériterait de la couleur laissée par le robot précédent sur
        // la texture partagée, au lieu de revenir à sa vraie teinte de base.
        RestorePristineColorMap();
        PaintZones(_primaryColorProperties, recipe.PrimaryColor);
        PaintZones(_secondaryColorProperties, recipe.SecondaryColor);
        // Toujours peint (pas de "pas de couleur" ici) : contrairement à la coque, l'œil doit
        // rester reconnaissable sur tous les robots, peints ou non.
        PaintZones(_eyeGlowColorProperties, recipe.GlowColor);

        GameObject character = _sidekickRuntime.CreateCharacter(_OUTPUT_MODEL_NAME, partsToUse, false, true);
        DetachSharedColorMaterial(character);

        foreach (SkinnedMeshRenderer renderer in character.GetComponentsInChildren<SkinnedMeshRenderer>())
        {
            SplitOffGlowSubmesh(renderer, recipe.GlowColor);
        }

        return character;
    }

    // Découpe, si présents, les triangles de la zone "Glow" du mesh (repérés via leurs UV,
    // voir _glowTexelKeys) dans un second submesh, et leur assigne un Material séparé avec une
    // vraie émission — au lieu de dépendre du MainColor peint dans PaintZones (qui reste posé
    // en secours, pour les têtes sans lentille où ce split ne trouve aucun triangle).
    private static void SplitOffGlowSubmesh(SkinnedMeshRenderer renderer, Color glowColor)
    {
        Mesh sourceMesh = renderer.sharedMesh;
        if (sourceMesh == null || _colorMapTexture == null)
        {
            return;
        }

        Vector2[] uv = sourceMesh.uv;
        int[] triangles = sourceMesh.triangles;
        List<int> mainTriangles = new List<int>(triangles.Length);
        List<int> glowTriangles = new List<int>();

        for (int i = 0; i < triangles.Length; i += 3)
        {
            bool isGlow = IsGlowVertex(uv[triangles[i]]) || IsGlowVertex(uv[triangles[i + 1]]) || IsGlowVertex(uv[triangles[i + 2]]);
            List<int> target = isGlow ? glowTriangles : mainTriangles;
            target.Add(triangles[i]);
            target.Add(triangles[i + 1]);
            target.Add(triangles[i + 2]);
        }

        if (glowTriangles.Count == 0)
        {
            return;
        }

        Mesh splitMesh = UnityEngine.Object.Instantiate(sourceMesh);
        splitMesh.subMeshCount = 2;
        splitMesh.SetTriangles(mainTriangles, 0);
        splitMesh.SetTriangles(glowTriangles, 1);
        renderer.sharedMesh = splitMesh;
        renderer.sharedMaterials = new[] { renderer.sharedMaterial, CreateGlowMaterial(glowColor) };
    }

    private static bool IsGlowVertex(Vector2 uv)
    {
        int x = Mathf.RoundToInt(uv.x * _colorMapTexture.width);
        int y = Mathf.RoundToInt(uv.y * _colorMapTexture.height);
        return _glowTexelKeys.Contains(new Vector2Int(x, y));
    }

    private static Material CreateGlowMaterial(Color glowColor)
    {
        Material material = new Material(Shader.Find("Universal Render Pipeline/Lit"));
        material.SetColor("_BaseColor", glowColor);
        material.EnableKeyword("_EMISSION");
        material.globalIlluminationFlags = MaterialGlobalIlluminationFlags.RealtimeEmissive;
        material.SetColor("_EmissionColor", glowColor * 2.5f);
        return material;
    }

    private static void RestorePristineColorMap()
    {
        if (_colorMapTexture == null || _pristineColorMapPixels == null)
        {
            return;
        }

        _colorMapTexture.SetPixels(_pristineColorMapPixels);
        _colorMapTexture.Apply();
    }

    private static void PaintZones(List<SidekickColorProperty> properties, Color? color)
    {
        if (!color.HasValue)
        {
            return;
        }

        foreach (SidekickColorProperty property in properties)
        {
            SidekickColorRow colorRow = new SidekickColorRow
            {
                ColorProperty = property,
                NiceColor = color.Value
            };
            _sidekickRuntime.UpdateColor(ColorType.MainColor, colorRow);
        }
    }

    // CreateCharacter() assigne le Material/Texture partagé du SidekickRuntime (voir le
    // commentaire de classe) : sans ça, le prochain BuildVisual() (le robot suivant tiré)
    // repeindrait la même texture et changerait rétroactivement la couleur de tous les robots
    // déjà construits qui la partagent encore — visible dès que plusieurs cartes de
    // CharacterSelect sont affichées en même temps. On clone donc le Material et sa texture
    // "_ColorMap" pour que chaque robot garde sa propre couleur, figée au moment de sa création.
    private const string _COLOR_MAP_PROPERTY = "_ColorMap";

    private static void DetachSharedColorMaterial(GameObject character)
    {
        foreach (Renderer renderer in character.GetComponentsInChildren<Renderer>())
        {
            Material materialClone = new Material(renderer.sharedMaterial);

            if (materialClone.GetTexture(_COLOR_MAP_PROPERTY) is Texture2D colorMap)
            {
                materialClone.SetTexture(_COLOR_MAP_PROPERTY, UnityEngine.Object.Instantiate(colorMap));
            }

            renderer.sharedMaterial = materialClone;
        }
    }

    // Un tirage sur (palette + 1) : le slot supplémentaire veut dire "pas de peinture", pour
    // qu'une partie des robots gardent leur coque grise/marron d'origine au lieu d'être tous unis.
    private static Color? RandomSolidColor()
    {
        List<Color> palette = _colorPalette != null ? _colorPalette.BodyColors : null;
        if (palette == null || palette.Count == 0)
        {
            return null;
        }

        int roll = UnityEngine.Random.Range(0, palette.Count + 1);
        return roll < palette.Count ? palette[roll] : (Color?) null;
    }

    // Variante pour la teinte secondaire : toujours peinte (pas de slot "aucune couleur"),
    // mais jamais égale à la principale pour garantir un vrai contraste entre les deux.
    private static Color RandomSolidColor(Color exclude)
    {
        List<Color> palette = _colorPalette.BodyColors;
        Color picked;
        do
        {
            picked = palette[UnityEngine.Random.Range(0, palette.Count)];
        } while (picked == exclude && palette.Count > 1);

        return picked;
    }

    private static Color RandomGlowColor()
    {
        List<Color> palette = _colorPalette != null ? _colorPalette.GlowColors : null;
        if (palette == null || palette.Count == 0)
        {
            return Color.white;
        }

        return palette[UnityEngine.Random.Range(0, palette.Count)];
    }

    private static SidekickPartPreset RandomPreset(PartGroup group)
    {
        List<SidekickPartPreset> presets = SidekickPartPreset.GetAllBySpeciesAndGroup(_dbManager, _scifiRobotsSpecies, group)
            .Where(preset => preset.HasAllPartsAvailable(_dbManager))
            .ToList();

        return presets.Count > 0 ? presets[UnityEngine.Random.Range(0, presets.Count)] : null;
    }

}
