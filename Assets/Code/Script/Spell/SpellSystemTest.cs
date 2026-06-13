using UnityEngine;
using Sirenix.OdinInspector;

/// <summary>
/// Attach to the player to test the spell system without any editor asset setup.
/// All nodes and graphs are created in code at runtime.
/// </summary>
public class SpellSystemTest : MonoBehaviour
{
    [Title("Test — Projectile direct")]
    [InfoBox("Tire un projectile sphere directement, sans passer par le graphe.")]
    [Button("1. Fire Direct Projectile"), GUIColor(0.4f, 0.8f, 1f)]
    private void TestDirectProjectile()
    {
        var sphere = CreateTestSphere(Color.red, 0.3f);

        var ctx = new SpellContext
        {
            Caster    = gameObject,
            Origin    = transform.position + transform.forward,
            Direction = transform.forward,
            Damage    = 20f,
            Speed     = 8f,
            Size      = 1f,
            Element   = ElementType.Fire,
            Emitter   = EmitterType.Projectile,
        };

        sphere.transform.position = ctx.Origin;
        sphere.AddComponent<SpellProjectile>().Initialize(ctx);
        Destroy(sphere, 5f);

        Debug.Log($"[SpellTest] Direct projectile fired — Damage: {ctx.Damage}, Element: {ctx.Element}");
    }

    [Title("Test — Graphe complet")]
    [InfoBox("Crée un graphe Emitter(Projectile) → Element(Fire) en code et l'exécute via SpellExecutor.")]
    [Button("2. Fire via SpellExecutor"), GUIColor(0.4f, 1f, 0.6f)]
    private void TestFullGraph()
    {
        // Prefab proxy : un simple sphere désactivé que SpellExecutor va cloner
        var prefabProxy = CreateTestSphere(Color.yellow, 0.3f);
        prefabProxy.SetActive(false);

        // Noeud Emitter
        var emitter = ScriptableObject.CreateInstance<EmitterNodeSO>();
        emitter.emitterType      = EmitterType.Projectile;
        emitter.projectilePrefab = prefabProxy;
        emitter.baseDamage       = 15f;
        emitter.baseSpeed        = 8f;
        emitter.baseSize         = 1f;
        emitter.complexityCost   = 1;

        // Noeud Element — multiplie les dégâts x1.5
        var element = ScriptableObject.CreateInstance<ElementNodeSO>();
        element.element          = ElementType.Fire;
        element.damageMultiplier = 1.5f;
        element.complexityCost   = 1;

        // Graphe : 0 (Emitter) → 1 (Element)
        var graph = ScriptableObject.CreateInstance<SpellGraphSO>();
        graph.nodes.Add(emitter);
        graph.nodes.Add(element);
        graph.connections.Add(new SpellGraphSO.Connection { fromIndex = 0, toIndex = 1 });
        graph.complexityBudget = 10;

        var ctx = new SpellContext
        {
            Caster    = gameObject,
            Origin    = transform.position + transform.forward,
            Direction = transform.forward,
        };

        SpellExecutor.Execute(graph, 0, ctx);

        // Le proxy n'est plus nécessaire une fois le clone créé
        Destroy(prefabProxy);

        Debug.Log($"[SpellTest] Graph executed — Nodes: {graph.nodes.Count}, " +
                  $"Final Damage: {ctx.Damage} (base 15 × 1.5 = 22.5 attendu)");
    }

    [Title("Test — Trigger OnHit → Nova")]
    [InfoBox("Projectile → Glace. Au OnHit, tire une Nova de feu. Vérifie la limite de génération.")]
    [Button("3. Fire Trigger Spell"), GUIColor(1f, 0.8f, 0.4f)]
    private void TestTriggerSpell()
    {
        var projectilePrefab = CreateTestSphere(Color.cyan, 0.3f);
        projectilePrefab.SetActive(false);

        var emitter = ScriptableObject.CreateInstance<EmitterNodeSO>();
        emitter.emitterType      = EmitterType.Projectile;
        emitter.projectilePrefab = projectilePrefab;
        emitter.baseDamage       = 10f;
        emitter.baseSpeed        = 6f;
        emitter.baseSize         = 1f;
        emitter.complexityCost   = 1;

        var element = ScriptableObject.CreateInstance<ElementNodeSO>();
        element.element        = ElementType.Ice;
        element.complexityCost = 1;

        var trigger = ScriptableObject.CreateInstance<TriggerNodeSO>();
        trigger.triggerType    = TriggerType.OnHit;
        trigger.complexityCost = 3;

        var effect = ScriptableObject.CreateInstance<EffectNodeSO>();
        effect.effectType      = EffectType.Nova;
        effect.radius          = 3f;
        effect.effectStrength  = 20f;
        effect.complexityCost  = 2;
        // Note: effect.effectPrefab est null → SpawnEffects skip silencieusement (normal pour ce test)

        // Graphe : 0(Emitter) → 1(Ice) → 2(OnHit) → 3(Nova)
        var graph = ScriptableObject.CreateInstance<SpellGraphSO>();
        graph.nodes.Add(emitter);  // 0
        graph.nodes.Add(element);  // 1
        graph.nodes.Add(trigger);  // 2
        graph.nodes.Add(effect);   // 3
        graph.connections.Add(new SpellGraphSO.Connection { fromIndex = 0, toIndex = 1 });
        graph.connections.Add(new SpellGraphSO.Connection { fromIndex = 1, toIndex = 2 });
        graph.connections.Add(new SpellGraphSO.Connection { fromIndex = 2, toIndex = 3 });
        graph.complexityBudget = 10;

        var ctx = new SpellContext
        {
            Caster    = gameObject,
            Origin    = transform.position + transform.forward,
            Direction = transform.forward,
        };

        SpellExecutor.Execute(graph, 0, ctx);
        Destroy(projectilePrefab);

        Debug.Log($"[SpellTest] Trigger spell fired — PendingTriggers: {ctx.PendingTriggers.Count} " +
                  $"(1 attendu). Le projectile déclenchera la Nova au OnHit.");
    }

    // ---- Helpers ----

    private static GameObject CreateTestSphere(Color color, float radius)
    {
        var go = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        go.transform.localScale               = Vector3.one * radius;
        go.GetComponent<Collider>().isTrigger = true;
        go.GetComponent<Renderer>().material.color = color;

        var rb           = go.AddComponent<Rigidbody>();
        rb.isKinematic   = true;  // mouvement via transform, pas la physique
        rb.useGravity    = false;

        go.name = "TestProjectile";
        return go;
    }
}
