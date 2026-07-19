# NyaonHunter Combat Stat Policy

This document records the intended combat stat rules before wiring every stat into runtime systems.

## Current Implementation Coverage

- Implemented today/in existing code: max HP, HP regen, damage reduction, critical rate, critical damage, attack rate, move speed, projectile count, projectile speed, duration, knockback, pickup range, XP gain, gold gain.
- Missing runtime systems: overheal decay, shield recovery, dodge, life steal, thorns, over-critical multi-roll damage, silver gain, elite damage split, jump and terrain speed rules, shrine/grimoire source buckets.
- Do not attach all formulas to `PlayerController.OnDamaged` or `CreatureController.OnDamaged` in one patch. Add one mechanic at a time with a visible stat source and a gameplay check.

## Stat Rules

- Max HP: base maximum health. Game over when current health reaches zero.
- HP regen: health recovered per minute. Runtime per-second regen is `hpRegenPerMinute / 60`.
- Overheal: healing can exceed max HP up to `currentVisibleHp + currentVisibleHp * overhealRate`. Extra HP decays when healing stops.
- Shield: absorbs hits like a separate protection layer. It starts recovering five seconds after the last hit. Overflow damage beyond max shield does not pass to HP.
- Armor: damage reduction chance/value is `n / (0.75 + n)`, where `n` is the sum of player, grimoire, and shrine armor percentages as decimals.
- Dodge: full-hit ignore chance is `n / (1 + n)`, where `n` is the sum of player, grimoire, and shrine dodge percentages as decimals.
- Life steal: each attack heals 1 HP per full 100 percent, then rolls the remainder chance for one extra HP.
- Thorns: on hit, fire thorns that deal final damage plus thorns stat. Main hit damage and thorns damage roll critical separately.
- Damage: weapon attack is multiplied by the damage stat according to the configured player/grimoire/shrine source buckets.
- Critical rate: values above 100 percent create guaranteed critical stacks plus a remainder roll. If two or more crits occur, multiplier follows `(n * 0.5)^2 + (n + 1)`.
- Critical damage: applied when critical damage occurs. Weapon-specific critical damage is only included when the weapon supports it.
- Attack speed: sum all source values in order.
- Projectile count: sum all source values in order; only the integer part creates additional projectiles.
- Projectile bounce: weapon-only stat. Grimoire and shrine do not add bounce.
- Size: weapon-size-supported only.
- Projectile speed: weapon-projectile-speed-supported only. Duration and knockback use the same source pattern.
- Duration: weapon-duration-supported only.
- Elite damage: applies to elite enemies only, not bosses. It multiplies damage before later critical or extra damage calculations.
- Knockback: weapon-knockback-supported only.
- Move speed: base movement speed plus item, grimoire, and shrine contributions. Water or mud surfaces use 40 percent of current movement speed.
- Extra jump: shrine-only integer value.
- Jump height: base jump height multiplied by item and shrine contributions.
- Luck: increases higher-grade rewards from objects and level-up choices.
- Difficulty: increases enemy spawn chance, enemy health, and rewards.
- Pickup range: base pickup range multiplied by grimoire and shrine contributions.
- XP gain: field XP shard reward multiplier. Character passive can add one more source bucket.
- Gold gain: field gold reward multiplier. Character passive can add one more source bucket.
- Silver gain: field silver reward multiplier.
- Elite spawn increase: shrine-only additive stat.
- Power-up multiplier: shrine-only additive stat.
- Power-up drop chance: shrine-only additive stat.

## Implementation Order

1. Add source buckets to data: player, equipment/item, grimoire, shrine, weapon, character passive.
2. Wire defensive stats first: armor, dodge, shield, overheal. Verify `PlayerController.OnDamaged`, `Healing`, and `UI_HP_Bar`.
3. Wire offensive stats next: damage, critical, elite damage, projectile count/speed/size/duration/knockback.
4. Wire economy and reward stats: XP, gold, silver, luck, difficulty, power-up drop.
5. Add UI stat descriptions only after the runtime values and displayed values share one calculation source.

## Upgrade Reward Grade Policy

These values are prepared as policy and formula helpers only. Do not connect them to live upgrade logic until the upgrade reward data model, UI, save migration, and balance tests are ready.

| Grade | Multiplier | Appearance rate |
| --- | ---: | ---: |
| Common | x1.0 | 70% |
| Uncommon | x1.2 | 15% |
| Rare | x1.4 | 6% |
| Epic | x1.6 | 1.5% |
| Legendary | x2.0 | 7.5% |

Legendary uses the remaining probability from the provided table: `100 - (70 + 15 + 6 + 1.5) = 7.5`.

Example sword upgrade base values:

| Grade | Damage | Knockback | Projectile count | Size |
| --- | ---: | ---: | ---: | ---: |
| Common | +2 | +0.5 | +1 | +20% |
| Uncommon | +2.4 | +0.6 | +1.2 | +24% |
| Rare | +2.8 | +0.7 | +1.4 | +28% |
| Epic | +3.2 | +0.8 | +1.6 | +32% |
| Legendary | +4.0 | +1.0 | +2.0 | +40% |

The same grade multiplier policy applies to grimoire and charging shrine rewards.

Weapons should be capped at 40 enhancement attempts/levels when this system is connected. Existing `EquipmentData.Grade_MaxLevel` values currently exceed 40 for higher equipment grades, so runtime cap wiring must be explicit and tested rather than silently changing CSV values.
