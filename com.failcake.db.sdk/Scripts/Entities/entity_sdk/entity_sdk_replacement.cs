#region

using SaintsField;
using UnityEngine;

#endregion

namespace HyenaQuest
{
    public class entity_sdk_replacement : MonoBehaviour
    {
        #if UNITY_EDITOR
        [FieldInfoBox("DO NOT EDIT, USED TO MAP TEMPLATES ON THE MAIN GAME", EMessageType.Error)]
        public Mesh preview;
        #endif

        public void Awake() {
            SDK_SETUP.PatchSDKEntity?.Invoke(this.gameObject);
        }

        #if UNITY_EDITOR
        private void OnDrawGizmos() {
            if (!this.preview) return;

            Gizmos.matrix = Matrix4x4.TRS(this.transform.position, this.transform.rotation, Vector3.one);
            Gizmos.DrawMesh(this.preview);
        }

        private string GetUniqueID() {
            return !this.preview ? "unknown" : this.preview.name;
        }

        private void OnValidate() {
            if (Application.isPlaying || !this.preview) return;
            this.name = $"SDK-{this.GetUniqueID()}";
        }
        #endif
    }
}