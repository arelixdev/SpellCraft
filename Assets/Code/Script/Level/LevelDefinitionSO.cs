using System.Collections.Generic;
using Sirenix.OdinInspector;
using UnityEngine;

/// <summary>
/// Un "skin" de niveau : son décor (optionnel) et la composition de POI qui lui est propre.
/// Piochée aléatoirement dans un LevelPoolSO par LevelDirector, ou référencée directement
/// pour un niveau fixe (ex. le Big Boss).
/// </summary>
[CreateAssetMenu(fileName = "NewLevel", menuName = "SpellCraft/Level Definition")]
public class LevelDefinitionSO : ScriptableObject
{
    [LabelText("Nom affiché")]
    public string DisplayName = "Niveau";

    [LabelText("Scène de décor")]
    [Tooltip("Nom de la scène (doit être dans les Build Settings) chargée en additif par-dessus Gameplay — terrain, NavMesh, éclairage, props. Vide = aucun décor chargé (à éviter en jeu).")]
    public string EnvironmentSceneName;

    [LabelText("Activités (POI)")]
    public List<ActivityPoolEntry> ActivityPool = new();
}
