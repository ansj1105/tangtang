using System;

public static class CombatContentPolicy
{
    [Flags]
    public enum WeaponUpgradeStat
    {
        None = 0,
        Damage = 1 << 0,
        CriticalChance = 1 << 1,
        CriticalDamage = 1 << 2,
        ProjectileCount = 1 << 3,
        ProjectileBounce = 1 << 4,
        ProjectileSpeed = 1 << 5,
        Knockback = 1 << 6,
        Duration = 1 << 7,
        Size = 1 << 8,
        Cooldown = 1 << 9
    }

    public enum PassiveStat
    {
        MoveSpeed,
        AttackSpeed,
        Damage,
        CriticalChance,
        Evasion,
        GoldGain,
        MaxHp,
        Knockback,
        ProjectileSpeed,
        HpRegen,
        Shield,
        SilverGain,
        Size,
        Armor,
        PickupRange,
        LifeSteal,
        RandomStat,
        Difficulty,
        Duration,
        Luck,
        ProjectileCount,
        Thorns,
        XpGain
    }

    public sealed class WeaponUpgradeProfile
    {
        public readonly Define.SkillType SkillType;
        public readonly WeaponUpgradeStat AllowedStats;
        public readonly string Archetype;

        public WeaponUpgradeProfile(Define.SkillType skillType, WeaponUpgradeStat allowedStats, string archetype)
        {
            SkillType = skillType;
            AllowedStats = allowedStats;
            Archetype = archetype;
        }

        public bool Allows(WeaponUpgradeStat stat)
        {
            return (AllowedStats & stat) == stat;
        }
    }

    public sealed class TomeDefinition
    {
        public readonly string Key;
        public readonly PassiveStat Stat;
        public readonly float BaseValue;
        public readonly bool IsPercent;

        public TomeDefinition(string key, PassiveStat stat, float baseValue, bool isPercent)
        {
            Key = key;
            Stat = stat;
            BaseValue = baseValue;
            IsPercent = isPercent;
        }
    }

    public static readonly WeaponUpgradeProfile[] WeaponProfiles =
    {
        new WeaponUpgradeProfile(Define.SkillType.PlasmaShot, WeaponUpgradeStat.Damage | WeaponUpgradeStat.CriticalChance | WeaponUpgradeStat.ProjectileCount | WeaponUpgradeStat.ProjectileSpeed | WeaponUpgradeStat.Knockback | WeaponUpgradeStat.Size | WeaponUpgradeStat.Cooldown, "Bow / Firestaff"),
        new WeaponUpgradeProfile(Define.SkillType.EnergyRing, WeaponUpgradeStat.Damage | WeaponUpgradeStat.ProjectileCount | WeaponUpgradeStat.Duration | WeaponUpgradeStat.Size | WeaponUpgradeStat.Knockback, "Aura / Chunkers"),
        new WeaponUpgradeProfile(Define.SkillType.PlasmaSpinner, WeaponUpgradeStat.Damage | WeaponUpgradeStat.ProjectileCount | WeaponUpgradeStat.ProjectileBounce | WeaponUpgradeStat.ProjectileSpeed | WeaponUpgradeStat.Duration | WeaponUpgradeStat.Size, "Bone / Bananarang"),
        new WeaponUpgradeProfile(Define.SkillType.SuicideDrone, WeaponUpgradeStat.Damage | WeaponUpgradeStat.ProjectileCount | WeaponUpgradeStat.ProjectileSpeed | WeaponUpgradeStat.Duration | WeaponUpgradeStat.Size | WeaponUpgradeStat.Cooldown, "Mines / Rocket"),
        new WeaponUpgradeProfile(Define.SkillType.ElectricShock, WeaponUpgradeStat.Damage | WeaponUpgradeStat.CriticalChance | WeaponUpgradeStat.ProjectileCount | WeaponUpgradeStat.ProjectileBounce | WeaponUpgradeStat.ProjectileSpeed | WeaponUpgradeStat.Cooldown, "Lightning Staff / Wireless Dagger"),
        new WeaponUpgradeProfile(Define.SkillType.GravityBomb, WeaponUpgradeStat.Damage | WeaponUpgradeStat.Duration | WeaponUpgradeStat.Size | WeaponUpgradeStat.Knockback | WeaponUpgradeStat.Cooldown, "Black Hole"),
        new WeaponUpgradeProfile(Define.SkillType.TimeStopBomb, WeaponUpgradeStat.Duration | WeaponUpgradeStat.Size | WeaponUpgradeStat.Knockback | WeaponUpgradeStat.Cooldown, "Frostwalker / Black Hole"),
        new WeaponUpgradeProfile(Define.SkillType.OrbitalBlades, WeaponUpgradeStat.Damage | WeaponUpgradeStat.CriticalChance | WeaponUpgradeStat.ProjectileCount | WeaponUpgradeStat.Duration | WeaponUpgradeStat.Size, "Chunkers / Axe"),
        new WeaponUpgradeProfile(Define.SkillType.ElectronicField, WeaponUpgradeStat.Damage | WeaponUpgradeStat.Duration | WeaponUpgradeStat.Size | WeaponUpgradeStat.Cooldown, "Aura"),
        new WeaponUpgradeProfile(Define.SkillType.SpectralSlash, WeaponUpgradeStat.Damage | WeaponUpgradeStat.CriticalChance | WeaponUpgradeStat.CriticalDamage | WeaponUpgradeStat.ProjectileCount | WeaponUpgradeStat.ProjectileSpeed | WeaponUpgradeStat.Size | WeaponUpgradeStat.Knockback, "Sword / Katana / Hero Sword")
    };

    public static readonly TomeDefinition[] TomeDefinitions =
    {
        new TomeDefinition("Agility", PassiveStat.MoveSpeed, 15f, true),
        new TomeDefinition("Cooldown", PassiveStat.AttackSpeed, 7.5f, true),
        new TomeDefinition("Damage", PassiveStat.Damage, 8f, true),
        new TomeDefinition("Evasion", PassiveStat.Evasion, 10f, true),
        new TomeDefinition("Golden", PassiveStat.GoldGain, 12f, true),
        new TomeDefinition("Health", PassiveStat.MaxHp, 25f, false),
        new TomeDefinition("Knockback", PassiveStat.Knockback, 20f, true),
        new TomeDefinition("Precision", PassiveStat.CriticalChance, 7f, true),
        new TomeDefinition("ProjectileSpeed", PassiveStat.ProjectileSpeed, 15f, true),
        new TomeDefinition("Regen", PassiveStat.HpRegen, 40f, false),
        new TomeDefinition("Shield", PassiveStat.Shield, 25f, false),
        new TomeDefinition("Silver", PassiveStat.SilverGain, 12f, true),
        new TomeDefinition("Size", PassiveStat.Size, 10f, true),
        new TomeDefinition("Armor", PassiveStat.Armor, 12f, true),
        new TomeDefinition("Attraction", PassiveStat.PickupRange, 75f, true),
        new TomeDefinition("Bloody", PassiveStat.LifeSteal, 10f, true),
        new TomeDefinition("Chaos", PassiveStat.RandomStat, 1f, false),
        new TomeDefinition("Cursed", PassiveStat.Difficulty, 3.5f, true),
        new TomeDefinition("Duration", PassiveStat.Duration, 15f, true),
        new TomeDefinition("Luck", PassiveStat.Luck, 7f, true),
        new TomeDefinition("Quantity", PassiveStat.ProjectileCount, 1f, false),
        new TomeDefinition("Thorns", PassiveStat.Thorns, 15f, false),
        new TomeDefinition("XP", PassiveStat.XpGain, 9f, true)
    };

    public static WeaponUpgradeProfile GetWeaponProfile(Define.SkillType skillType)
    {
        for (int i = 0; i < WeaponProfiles.Length; i++)
        {
            if (WeaponProfiles[i].SkillType == skillType)
                return WeaponProfiles[i];
        }

        return null;
    }
}
