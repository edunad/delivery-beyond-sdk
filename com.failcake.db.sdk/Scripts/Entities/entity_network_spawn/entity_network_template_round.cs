#region

using SaintsField.Playa;
using UnityEditor;
using UnityEngine;

#endregion

namespace HyenaQuest
{
    public class entity_network_template_round : entity_network_template_chance
    {
        [LayoutStart("Settings", ELayout.Background | ELayout.TitleOut), Range(1, byte.MaxValue)]
        public byte minRounds;
        
        public override bool CanSpawn() {
            byte currentRound = SDK_SETUP.GetCurrentRound?.Invoke() ?? 1;
            if (currentRound < this.minRounds) return false;

            return Random.Range(0f, 1f) < this.chance;
        }
        
        #region PRIVATE

        #if UNITY_EDITOR
        public new void OnDrawGizmos() {
            base.OnDrawGizmos();

            Handles.color = Color.white;
            Handles.Label(this.transform.position, this.minRounds.ToString());
        }
        #endif

        #endregion
    }
}