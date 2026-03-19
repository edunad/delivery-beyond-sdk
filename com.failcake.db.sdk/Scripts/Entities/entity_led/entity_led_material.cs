#region

using System.Collections.Generic;
using SaintsField.Playa;
using UnityEngine;

#endregion

namespace HyenaQuest
{
    public class entity_led_material : MonoBehaviour
    {
        [LayoutStart("Settings", ELayout.Background | ELayout.TitleOut)]
        public MeshRenderer meshRenderer;

        [LayoutStart("Colors", ELayout.Background | ELayout.TitleOut)]
        public bool active;

        [ColorUsage(true, true)]
        public Color activeColor;

        [ColorUsage(true, true)]
        public Color disabledColor;

        [LayoutStart("Material", ELayout.Background | ELayout.TitleOut)]
        public int materialSlot;

        #region PRIVATE

        private static readonly int ShaderColor = Shader.PropertyToID("_BaseColor");

        #endregion

        public void Awake() {
            if (!this.meshRenderer) throw new UnityException("entity_led requires a MeshRenderer component to work.");
            if (this.meshRenderer.sharedMaterials.Length <= this.materialSlot) throw new UnityException("entity_led requires a material to work.");

            this.UpdateMaterial();
        }

        public void SetActive(bool enable) {
            if (this.active == enable) return;
            this.active = enable;

            this.UpdateMaterial();
        }

        #region PRIVATE

        private void UpdateMaterial() {
            if (!this.meshRenderer) return;

            List<Material> m = new List<Material>();
            this.meshRenderer.GetMaterials(m);
            m[this.materialSlot].SetColor(entity_led_material.ShaderColor, this.active ? this.activeColor : this.disabledColor);
            this.meshRenderer.SetMaterials(m);
        }

        #endregion
    }
}