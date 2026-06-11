public enum NodeType      { Emitter, Element, Behavior, Condition, Trigger, Effect }
public enum ElementType   { None, Fire, Ice, Lightning, Arcane, Poison }
public enum EmitterType   { None, Projectile, Zone, Cone, Beam, Self }
public enum BehaviorType  { Pierce, Bounce, Split, Homing, Orbit }
public enum ConditionType { TargetHasStatus, SelfAtFullHP, ComboCount, EnemiesNearby }
public enum TriggerType   { OnHit, OnKill, OnExpire, OnTick }
public enum EffectType    { Explosion, Nova, DamageOverTime, Slow, Pull, Push }
public enum LauncherType  { AutoCast, KeyBind, OnEvent, Passive }
public enum GameEventType { OnKill, OnDamageTaken }
