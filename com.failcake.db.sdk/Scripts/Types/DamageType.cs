using System;

namespace HyenaQuest
{
    [Serializable, Flags]
    public enum DamageType
    {
        GENERIC = 1,
        CUT = 1 << 1,

        PIT = 1 << 2, // PIT
        FALL = 1 << 3, // Basically fall damage

        ELECTRIC = 1 << 4,
        NECK_SNAP = 1 << 5,
        BURN = 1 << 6, // Fire damage
        CURSE = 1 << 7,

        // SPECIAL ---
        INSTANT = 1 << 10,
        ABYSS = 1 << 11,
        ELECTRIC_ASHES = 1 << 12 // Instant electric damage

        // -----------
    }

}