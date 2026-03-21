#region

using FailCake;
using SaintsField;
using UnityEngine;

#endregion

namespace HyenaQuest
{
    public class entity_ambient_sound : MonoBehaviour
    {
        #region PUBLIC

        [Header("Settings"), Required]
        public GameObject speaker;

        #endregion

        #region PRIVATE

        private AudioSource _speaker;
        private Collider[] _areas;

        private util_fade_timer _transition;
        private float _volume;

        #endregion

        public void Awake() {
            if (!this.speaker) throw new UnityException("entity_ambient_sound requires GameObject speaker component");
            this.speaker.isStatic = false;

            this._speaker = this.speaker.GetComponent<AudioSource>();
            if (!this._speaker) throw new UnityException("entity_ambient_sound requires AudioSource component");

            this._speaker.loop = true;
            this._volume = this._speaker.volume;

            this._areas = this.GetComponentsInChildren<Collider>(true);
            if (this._areas == null || this._areas.Length == 0) throw new UnityException("entity_ambient_sound requires Collider component");
        }

        public void OnDestroy() {
            this.Stop();
        }

        public Collider[] GetArea() { return this._areas; }

        public void Update() {
            if (!SDK.MainCamera || !this._speaker || !this._speaker.isPlaying) return;

            Vector3 centerPosition = SDK.MainCamera.transform.position;
            Vector3 bestPoint = Vector3.zero;
            float closestSqrDistance = Mathf.Infinity;

            foreach (Collider col in this._areas)
            {
                Vector3 pos = col is MeshCollider
                    ? Physics.ClosestPoint(centerPosition, col, col.transform.position, col.transform.rotation)
                    : col.ClosestPointOnBounds(centerPosition);

                float sqrDist = (pos - centerPosition).sqrMagnitude;

                if (sqrDist < closestSqrDistance)
                {
                    closestSqrDistance = sqrDist;
                    bestPoint = pos;
                }
            }

            this.speaker.transform.position = bestPoint;
        }

        public void Play() {
            if (!this._speaker || !this._speaker.enabled) return;
            this._speaker.Play();
        }

        public void SetClip(AudioClip clip) {
            if (!this._speaker) return;
            this._speaker.clip = clip;
        }

        public void TransitionSound(AudioClip clip) {
            if (!this._speaker || this._speaker.clip == clip) return;

            if (!this._speaker.isPlaying)
            {
                // Not playing, no need to transition
                this._speaker.volume = this._volume;

                this.SetClip(clip);
                this.Play();
                return;
            }

            if (this._transition != null) this._transition.Stop();
            this._transition = util_fade_timer.Fade(0.5f, this._speaker.volume, 0, f => this._speaker.volume = f, _ => {
                this.SetClip(clip);
                this.Play();

                this._transition = util_fade_timer.Fade(0.5f, 0, this._volume, f => this._speaker.volume = f);
            });
        }

        public void Stop() {
            if (!this._speaker) return;
            if (this._transition != null) this._transition.Stop();
            this._speaker.Stop();
        }
    }
}