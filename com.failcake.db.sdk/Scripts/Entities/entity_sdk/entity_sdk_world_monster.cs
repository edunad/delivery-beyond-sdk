using SaintsField;

namespace HyenaQuest
{
    public class entity_sdk_world_monster: entity_sdk_replacement
    {
        [InfoBox("DO NOT PLACE ON THE WorldSettings MONSTER LIST!", EMessageType.Warning)]
        public void Awake() { } // Do not register
    }
}