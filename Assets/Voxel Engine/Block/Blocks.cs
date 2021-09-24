using System.Collections.Generic;
using Unity.Collections;
using Unity.Mathematics;
using UnityEngine;

namespace VoxelEngine
{
    public static class Blocks
    {
        public const uint BLOCK_AIR = 0;

        public static uint BLOCK_DIRT {get; private set;}

        public static uint BLOCK_GRASS {get; private set;}

        public static ComputeBuffer BlocksBuffer => m_blocksBuffer;
        
        public static NativeList<Block> NativeList => m_blocks;

        static NativeList<Block> m_blocks;

        static ComputeBuffer m_blocksBuffer;

        static Block AirBlock = new Block("Air", 0);

        /// <summary>
        /// Returns the block with the specific ID.
        /// </summary>
        /// <param name="blockId"></param>
        /// <returns></returns>
        public static Block GetBlockByID(uint blockId)
        {
            if (blockId == 0) return AirBlock;
            else return m_blocks[(int)blockId - 1];
        }

        /// <summary>
        /// Creates the block's list and adds compound blocks
        /// </summary>
        public static void InitializeBlocks()
        {
            if (m_blocks.IsCreated) return;
            
            m_blocks = new NativeList<Block>(Allocator.Persistent);
            m_blocksBuffer = new ComputeBuffer(1000, Block.SIZEOF);

            GPUMemoryTracker.Register(m_blocksBuffer);

            BLOCK_DIRT = RegisterBlock(new Block("Dirt Block",
                UVPacker.Pack(math.int2(2, 0), math.int2(2, 0), math.int2(2, 0), math.int2(2, 0))
            ));

            BLOCK_GRASS = RegisterBlock(new Block("Grass Block",
                UVPacker.Pack(math.int2(0, 0), math.int2(2, 0), math.int2(3, 0), math.int2(3, 0))
            ));
        }

        /// <summary>
        /// Adds the block to the block list.
        /// </summary>
        /// <param name="block">Block to be added</param>
        /// <returns>The block's ID</returns>
        public static uint RegisterBlock(Block block)
        {
            m_blocks.Add(block);

            m_blocksBuffer.SetData<Block>(m_blocks, 0, 0, m_blocks.Length);

            block.id = m_blocks.Length;

            return (uint)m_blocks.Length;
        }

        /// <summary>
        /// Adds the block to the block list.
        /// This will update the block's ID.
        /// </summary>
        /// <param name="block">Block to be added</param>
        /// <returns></returns>
        public static uint RegisterBlock(ref Block block)
        {
            uint id = RegisterBlock(block);
            block.id = (int)id;
            return id;
        }

        public static void Dispose()
        {
            if (m_blocks.IsCreated)
                m_blocks.Dispose();

            m_blocksBuffer?.Release();
            m_blocksBuffer = null;
        }
    }
}