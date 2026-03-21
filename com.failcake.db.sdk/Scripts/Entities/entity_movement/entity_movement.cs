#region

using System;
using System.Collections.Generic;
using SaintsField;
using SaintsField.Playa;
using UnityEditor;
using UnityEngine;
using Random = UnityEngine.Random;

#endregion

namespace HyenaQuest
{
    [Serializable]
    public struct Point : IEquatable<Point>
    {
        public Vector3 pos;
        public bool smoothPos;

        public Vector3 angle;
        public bool smoothAngle;

        [MinMaxSlider(0, 100)]
        public Vector2 delay;

        [Range(0, 2f)]
        public float speedModifier;

        public bool Equals(Point other) {
            return this.pos.Equals(other.pos) && this.smoothPos == other.smoothPos && this.angle.Equals(other.angle) && this.smoothAngle == other.smoothAngle && this.delay.Equals(other.delay) && this.speedModifier.Equals(other.speedModifier);
        }

        public override bool Equals(object obj) {
            return obj is Point other && this.Equals(other);
        }

        public static bool operator ==(Point a, Point b) {
            return a.Equals(b);
        }

        public static bool operator !=(Point a, Point b) {
            return !(a == b);
        }

        public override int GetHashCode() {
            return HashCode.Combine(this.pos, this.smoothPos, this.angle, this.smoothAngle, this.delay, this.speedModifier);
        }
    }

    public class entity_movement : MonoBehaviour
    {
        [LayoutStart("Target", ELayout.Background | ELayout.TitleOut), Required]
        public GameObject obj;

        [LayoutStart("Settings", ELayout.Background | ELayout.TitleOut)]
        public List<Point> points = new List<Point>();

        public float speed = 1f;

        public bool autoFacePoints;

        public bool startActive;
        public bool reverse;
        public bool loop;
        public bool catmullSmooth;

        [LayoutStart("Sound", ELayout.Background | ELayout.TitleOut)]
        public AudioClip stopSound;

        public AudioClip startSound;
        public AudioClip pathSound;

        #region PRIVATE

        private bool _isActive;

        private int _pointIndex;
        private int _targetPointIndex;

        private float _movementStartTime;
        private float _delayEndTime;

        private bool _isDelaying;
        private Action _onCompleteCallback;

        private Vector3 CatmullRom(Vector3 p0, Vector3 p1, Vector3 p2, Vector3 p3, float t) {
            float t2 = t * t;
            float t3 = t2 * t;

            return 0.5f * (
                2f * p1 +
                (-p0 + p2) * t +
                (2f * p0 - 5f * p1 + 4f * p2 - p3) * t2 +
                (-p0 + 3f * p1 - 3f * p2 + p3) * t3
            );
        }

        private Vector3 CatmullRomDerivative(Vector3 p0, Vector3 p1, Vector3 p2, Vector3 p3, float t) {
            float t2 = t * t;

            return 0.5f * (
                -p0 + p2 +
                (2f * p0 - 5f * p1 + 4f * p2 - p3) * (2f * t) +
                (-p0 + 3f * p1 - 3f * p2 + p3) * (3f * t2)
            );
        }

        private Vector3 ApplyAutoFace(Vector3 direction, Vector3 angleOffset) {
            if (direction.sqrMagnitude < 0.0001f) return angleOffset;

            Quaternion lookRot = Quaternion.LookRotation(direction);
            Quaternion offsetRot = Quaternion.Euler(angleOffset);
            return (lookRot * offsetRot).eulerAngles;
        }

        private void GetCatmullRomIndices(out int p0Index, out int p1Index, out int p2Index, out int p3Index) {
            p1Index = this._pointIndex;
            p2Index = this._targetPointIndex;

            if (this.reverse)
            {
                p0Index = p1Index + 1;
                p3Index = p2Index - 1;
            }
            else
            {
                p0Index = p1Index - 1;
                p3Index = p2Index + 1;
            }

            if (this.loop)
            {
                if (p0Index < 0) p0Index = this.points.Count + p0Index;
                if (p0Index >= this.points.Count) p0Index = p0Index - this.points.Count;
                if (p3Index < 0) p3Index = this.points.Count + p3Index;
                if (p3Index >= this.points.Count) p3Index = p3Index - this.points.Count;
            }
            else
            {
                p0Index = Mathf.Clamp(p0Index, 0, this.points.Count - 1);
                p3Index = Mathf.Clamp(p3Index, 0, this.points.Count - 1);
            }
        }

        private float CalculateCurveLength(Vector3 p0, Vector3 p1, Vector3 p2, Vector3 p3, int segments = 20) {
            float length = 0f;
            Vector3 prevPoint = p1;

            for (int i = 1; i <= segments; i++)
            {
                float t = (float)i / segments;
                Vector3 currentPoint = this.CatmullRom(p0, p1, p2, p3, t);
                length += Vector3.Distance(prevPoint, currentPoint);
                prevPoint = currentPoint;
            }

            return length;
        }

        private float CalculateT(Vector3 p0, Vector3 p1, Vector3 p2, Vector3 p3, float targetFraction, int segments = 20) {
            if (targetFraction <= 0f) return 0f;
            if (targetFraction >= 1f) return 1f;

            float totalLength = 0f;
            float[] segmentLengths = new float[segments];

            Vector3 prevPoint = p1;
            for (int i = 1; i <= segments; i++)
            {
                float t = (float)i / segments;
                Vector3 currentPoint = this.CatmullRom(p0, p1, p2, p3, t);

                float segmentLength = Vector3.Distance(prevPoint, currentPoint);
                segmentLengths[i - 1] = segmentLength;

                totalLength += segmentLength;
                prevPoint = currentPoint;
            }

            float targetDistance = totalLength * targetFraction;
            float accumulatedDistance = 0f;

            for (int i = 0; i < segments; i++)
            {
                float nextDistance = accumulatedDistance + segmentLengths[i];

                if (nextDistance >= targetDistance)
                {
                    float segmentFraction = segmentLengths[i] > 0
                        ? (targetDistance - accumulatedDistance) / segmentLengths[i]
                        : 0f;

                    float tStart = (float)i / segments;
                    float tEnd = (float)(i + 1) / segments;
                    return Mathf.Lerp(tStart, tEnd, segmentFraction);
                }

                accumulatedDistance = nextDistance;
            }

            return 1f;
        }

        #endregion

        public void Awake() {
            if (!this.obj) throw new UnityException("Missing game object");
            if (this.points.Count < 2) throw new UnityException("At least 2 points are needed");

            if (this.startActive) this.StartMovement();
        }

        public void Update() {
            if (!this._isActive) return;

            if (this._isDelaying)
            {
                if (!(Time.time >= this._delayEndTime)) return;

                this._isDelaying = false;
                this._movementStartTime = Time.time;

                if (this._pointIndex == (this.reverse ? this.points.Count - 1 : 0))
                    SDK.Play3DSoundClip?.Invoke(this.startSound, this.obj.transform.position,
                        new AudioData { distance = 4, pitch = Random.Range(0.85f, 1.15f), volume = 0.5F }, this.ShouldBroadcastSound());

                return;
            }

            Point current = this.points[this._pointIndex];
            Point dest = this.points[this._targetPointIndex];

            float positionDistance;
            if (this.catmullSmooth && dest.smoothPos)
            {
                this.GetCatmullRomIndices(out int p0Index, out int p1Index, out int p2Index, out int p3Index);
                positionDistance = this.CalculateCurveLength(
                    this.points[p0Index].pos,
                    this.points[p1Index].pos,
                    this.points[p2Index].pos,
                    this.points[p3Index].pos
                );
            }
            else
                positionDistance = Vector3.Distance(current.pos, dest.pos);

            float rotationDistance = Quaternion.Angle(Quaternion.Euler(current.angle), Quaternion.Euler(dest.angle));
            float effectiveSpeed = this.speed * (dest.speedModifier > 0 ? dest.speedModifier : 1f);

            float journeyLength;
            if (positionDistance < 0.001f)
                journeyLength = rotationDistance / (effectiveSpeed * 90f);
            else
                journeyLength = positionDistance / effectiveSpeed;

            if (journeyLength <= 0)
            {
                this._pointIndex = this._targetPointIndex;
                this.MoveToNextPoint();
                return;
            }

            float fracJourney = (Time.time - this._movementStartTime) / journeyLength;
            if (fracJourney >= 1.0f || dest is { smoothPos: false, smoothAngle: false })
            {
                this._pointIndex = this._targetPointIndex;
                this.OnPointReached(dest);
                SDK.Play3DSoundClip?.Invoke(this.pathSound, this.obj.transform.position,
                    new AudioData { distance = 4, pitch = Random.Range(0.85f, 1.15f), volume = 0.5F }, this.ShouldBroadcastSound());

                this.MoveToNextPoint();
            }
            else
            {
                Vector3 newPos;
                Vector3 moveDirection = Vector3.zero;

                if (this.catmullSmooth && dest.smoothPos)
                {
                    this.GetCatmullRomIndices(out int p0Index, out int p1Index, out int p2Index, out int p3Index);

                    Vector3 p0 = this.points[p0Index].pos;
                    Vector3 p1 = this.points[p1Index].pos;
                    Vector3 p2 = this.points[p2Index].pos;
                    Vector3 p3 = this.points[p3Index].pos;

                    float t = this.CalculateT(p0, p1, p2, p3, fracJourney);
                    newPos = this.CatmullRom(p0, p1, p2, p3, t);

                    if (this.autoFacePoints) moveDirection = this.CatmullRomDerivative(p0, p1, p2, p3, t);
                }
                else
                {
                    newPos = !dest.smoothPos ? dest.pos : Vector3.Lerp(current.pos, dest.pos, fracJourney);

                    if (this.autoFacePoints) moveDirection = dest.pos - current.pos;
                }

                this.SetPosition(newPos);

                Vector3 angleValue = !dest.smoothAngle ? dest.angle : Vector3.Lerp(current.angle, dest.angle, fracJourney);
                if (this.autoFacePoints)
                    this.SetAngle(this.ApplyAutoFace(moveDirection, angleValue));
                else
                    this.SetAngle(angleValue);
            }
        }

        public virtual void StartMovement(bool reset = true, Action onComplete = null) {
            if (this.points.Count < 2) throw new UnityException("At least 2 points are needed");
            if (reset) this.ResetMovement();

            this._onCompleteCallback = onComplete;
            this._targetPointIndex = this.reverse ? this._pointIndex > 0 ? this._pointIndex - 1 : this.loop ? this.points.Count - 1 : 0 :
                this._pointIndex < this.points.Count - 1 ? this._pointIndex + 1 :
                this.loop ? 0 : this.points.Count - 1;

            this._movementStartTime = Time.time;
            this._isActive = true;

            SDK.Play3DSoundClip?.Invoke(this.startSound, this.obj.transform.position, new AudioData { distance = 4, pitch = Random.Range(0.85f, 1.15f), volume = 0.5F }, this.ShouldBroadcastSound());
        }

        public Point GetPoint(int index) {
            if (this.points == null || this.points.Count < 2 || index > this.points.Count) return default(Point);
            return this.points[index];
        }


        public virtual void StopMovement() {
            this._isActive = false;
            this._isDelaying = false;
        }

        #region PRIVATE

        protected virtual bool ShouldBroadcastSound() { return false; }

        protected virtual void ResetMovement() {
            if (this.points == null || this.points.Count < 2) return;
            this._pointIndex = this.reverse ? this.points.Count - 1 : 0;

            this._isDelaying = false;
            this._isActive = false;

            Point point = this.points[this._pointIndex];
            this.ForcePosition(point);
        }

        protected virtual void OnPointReached(Point dest) { }

        protected virtual void ForcePosition(Point point) {
            this.SetPosition(point.pos);

            if (this.autoFacePoints)
            {
                int nextIdx = this._pointIndex + (this.reverse ? -1 : 1);

                if (this.loop)
                {
                    if (nextIdx < 0)
                        nextIdx = this.points.Count - 1;
                    else if (nextIdx >= this.points.Count) nextIdx = 0;
                }
                else
                    nextIdx = Mathf.Clamp(nextIdx, 0, this.points.Count - 1);

                Vector3 direction = this.points[nextIdx].pos - point.pos;
                this.SetAngle(this.ApplyAutoFace(direction, point.angle));
            }
            else
                this.SetAngle(point.angle);
        }

        private void SetPosition(Vector3 pos) {
            if (!this.obj) throw new UnityException("Missing game object");
            this.obj.transform.localPosition = pos;
        }

        private void SetAngle(Vector3 angle) {
            if (!this.obj) throw new UnityException("Missing game object");
            this.obj.transform.localEulerAngles = angle;
        }

        private void MoveToNextPoint() {
            int nextIndex = this._pointIndex + (this.reverse ? -1 : 1);
            if (nextIndex < 0 || nextIndex >= this.points.Count)
            {
                SDK.Play3DSoundClip?.Invoke(this.stopSound, this.obj.transform.position, new AudioData { distance = 4, pitch = Random.Range(0.85f, 1.15f), volume = 0.5F }, this.ShouldBroadcastSound());

                if (!this.loop)
                {
                    this._isActive = false;
                    this._onCompleteCallback?.Invoke();
                    return;
                }

                nextIndex = this.reverse ? this.points.Count - 1 : 0;
            }

            this._targetPointIndex = nextIndex;
            Point targetPoint = this.points[nextIndex];

            if (targetPoint.delay != Vector2.zero)
            {
                this._isDelaying = true;
                this._delayEndTime = Time.time + Random.Range(targetPoint.delay.x, targetPoint.delay.y);
            }
            else
                this._movementStartTime = Time.time;
        }

        #if UNITY_EDITOR
        private void OnDrawGizmos() {
            if (this.points == null || this.points.Count < 2) return;

            Color originalColor = Handles.color;

            for (int i = 0; i < this.points.Count; i++)
            {
                Vector3 worldPos = this.transform.TransformPoint(this.points[i].pos);
                Point point = this.points[i];

                if (point is { smoothPos: true, smoothAngle: true })
                    Handles.color = Color.green;
                else if (point.smoothPos || point.smoothAngle)
                    Handles.color = Color.yellow;
                else
                    Handles.color = Color.red;

                Handles.DrawSolidDisc(worldPos, Camera.current?.transform.forward ?? Vector3.up, 0.1f);

                if (i < this.points.Count - 1 || this.loop)
                {
                    int nextIndex = (i + 1) % this.points.Count;

                    Vector3 nextWorldPos = this.transform.TransformPoint(this.points[nextIndex].pos);
                    Point nextPoint = this.points[nextIndex];

                    Handles.color = point is { smoothPos: true, smoothAngle: true } ? new Color(0f, 1f, 0f, 0.8f) : new Color(1f, 0.5f, 0f, 0.8f);

                    if (this.catmullSmooth && nextPoint.smoothPos)
                    {
                        int p0Index = i - 1;
                        int p3Index = nextIndex + 1;

                        if (this.loop)
                        {
                            if (p0Index < 0) p0Index = this.points.Count - 1;
                            if (p3Index >= this.points.Count) p3Index = 0;
                        }
                        else
                        {
                            p0Index = Mathf.Max(0, p0Index);
                            p3Index = Mathf.Min(this.points.Count - 1, p3Index);
                        }

                        Vector3 p0 = this.transform.TransformPoint(this.points[p0Index].pos);
                        Vector3 p1 = worldPos;
                        Vector3 p2 = nextWorldPos;
                        Vector3 p3 = this.transform.TransformPoint(this.points[p3Index].pos);

                        int segments = 20;
                        Vector3 prevPoint = p1;
                        for (int seg = 1; seg <= segments; seg++)
                        {
                            float t = (float)seg / segments;
                            Vector3 curvePoint = this.CatmullRom(p0, p1, p2, p3, t);
                            Handles.DrawLine(prevPoint, curvePoint, 2f);
                            prevPoint = curvePoint;
                        }

                        Vector3 direction = (prevPoint - this.CatmullRom(p0, p1, p2, p3, 0.5f)).normalized;
                        Vector3 arrowPos = this.CatmullRom(p0, p1, p2, p3, 0.6f);

                        Handles.color = new Color(Handles.color.r, Handles.color.g, Handles.color.b, 1f);
                        if (direction != Vector3.zero) Handles.ArrowHandleCap(0, arrowPos, Quaternion.LookRotation(direction), 0.5f, EventType.Repaint);

                        if (nextPoint.smoothAngle) this.DrawRotationArc(p0, p1, p2, p3, point.angle, nextPoint.angle, true);
                    }
                    else
                    {
                        Handles.DrawLine(worldPos, nextWorldPos, 2f);

                        Vector3 direction = (nextWorldPos - worldPos).normalized;
                        Vector3 arrowPos = Vector3.Lerp(worldPos, nextWorldPos, 0.6f);

                        Handles.color = new Color(Handles.color.r, Handles.color.g, Handles.color.b, 1f);
                        if (direction != Vector3.zero) Handles.ArrowHandleCap(0, arrowPos, Quaternion.LookRotation(direction), 0.5f, EventType.Repaint);

                        if (nextPoint.smoothAngle) this.DrawRotationArc(worldPos, nextWorldPos, Vector3.zero, Vector3.zero, point.angle, nextPoint.angle, false);
                    }
                }
            }

            Handles.color = originalColor;
        }

        private void DrawRotationArc(Vector3 p0, Vector3 p1, Vector3 p2, Vector3 p3, Vector3 startAngle, Vector3 endAngle, bool useCatmullRom) {
            Quaternion startRot = Quaternion.Euler(startAngle);
            Quaternion endRot = Quaternion.Euler(endAngle);

            float angleDifference = Quaternion.Angle(startRot, endRot);
            if (angleDifference < 2f) return;

            Color originalColor = Handles.color;
            Handles.color = new Color(1f, 0f, 1f, 0.6f);

            int arcSegments = Mathf.Max(8, Mathf.RoundToInt(angleDifference / 10f));
            for (int i = 0; i < arcSegments; i++)
            {
                float t1 = (float)i / arcSegments;
                float t2 = (float)(i + 1) / arcSegments;

                Vector3 pos1, pos2;
                if (useCatmullRom)
                {
                    pos1 = this.CatmullRom(p0, p1, p2, p3, t1);
                    pos2 = this.CatmullRom(p0, p1, p2, p3, t2);
                }
                else
                {
                    pos1 = Vector3.Lerp(p0, p1, t1);
                    pos2 = Vector3.Lerp(p0, p1, t2);
                }

                Quaternion rot1 = Quaternion.Slerp(startRot, endRot, t1);
                Quaternion rot2 = Quaternion.Slerp(startRot, endRot, t2);

                Vector3 forward1 = rot1 * Vector3.forward * 0.15f;
                Vector3 forward2 = rot2 * Vector3.forward * 0.15f;

                Handles.DrawLine(pos1, pos1 + forward1, 1f);
                if (i == arcSegments - 1) Handles.DrawLine(pos2, pos2 + forward2, 1f);
            }

            Handles.color = originalColor;
        }

        protected void OnValidate() {
            if (Application.isPlaying) return;
            if (!this.obj) return;

            this.ResetMovement();
        }

        #endif

        #endregion
    }
}