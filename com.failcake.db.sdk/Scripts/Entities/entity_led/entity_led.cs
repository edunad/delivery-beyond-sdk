#region

using FailCake;
using SaintsField;
using SaintsField.Playa;
using UnityEngine;

#endregion

namespace HyenaQuest
{
    public class entity_led : MonoBehaviour
    {
        [LayoutStart("Colors", ELayout.Background | ELayout.TitleOut), ColorUsage(true, true)]
        public Color activeColor;

        [ColorUsage(true, true)]
        public Color disabledColor = Color.black;

        [LayoutEnd("Colors"), LayoutStart("Settings", ELayout.Background | ELayout.TitleOut), SerializeField]
        public bool active;

        [Required]
        public Renderer mesh;

        [Range(0, 100)]
        public byte materialIndex;

        [LayoutEnd("Settings"), LayoutStart("Blink", ELayout.Background | ELayout.TitleOut)]
        public float blink;

        public float blinkDelay;

        [LayoutEnd("Blink"), LayoutStart("Sounds", ELayout.Background | ELayout.TitleOut)]
        public float maxDistance = 2f;

        public AudioClip enableSnd;
        public AudioClip disableSnd;

        #region PRIVATE

        private static readonly int ShaderColor = Shader.PropertyToID("_BaseColor");

        private util_timer _timer;
        private util_timer _delayTimer;

        private bool _blinkState = true;

        #endregion

        public void Awake() {
            if (!this.mesh) this.mesh = this.GetComponent<Renderer>();
            if (!this.mesh) throw new UnityException("entity_led requires a Renderer component to work.");

            if (this.active && this.blink > 0) this.StartBlinking();
            this.UpdateMaterial();
        }

        public void OnDestroy() {
            this.StopBlinking();
        }

        public void SetActive(bool enable, bool sound = false) {
            if (this.active == enable) return;
            this.active = enable;

            switch (this.active)
            {
                case true when this.blink > 0:
                    this.StartBlinking();
                    break;
                case false:
                    this.StopBlinking();
                    break;
            }

            this.UpdateMaterial();
            if (sound)
                SDK.Play3DSoundClip(this.active ? this.enableSnd : this.disableSnd, this.transform.position,
                    new AudioData { distance = this.maxDistance }, false);
        }

        public void SetActiveColor(Color color) {
            this.activeColor = color;
            this.UpdateMaterial();
        }

        public void SetDisabledColor(Color color) {
            this.disabledColor = color;
            this.UpdateMaterial();
        }

        #region PRIVATE

        private void StartBlinking() {
            this._blinkState = true;

            this._delayTimer?.Stop();
            this._delayTimer = util_timer.Simple(this.blinkDelay, () => {
                this._timer?.Stop();
                this._timer = util_timer.Create(-1, this.blink, i => {
                    this._blinkState = !this._blinkState;
                    this.UpdateMaterial();
                });
            });
        }

        private void StopBlinking() {
            this._timer?.Stop();
            this._delayTimer?.Stop();
            this._blinkState = true;
        }

        private void UpdateMaterial() {
            if (!this.mesh) return;

            bool visible = this.active && this._blinkState;
            this.mesh.materials[this.materialIndex].SetColor(entity_led.ShaderColor, visible ? this.activeColor : this.disabledColor);
        }

        #endregion
    }
}