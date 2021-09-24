using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace AudioEngine
{
    public class AudioBlender : MonoBehaviour
    {
        [SerializeField] AudioClip A;

        [SerializeField] AudioClip B;

        [SerializeField, Range(0, 1)] float Lerp;

        [SerializeField, Range(0, 2)] float AVolume = 1f;

        [SerializeField, Range(0, 2)] float BVolume = 1f;

        ClipData m_clipA, m_clipB;

        bool m_used = true;

        private void Start()
        {
            m_clipA = new ClipData(A);
            m_clipB = new ClipData(B);
        }

        private void Update()
        {
            if (m_used)
            {
                float clipLength = Mathf.Lerp(m_clipA.Length, m_clipB.Length, Lerp);

                float aPitch = m_clipA.Length / clipLength;
                float bPitch = m_clipB.Length / clipLength;

                m_clipA.SetPitch(aPitch);
                m_clipB.SetPitch(bPitch);

                m_clipA.ReadBuffer();
                m_clipB.ReadBuffer();

                m_used = false;
            }
        }



        private void OnAudioFilterRead(float[] data, int channels)
        {
            if (!m_used)
            {
                for (int i = 0; i < data.Length; i++)
                {
                    float aValue = m_clipA.SampleIndex(i) * AVolume;
                    float bValue = m_clipB.SampleIndex(i) * BVolume;

                    data[i] = Mathf.Lerp(aValue, bValue, Lerp);
                }

                m_clipA.NextOffset();
                m_clipB.NextOffset();
                m_used = true;
            }
        }
    }

    struct ClipData
    {
        const int BUFFER_SIZE = 2048;

        int m_offset;

        int m_frequency;

        int m_samples;

        int m_channelCount;

        float m_rate;

        float m_pitch;

        float[] m_buffer;

        AudioClip m_clip;

        public int CurrentOffset => m_offset;

        public float[] Buffer => m_buffer;

        public float Length => m_clip.length;

        public void SetPitch(float pitch)
        {
            m_pitch = pitch;
        }

        public ClipData(AudioClip clip)
        {
            m_clip = clip;
            m_pitch = 1f;
            m_offset = 0;
            m_frequency = clip.frequency;
            m_channelCount = clip.channels;
            m_samples = clip.samples;
            m_buffer = new float[BUFFER_SIZE * 5];

            m_rate = (m_channelCount / 2f) * (m_frequency / (float)AudioSettings.outputSampleRate);
        }

        public void ReadBuffer()
        {
            m_clip.GetData(m_buffer, m_offset);
        }

        public float SampleIndex(int index)
        {
            int i = Mathf.FloorToInt(index * m_rate * m_pitch);
            return m_buffer[Mathf.Min(m_buffer.Length - 1, i)];
        }

        public void NextOffset()
        {
            m_offset += Mathf.FloorToInt(BUFFER_SIZE * (m_rate * m_pitch / m_channelCount));

            if (m_offset >= m_samples)
                m_offset = 0;
        }

        public void SetTime(float time)
        {
            m_offset = Mathf.FloorToInt(m_frequency * time);
        }
    }
}