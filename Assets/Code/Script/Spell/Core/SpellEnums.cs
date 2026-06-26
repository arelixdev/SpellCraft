public enum NodeType      { Emitter, Element, Behavior, Condition, Trigger, Effect }
public enum NodeRarity    { Commun, Rare, Epique, Boss, Corrompu }
public enum ElementType   { None, Fire, Ice, Lightning, Arcane, Poison }
public enum EmitterType   { None, Projectile, Zone, Cone, Beam, Self, Grenade, Orbital }
public enum BehaviorType  { Pierce, Bounce, Split, Homing, Orbit }
public enum ConditionType { TargetHasStatus, SelfAtFullHP, ComboCount, EnemiesNearby }
public enum TriggerType        { OnHit, OnKill, OnExpire, OnTick }
public enum TriggerSpawnSource { Projectile, Target, Caster }
public enum TriggerDirectionMode { Inherit, AwayFromCaster, TowardCaster, Random }
public enum EffectType    { Explosion, Nova, DamageOverTime, Slow, Pull, Push }
public enum LauncherType  { AutoCast, KeyBind, OnEvent, Passive }
public enum GameEventType { OnKill, OnDamageTaken }
public enum ZoneType      { StaticOnPlayer, GrowingOnPlayer, GrowingOnGround }
public enum ZoneDamageMode { Tick, OnEnter, Both }
