using System;
using Unity.Netcode;
using UnityEngine;

namespace HyenaQuest
{
    [Serializable]
    public enum SoundMixer
    {
        MUSIC,
        SFX,
        CURSES,
        MICROPHONE,
        MASTER
    }
    
    [Flags, Serializable]
    public enum SoundFlags
    {
        NONE = 1,

        REFLECTION = 1 << 0,
        AIR_RESISTANT = 1 << 1,
        OCCLUSION = 1 << 2
    }

    [Serializable]
    public class AudioData : INetworkSerializable
    {
        public float pitch = 1;
        public float volume = 1;
        public float distance = 5;

        public SoundFlags flags;

        public NetworkBehaviourReference parent;
        public SoundMixer mixer = SoundMixer.SFX;

        public override bool Equals(object obj) {
            if (!(obj is AudioData)) return false;

            AudioData other = (AudioData)obj;
            return Mathf.Approximately(this.pitch, other.pitch) && Mathf.Approximately(this.volume, other.volume) && Mathf.Approximately(this.distance, other.distance) &&
                   this.flags == other.flags && this.mixer == other.mixer && this.parent.Equals(other.parent);
        }

        public override int GetHashCode() {
            return HashCode.Combine(this.pitch, this.volume, this.distance, this.flags, this.parent, (int)this.mixer);
        }

        public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter {
            if (serializer.IsReader)
            {
                FastBufferReader reader = serializer.GetFastBufferReader();

                reader.ReadValueSafe(out this.pitch);
                reader.ReadValueSafe(out this.volume);
                reader.ReadValueSafe(out this.distance);

                reader.ReadValueSafe(out this.flags);

                reader.ReadValueSafe(out this.mixer);
                reader.ReadValueSafe(out this.parent);
            }
            else
            {
                FastBufferWriter writer = serializer.GetFastBufferWriter();

                writer.WriteValueSafe(this.pitch);
                writer.WriteValueSafe(this.volume);
                writer.WriteValueSafe(this.distance);

                writer.WriteValueSafe(this.flags);

                writer.WriteValueSafe(this.mixer);
                writer.WriteValueSafe(this.parent);
            }
        }

        public static bool operator ==(AudioData a, AudioData b) {
            if (ReferenceEquals(a, b)) return true;
            if (a is null || b is null) return false;
            return a.Equals(b);
        }

        public static bool operator !=(AudioData a, AudioData b) {
            return !(a == b);
        }
    }

}