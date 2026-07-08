#region

using System;

#endregion

namespace HyenaQuest
{
    [Serializable]
    public enum STEAM_STATS : byte
    {
        DEATHS = 0,
        SCRAPS,
        DELIVERIES,
        ARRESTED,
        ROUNDS = 200
    }

    [Serializable]
    public enum STEAM_ACHIEVEMENTS : byte
    {
        NONE = 0,

        // EASTER EGGS -----
        ACHIEVEMENT_PIZZA,
        ACHIEVEMENT_FORBIDDEN_PIZZA,
        ACHIEVEMENT_MISSING_TEXTURE,
        ACHIEVEMENT_OUT_OF_BOUNDS,
        ACHIEVEMENT_SURFER,
        ACHIEVEMENT_FORBIDDEN_LOVE,
        ACHIEVEMENT_BRICKY,
        ACHIEVEMENT_PRACTICE,
        ACHIEVEMENT_UNIVERSE_LOAD,
        ACHIEVEMENT_SPEEDRUN,
        ACHIEVEMENT_URCHIN,

        // MAPS ----
        ACHIEVEMENT_MAP_APARTMENTS,
        ACHIEVEMENT_MAP_TRAIN,
        ACHIEVEMENT_MAP_CITY,
        ACHIEVEMENT_MAP_FRACTURE,
        // --------

        ACHIEVEMENT_CHEAT_DEATH,
        ACHIEVEMENT_SQUEAKY_CLEAN,

        ACHIEVEMENT_PIZZA_TUNA,
        ACHIEVEMENT_PIZZA_PEPPERONI,
        ACHIEVEMENT_PIZZA_VEGGIE,

        ACHIEVEMENT_STAN,

        ACHIEVEMENT_MAP_TRENCHES,
        ACHIEVEMENT_WELCOME,

        ACHIEVEMENT_NO_DELIVERY_DAMAGE,
        ACHIEVEMENT_WHY_WOULD_YOU,
        ACHIEVEMENT_DELETE_EMPLOYEEN,
        // -----------------

        // STATS CONTROLLED -----
        ACHIEVEMENT_SCRAPPER = 100,
        ACHIEVEMENT_DELIVERY,
        ACHIEVEMENT_ARRESTED,
        ACHIEVEMENT_MASTER_MIND,

        ACHIEVEMENT_DEATH,

        // SPECIAL ----
        ACHIEVEMENT_DEV = 230,

        ACHIEVEMENT_KOFI
        // ----------------------
    }
}