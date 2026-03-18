#region

using System;
using System.Collections.Generic;
using System.Text;
using SaintsField;
using SaintsField.Playa;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.Scripting;

#endregion

namespace HyenaQuest
{
    [Preserve, Serializable]
    public struct SunSettings : INetworkSerializable, IEquatable<SunSettings>
    {
        public float intensity;
        public Vector3 angle;
        public Color color;

        public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter {
            if (serializer.IsReader)
            {
                FastBufferReader reader = serializer.GetFastBufferReader();
                reader.ReadValueSafe(out this.intensity);
                reader.ReadValueSafe(out this.angle);
                reader.ReadValueSafe(out this.color);
            }
            else
            {
                FastBufferWriter writer = serializer.GetFastBufferWriter();
                writer.WriteValueSafe(this.intensity);
                writer.WriteValueSafe(this.angle);
                writer.WriteValueSafe(this.color);
            }
        }

        public bool Equals(SunSettings other) {
            return Mathf.Approximately(this.intensity, other.intensity) && this.angle == other.angle && this.color == other.color;
        }

        public static bool operator ==(SunSettings left, SunSettings right) {
            return left.Equals(right);
        }

        public static bool operator !=(SunSettings left, SunSettings right) {
            return !left.Equals(right);
        }

        public override bool Equals(object obj) {
            if (obj is SunSettings other) return this.Equals(other);
            return false;
        }

        public override int GetHashCode() {
            return HashCode.Combine(this.intensity, this.angle, this.color);
        }
    }

    [Preserve, Serializable]
    public struct EntryRenderSettings : INetworkSerializable, IEquatable<EntryRenderSettings>
    {
        public bool exterior;
        public Vector3 doorOpenAngle;
        public Vector3 shipOffset;
        public SunSettings sun;

        public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter {
            if (serializer.IsReader)
            {
                FastBufferReader reader = serializer.GetFastBufferReader();

                reader.ReadValueSafe(out this.exterior);
                reader.ReadValueSafe(out this.doorOpenAngle);
                reader.ReadValueSafe(out this.shipOffset);
                reader.ReadValueSafe(out this.sun);
            }
            else
            {
                FastBufferWriter writer = serializer.GetFastBufferWriter();

                writer.WriteValueSafe(this.exterior);
                writer.WriteValueSafe(this.doorOpenAngle);
                writer.WriteValueSafe(this.shipOffset);
                writer.WriteValueSafe(this.sun);
            }
        }

        public bool Equals(EntryRenderSettings other) {
            return this.doorOpenAngle == other.doorOpenAngle && this.shipOffset == other.shipOffset && this.exterior == other.exterior && this.sun == other.sun;
        }

        public static bool operator ==(EntryRenderSettings left, EntryRenderSettings right) {
            return left.Equals(right);
        }

        public static bool operator !=(EntryRenderSettings left, EntryRenderSettings right) {
            return !left.Equals(right);
        }

        public override bool Equals(object obj) {
            if (obj is EntryRenderSettings other) return this.Equals(other);
            return false;
        }

        public override int GetHashCode() {
            return HashCode.Combine(this.doorOpenAngle, this.shipOffset, this.exterior, this.sun);
        }
    }

    [Preserve, Serializable]
    public struct EntrySettings
    {
        [Required]
        public GameObject template;

        public EntryRenderSettings settings;
    }

    [Preserve, Serializable]
    public struct MonsterSpawn
    {
        [Range(0, 1)]
        public float chance;

        [Required]
        public List<GameObject> variants;
    }

    [Preserve, Serializable]
    public struct FogSettings
    {
        public Color color;

        [Range(0, 1)]
        public float density;
    }
    
    [Serializable, Flags]
    public enum ContractModifiers
    {
        NONE = 1,

        // GENERATION (NO NEED TO SUPPORT) ---
        LOCKED_DOORS = 1 << 1,
        DELIVERY_MALFUNCTION = 1 << 2,
        // ----------------

        // CURSES (WORLD NEEDS TO SUPPORT THEM) ----

        ICE_WORLD = 1 << 10,

        TOXIC_GAS_WORLD = 1 << 11,
        DARKNESS_WORLD = 1 << 12
    }

    [Preserve, Serializable, CreateAssetMenu(menuName = "HyenaQuest/World Settings")]
    public class WorldSettings : ScriptableObject
    {
        [Required]
        public string uniqueName;

        [LayoutStart("Settings", ELayout.Background | ELayout.TitleOut), Range(1, byte.MaxValue)]
        public int minRounds = 1;

        [Range(0, 1)]
        public float weight = 1;

        [LayoutStart("Settings/Checks", ELayout.Background | ELayout.TitleOut)]
        public bool collisionChecks;

        public bool duplicateChecks;

        [LayoutStart("Settings/Support", ELayout.Background | ELayout.TitleOut)]
        public bool interiorMirroring;

        public bool exitShuffle;

        public bool visCleanup;

        [EnumFlags]
        public ContractModifiers modifiers;

        [LayoutStart("Settings/Limit", ELayout.Background | ELayout.TitleOut), InfoBox("$" + nameof(WorldSettings.__CALCULATION__))]
        public AnimationCurve difficultyCurve = new AnimationCurve(
            new Keyframe(1, 1.0f),
            new Keyframe(2, 1.8f),
            new Keyframe(3, 2.2f),
            new Keyframe(4, 2.5f),
            new Keyframe(5, 2.8f),
            new Keyframe(6, 3.0f),
            new Keyframe(7, 4.0f)
        );

        [Range(0.1f, 1.0f), Tooltip("Monster to room ratio (0.4 = 4 monsters per 10 rooms)")]
        public float monsterDensity = 0.4f;

        [Range(0, 20)]
        public float baseRooms = 9;

        [Range(0, 20)]
        public int minInteriorRooms = 3;

        [LayoutStart("Settings/Limit/Biome", ELayout.Background | ELayout.TitleOut)]
        public SaintsDictionary<string, int> biomeLimit;

        [LayoutStart("Settings/Limit/Template", ELayout.Background | ELayout.TitleOut)]
        public SaintsDictionary<string, int> templateLimit;

        [LayoutStart("Settings/Monsters", ELayout.Background | ELayout.TitleOut), ArraySize(min: 1)]
        public List<MonsterSpawn> monsters;

        [LayoutStart("Settings/Ambient", ELayout.Background | ELayout.TitleOut)]
        public FogSettings fog;

        public float musicVolume = 0.1F;
        public List<AudioClip> heistMusic;

        public Material skyMaterial;

        [LayoutStart("Templates", ELayout.Background | ELayout.TitleOut), NoLabel]
        public List<EntrySettings> entry;

        [NoLabel]
        public List<GameObject> closers;

        [NoLabel]
        public List<GameObject> interiorClosers;

        [NoLabel]
        public List<GameObject> interiors;

        [NoLabel]
        public List<GameObject> rooms;

        [NoLabel]
        public List<GameObject> traversal;

        [NoLabel]
        public List<GameObject> deadEnds;

        #region PRIVATE

        private string __CALCULATION__ {
            get {
                StringBuilder samples = new StringBuilder();
                samples.AppendLine("---------------- WORLD SIZE ----------------\n");

                int lineCount = 0;
                for (byte i = 1; i < 11; i++)
                {
                    samples.Append($"Round {i} -> {this.CalculateMapSize(i)} || ");

                    lineCount++;
                    if (lineCount % 5 == 0) samples.AppendLine("");
                }

                samples.AppendLine("\n---------------- MAX MONSTERS ----------------\n");

                for (byte i = 1; i < 11; i++)
                {
                    samples.Append($"Round {i} -> {this.CalculateMaxMonsters(i)} || ");

                    lineCount++;
                    if (lineCount % 5 == 0) samples.AppendLine("");
                }

                samples.AppendLine("\n");
                return samples.ToString();
            }
        }

        #endregion

        public int CalculateMapSize(byte currentRound) {
            float roundMultiplier;

            int keyCount = this.difficultyCurve.length;
            if (keyCount >= 2)
            {
                Keyframe lastKey = this.difficultyCurve[keyCount - 1];
                Keyframe secondLastKey = this.difficultyCurve[keyCount - 2];

                if (currentRound > lastKey.time)
                {
                    float slope = (lastKey.value - secondLastKey.value) / (lastKey.time - secondLastKey.time);
                    roundMultiplier = lastKey.value + slope * (currentRound - lastKey.time);
                }
                else
                    roundMultiplier = this.difficultyCurve.Evaluate(currentRound);
            }
            else
                roundMultiplier = this.difficultyCurve.Evaluate(currentRound);

            float difficultyMultiplier = 1.0f;

            int finalSize = Mathf.RoundToInt(this.baseRooms * roundMultiplier * difficultyMultiplier);
            return Mathf.Clamp(finalSize, 3, 100);
        }

        public int CalculateMaxMonsters(byte currentRound) {
            int roomCount = this.CalculateMapSize(currentRound);

            float baseMonsters = roomCount * this.monsterDensity;
            float difficultyMultiplier = 1.0f;

            int finalMonsters = Mathf.RoundToInt(baseMonsters * difficultyMultiplier);
            return Mathf.Clamp(finalMonsters, 3, 20);
        }
    }
}