using System.Collections.Generic;
using UnityEngine;
using Sirenix.OdinInspector;

/// <summary>
/// Palettes de couleurs pour les robots générés par RobotVisualGenerator, chargée via
/// Resources.Load("RobotColorPalette") (même convention que NodeRarityPaletteSO) plutôt que
/// wirée dans l'Inspector : RobotVisualGenerator est une classe statique sans MonoBehaviour
/// à qui assigner une référence.
/// </summary>
[CreateAssetMenu(fileName = "RobotColorPalette", menuName = "SpellCraft/Robot Color Palette")]
public class RobotColorPaletteSO : ScriptableObject
{
    [Tooltip("Couleurs possibles pour la coque principale et les jointures.")]
    [ListDrawerSettings(ShowIndexLabels = false)]
    public List<Color> BodyColors = new();

    [Tooltip("Couleurs possibles pour le glow des yeux.")]
    [ListDrawerSettings(ShowIndexLabels = false)]
    public List<Color> GlowColors = new();
}
