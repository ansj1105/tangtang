# Megabonk-Inspired Combat Content Plan

This file records content direction only. It is not connected to live battle logic yet.

## Reference Summary

- Megabonk weapons differ by supported upgrade stats. Damage is broadly available, while critical damage, projectile bounce, projectile speed, duration, size, and knockback are weapon-dependent.
- Megabonk Tomes are run-limited passive upgrades that raise global stats such as movement speed, attack speed, damage, evasion, gold gain, max HP, HP regen, shield, silver gain, armor, pickup range, life steal, difficulty, duration, luck, projectile count, thorns, and XP gain.
- NyaonHunter already has matching data columns for many weapon stats in `SkillData.csv`: damage, scale, cooldown, range, duration, projectile count, rotate speed, bounce, knockback, bounce speed, penetration, speed, and attack count.

Sources checked:

- https://megabonk.wiki/wiki/Weapons
- https://megabonk.wiki/wiki/Tomes
- https://megabonk.wiki/wiki/Items

## Weapon Upgrade Profiles

Use `CombatContentPolicy.WeaponProfiles` as the first source for which upgrade options a skill may roll once the new upgrade system is wired.

| NyaonHunter skill | Megabonk-style archetype | Allowed upgrade stats |
| --- | --- | --- |
| PlasmaShot | Bow / Firestaff | Damage, crit chance, projectile count, projectile speed, knockback, size, cooldown |
| EnergyRing | Aura / Chunkers | Damage, projectile count, duration, size, knockback |
| PlasmaSpinner | Bone / Bananarang | Damage, projectile count, projectile bounce, projectile speed, duration, size |
| SuicideDrone | Mines / Rocket | Damage, projectile count, projectile speed, duration, size, cooldown |
| ElectricShock | Lightning Staff / Wireless Dagger | Damage, crit chance, projectile count, projectile bounce, projectile speed, cooldown |
| GravityBomb | Black Hole | Damage, duration, size, knockback, cooldown |
| TimeStopBomb | Frostwalker / Black Hole | Duration, size, knockback, cooldown |
| OrbitalBlades | Chunkers / Axe | Damage, crit chance, projectile count, duration, size |
| ElectronicField | Aura | Damage, duration, size, cooldown |
| SpectralSlash | Sword / Katana / Hero Sword | Damage, crit chance, crit damage, projectile count, projectile speed, size, knockback |

## Tome Passive Set

Use Tomes as run-level passive slots, separate from permanent lobby upgrades and equipment.

| Tome | Stat | Base value |
| --- | --- | ---: |
| Agility | Move speed | +15% |
| Cooldown | Attack speed | +7.5% |
| Damage | Damage | +8% |
| Evasion | Evasion | +10% |
| Golden | Gold gain | +12% |
| Health | Max HP | +25 |
| Knockback | Knockback | +20% |
| Precision | Crit chance | +7% |
| ProjectileSpeed | Projectile speed | +15% |
| Regen | HP regen per minute | +40 |
| Shield | Shield | +25 |
| Silver | Silver gain | +12% |
| Size | Size | +10% |
| Armor | Armor | +12% |
| Attraction | Pickup range | +75% |
| Bloody | Life steal | +10% |
| Chaos | Random stat | 1 roll |
| Cursed | Difficulty | +3.5% |
| Duration | Duration | +15% |
| Luck | Luck | +7% |
| Quantity | Projectile count | +1 |
| Thorns | Thorns damage | +15 |
| XP | XP gain | +9% |

## Step-by-Step Runtime Plan

1. Add passive-slot save data for a run only. Verify new runs start empty and continue data restores selected Tomes.
2. Add a Tome selection popup after level-up only when weapon choices are exhausted or a Tome reward is rolled. Verify no empty skill-select popup can pause the game.
3. Route all stat reads through one combat stat snapshot. Verify player, equipment, passive, grimoire, and shrine sources do not double-apply.
4. Apply weapon upgrade profiles to future weapon reward rolls. Verify each weapon only rolls supported stats.
5. Apply `CombatStatFormula.MaxWeaponEnhanceLevel` as a runtime cap after UI/save migration. Verify weapons stop at 40 without hiding existing level data.

## Guardrails

- Do not edit `SkillData.csv` balance numbers until the profile filtering and reward UI are connected.
- Do not connect Tomes to permanent `SpecialSkillData.csv`; Tomes are run-local passives.
- Keep a compatibility path for the current 5-star skill evolution flow until the new 40-upgrade weapon model has save migration.
