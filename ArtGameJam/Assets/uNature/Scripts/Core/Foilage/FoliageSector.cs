using UnityEngine;
using System.Collections.Generic;
using System.Linq;

using uNature.Core.Sectors;

namespace uNature.Core.FoliageClasses
{
    /// <summary>
    /// An sector class dedicated only to Foliage.
    /// </summary>
    public class FoliageSector : Sector
    {
        public List<FoliageChunk> FoliageChunks = new List<FoliageChunk>();

        protected override void OnChunkCreated(Chunk chunk)
        {
            base.OnChunkCreated(chunk);

            FoliageChunk FoliageChunkInstance = chunk as FoliageChunk;

            if (FoliageChunkInstance != null)
            {
                FoliageChunks.Add(FoliageChunkInstance);
            }
        }

        protected override void OnStartCreatingChunks()
        {
            base.OnStartCreatingChunks();

            for (int i = 0; i < FoliageChunks.Count; i++)
            {
                if (FoliageChunks[i] != null)
                {
                    DestroyImmediate(FoliageChunks[i]);
                }
            }

            FoliageChunks.Clear();
        }
    }
}
