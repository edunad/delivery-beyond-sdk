#region

using SaintsField;
using UnityEngine;

#endregion

namespace HyenaQuest
{
    public class entity_sdk_phys_scrap : MonoBehaviour
    {
        #if UNITY_EDITOR
        [FieldInfoBox("DO NOT EDIT, USED TO MAP TEMPLATES ON THE MAIN GAME", EMessageType.Error)]
        public Mesh preview;

        public void OnDrawGizmos() {
            if (!this.preview) return;
            
            Gizmos.matrix = Matrix4x4.TRS(this.transform.position, this.transform.rotation, Vector3.one);
            Gizmos.DrawMesh(this.preview);
        }

        public void OnValidate() {
            if (Application.isPlaying || !this.preview) return;
            this.name = $"SDK-{this.preview.name}";
        }
        #endif
    }
}