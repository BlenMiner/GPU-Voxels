using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Mathematics;
using UnityEngine;

namespace VoxelEngine
{
    public interface IDestructible
    {
        void OnRemovedFromWorld();
    }

    public class ChunkCollection<T> where T : IDestructible
    {
        List<T> m_rawList = new List<T>();

        Dictionary<int3, T> m_dictionaryRef = new Dictionary<int3, T>();

        public Dictionary<int3, T> Dictionary => m_dictionaryRef;

        public void Add(int3 key, T value)
        {
            m_dictionaryRef.Add(key, value);
            m_rawList.Add(value);
        }

        /// <summary>
        /// Calls function on all chunks. If function returns false, it breaks the loop.
        /// </summary>
        /// <param name="func"></param>
        public void ForEach(Func<T, bool> func)
        {
            for(int i = 0; i < m_rawList.Count; ++i)
            {
                if (!func(m_rawList[i])) break;
            }
        }

        public void ForEach(Action<T> func)
        {
            for(int i = 0; i < m_rawList.Count; ++i)
            {
                func(m_rawList[i]);
            }
        }

        /// <summary>
        /// Calls function on all chunks. If function returns false, it breaks the loop.
        /// </summary>
        /// <param name="func"></param>
        public void ForEach(Func<int3, T, bool> func)
        {
            foreach(var pair in m_dictionaryRef)
            {
                if (!func(pair.Key, pair.Value)) break;
            }
        }

        /// <summary>
        /// Calls function on all chunks. If function returns false, it breaks the loop.
        /// </summary>
        /// <param name="func"></param>
        public void ForEach(Action<int3, T> func)
        {
            foreach(var pair in m_dictionaryRef)
            {
                func(pair.Key, pair.Value);
            }
        }

        public bool TryGetValue(int3 key, out T value)
        {
            return m_dictionaryRef.TryGetValue(key, out value);
        }

        public bool Contains(int3 key)
        {
            return m_dictionaryRef.ContainsKey(key);
        }

        public void RemoveChunk(int3 key)
        {
            if (TryGetValue(key, out var chunk))
            {
                chunk.OnRemovedFromWorld();

                m_rawList.Remove(chunk);
                m_dictionaryRef.Remove(key);
            }
        }
    }
}