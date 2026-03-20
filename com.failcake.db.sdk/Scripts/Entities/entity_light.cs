#region

using FailCake;
using SaintsField.Playa;
using UnityEngine;

#endregion

namespace HyenaQuest
{
    [RequireComponent(typeof(Light)), DisallowMultipleComponent]
    public class entity_light : MonoBehaviour
    {
        #region PUBLIC

        [LayoutStart("Settings", ELayout.Background | ELayout.TitleOut)]
        public PowerGrid area = PowerGrid.UNCONTROLLED;

        public bool on;
        public bool breakable;

        #endregion

        #region PRIVATE

        private Light _light;
        private entity_led_material _led;

        private float _intensity;
        private bool _broken;
        private Color _color;

        #region TIMERS

        private util_timer _flickerTimer;

        #endregion

        #endregion

        public void Awake() {
            this._light = this.GetComponent<Light>();
            if (!this._light) throw new UnityException("Light component not found on entity_light");

            this._led = this.GetComponent<entity_led_material>();
            this._intensity = this._light.intensity;
            this._color = this._light.color;

            // EVENTS ---
            CoreController.WaitFor<LightController>(lightCtrl => { lightCtrl.OnLightAreaCommand += this.OnLightAreaCommand; });
            // ----------
        }

        public void OnDestroy() {
            this._flickerTimer?.Stop();

            // EVENTS ---
            if (LightController.Instance) LightController.Instance.OnLightAreaCommand -= this.OnLightAreaCommand;
            // ----------
        }

        public bool IsOn() {
            return this.on;
        }

        public bool IsSpotlight() {
            return this._light?.type == LightType.Spot;
        }

        public float GetSpotlightAngle() {
            return this._light?.spotAngle ?? 0;
        }

        public float GetRange() {
            return this._light?.range ?? 0;
        }

        [Client]
        public void SetIntensity(float intensity) {
            if (!this._light) return;

            this._light.intensity = intensity;
            this._intensity = intensity;
        }

        [Client]
        public float GetIntensity() {
            if (!this._light) return 0;
            return this._light.intensity;
        }

        [Client]
        public void SetColor(Color? color) {
            if (!this._light) return;
            this._light.color = color ?? this._color;
        }

        [Client]
        public void Break() {
            if (!this.breakable || this._broken) return;
            SDK_SETUP.Play3DSound?.Invoke("General/Entities/Light/light_break.ogg", this.transform.position, new AudioData { distance = 3, volume = 0.8F }, false);

            this._light.intensity = 0;
            this._broken = true;
        }

        [Client]
        public void Restore() {
            if (!this.breakable || !this._broken) return;

            this._light.intensity = this._intensity;
            this._broken = false;
        }

        [Client]
        public void Flicker(bool stayOn = true) {
            this._flickerTimer?.Stop();
            this._flickerTimer = util_timer.Create(Random.Range(2, 8), 0.06F, ticks => {
                this._light.intensity = Random.Range(0.25F, this._intensity);
            }, () => {
                this._light.intensity = this._intensity;
                if (!stayOn) this.SetLightStatus(false);
            });
        }

        #region PRIVATE

        [Client]
        public void SetLightStatus(bool enable, bool skipAudio = false) {
            if (enable == this.on) return;
            this.on = enable;

            if (!skipAudio)
                SDK_SETUP.Play3DSound?.Invoke(enable ? "General/Entities/Light/light_on.ogg" : "General/Entities/Light/light_off.ogg", this.transform.position, new AudioData { pitch = Random.Range(0.8f, 1.2f), distance = 2, volume = 0.15F }, false);
        }

        private void Update() {
            if (!this._light || this._light.enabled == this.on) return;
            if (this._led) this._led.SetActive(this.on);
        }

        [Client]
        private void OnLightAreaCommand(PowerGrid lightArea, LightCommand command, bool server) {
            if (lightArea == PowerGrid.UNCONTROLLED) return;
            if (lightArea != this.area) return;

            switch (command)
            {
                case LightCommand.OFF:
                    this.SetLightStatus(false);
                    break;
                case LightCommand.ON:
                    this.SetLightStatus(true);
                    break;
                case LightCommand.FLICKER:
                    this.Flicker();
                    break;
                case LightCommand.FLICKER_OFF:
                    this.Flicker(false);
                    break;
            }
        }

        #if UNITY_EDITOR
        private void OnValidate() {
            if (Application.isPlaying) return;

            if (!this._light) this._light = this.GetComponent<Light>();
            if (!this._light) return;

            this._light.enabled = this.on;
        }
        #endif

        #endregion
    }
}