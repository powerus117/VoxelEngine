using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Diagnostics;

public class MapGenerator : MonoBehaviour
{
    public float zoomScale;
    public Vector3 manualOffset;

    public int seed;
    public int octaves;
    [Range(0, 1f)]
    public float persistence;
    public float lacunarity;

    public AnimationCurve _heightCurve;

    public int minHeight = 4;
    public float multiplier = 10;

    public int grassLayerThickness = 1;

    public static MapGenerator instance = null;

    //3D variables
    public float solidHeight = 20;
    public float airHeight = 70;
    public float adjustScalar = 0.3f;

    // Has to be a power of 2
    public int horizontalNoiseScale = 8;
    public int verticalNoiseScale = 4;

    public int waterHeight = 50;

    public const float SOLID_TRESHOLD = 0.5f;

    private void Awake()
    {
        //Singleton pattern implementation
        if (instance == null)
            instance = this;
        else if (instance != this)
            Destroy(gameObject);
    }

    /// <summary>
    /// Generate chunk map data based on perlin noise and interpolation
    /// </summary>
    /// <param name="colCoord">Column coordinate to sample</param>
    /// <returns>Chunk block data</returns>
    public ChunkColumn.MapDataInfo GenerateNoiseMap(ColumnCoord colCoord)
    {
        int size = ChunkColumn.chunkSize;
        int amountOfVerticalChunks = ChunkColumn.worldHeight;

        ChunkColumn.MapDataInfo mapData = new ChunkColumn.MapDataInfo();
        mapData.SetAllEmpty();
        byte[,,] chunkBlockData = new byte[size, size * amountOfVerticalChunks, size];

        // Sampling all points using perlin noise within chunk
        float[,,] sampleNoiseMap = SamplePoints(colCoord);

        // 0 = differences, 1 = all non-solid, 2 = all solid
        int allSolidIndex;

        for (int x = 0; x < sampleNoiseMap.GetLength(0) - 1; x++)
        {
            for (int y = 0; y < sampleNoiseMap.GetLength(1) - 1; y++)
            {
                for (int z = 0; z < sampleNoiseMap.GetLength(2) - 1; z++)
                {
                    allSolidIndex = GetAllSolidIndex(ref sampleNoiseMap, x, y, z);

                    if (allSolidIndex == 0)
                    {
                        InterpolatePoints(ref sampleNoiseMap, ref chunkBlockData, x, y, z, colCoord);
                        // Any interpolation means the is atleast 1 solid block
                        mapData.isEmpty[(y * verticalNoiseScale + verticalNoiseScale / 2) / ChunkColumn.chunkSize] = false;
                    }
                    else
                    {
                        float noiseVal;
                        int blockX, blockY, blockZ;

                        if (allSolidIndex == 2)
                            mapData.isEmpty[(y * verticalNoiseScale + verticalNoiseScale / 2) / ChunkColumn.chunkSize] = false;

                        // All points the same, skip interpolation
                        for (int k = 0; k < horizontalNoiseScale; k++)
                        {
                            for (int j = 0; j < verticalNoiseScale; j++)
                            {
                                for (int i = 0; i < horizontalNoiseScale; i++)
                                {
                                    blockX = x * horizontalNoiseScale + k;
                                    blockY = y * verticalNoiseScale + j;
                                    blockZ = z * horizontalNoiseScale + i;

                                    noiseVal = allSolidIndex == 1 ? 0 : 1;

                                    byte blockId = GetBlockIdFromNoise(blockX, blockY, blockZ, noiseVal, colCoord);
                                    chunkBlockData[blockX, blockY, blockZ] = blockId;

                                    // Add grass to the top layer
                                    if (y == 0)
                                        continue;

                                    if (blockId == 0 && chunkBlockData[blockX, blockY - 1, blockZ] == 2)
                                    {
                                        // Spawn grass
                                        chunkBlockData[blockX, blockY - 1, blockZ] = 3;
                                    }
                                }
                            }
                        }
                    }
                }
            }
        }

        mapData.voxelData = chunkBlockData;
        return mapData;
    }

    /// <summary>
    /// Translate the noisevalue into a blockId at a position
    /// </summary>
    /// <param name="x">Local x position</param>
    /// <param name="y">Local y position</param>
    /// <param name="z">Local z position</param>
    /// <param name="noiseVal">Sampled value</param>
    /// <param name="colCoord">Column coordinate</param>
    /// <returns>The blockId</returns>
    private byte GetBlockIdFromNoise(int x, int y, int z, float noiseVal, ColumnCoord colCoord)
    {
        // Treshold needed
        if (noiseVal > SOLID_TRESHOLD)
        {
            // Spawn block
            if (y < waterHeight + 5)
            {
                if (y < waterHeight + 1 || y < waterHeight + 3 + (Mathf.PerlinNoise((x + colCoord.x * ChunkColumn.chunkSize) / 13f, (z + colCoord.z * ChunkColumn.chunkSize) / 17f) * 2 - 1) * 2)
                {
                    // Sand
                    return 6;
                }
            }
            // Stone
            return 2;
        }
        else if (y < waterHeight)
        {
            // Water
            return 1;
        }
        else
        {
            // Air
            return 0;
        }
    }

    /// <summary>
    /// Samples all points in a chunk column using perlin noise
    /// </summary>
    /// <param name="colCoord">The coordinate of the chunk</param>
    /// <returns>The filled noisemap</returns>
    private float[,,] SamplePoints(ColumnCoord colCoord)
    {
        int size = ChunkColumn.chunkSize;
        int amountOfVerticalChunks = ChunkColumn.worldHeight;
        float halfSize = size * 0.5f;

        float[,,] sampleNoiseMap = new float[size / horizontalNoiseScale + 1, size * amountOfVerticalChunks / verticalNoiseScale + 1, size / horizontalNoiseScale + 1];

        float sampleX, sampleY, sampleZ, adjustmentVal;

        // Sampling points
        for (int x = 0; x < sampleNoiseMap.GetLength(0); x++)
        {
            for (int y = 0; y < sampleNoiseMap.GetLength(1); y++)
            {
                for (int z = 0; z < sampleNoiseMap.GetLength(2); z++)
                {
                    Vector3 chunkOffset = new Vector3(colCoord.x, 0, colCoord.z) * size;

                    sampleX = ((float)x * horizontalNoiseScale - halfSize + chunkOffset.x) * ChunkColumn.blockSize / zoomScale;
                    sampleY = ((float)y * verticalNoiseScale + chunkOffset.y) * ChunkColumn.blockSize / zoomScale;
                    sampleZ = ((float)z * horizontalNoiseScale - halfSize + chunkOffset.z) * ChunkColumn.blockSize / zoomScale;

                    sampleNoiseMap[x, y, z] = CustomPerlinNoise.PerlinNoise(sampleX, sampleY, sampleZ);

                    adjustmentVal = (1 - Mathf.InverseLerp(solidHeight, airHeight, y * verticalNoiseScale) * (1 - Mathf.PerlinNoise(sampleX, sampleZ) * 0.2f)) * 2 - 1;

                    adjustmentVal *= adjustScalar;

                    sampleNoiseMap[x, y, z] += adjustmentVal;
                }
            }
        }

        return sampleNoiseMap;
    }

    /// <summary>
    /// Interpolate points within a sampling block and translate them to blockId's to fill the array with
    /// </summary>
    /// <param name="sampleNoiseMap">Noise map to interpolate from</param>
    /// <param name="blockMap">BlockId array to fill</param>
    /// <param name="x">Local x coordinate</param>
    /// <param name="y">Local y coordinate</param>
    /// <param name="z">Local z coordinate</param>
    /// <param name="colCoord">Column coordinate</param>
    private void InterpolatePoints(ref float[,,] sampleNoiseMap, ref byte[,,] blockMap, int x, int y, int z, ColumnCoord colCoord)
    {
        float xStep1, xStep2, xStep3, xStep4, xLerped1, xLerped2, xLerped3, xLerped4, yPercentage, zPercentage, interpolatedY1, interpolatedY2;
        int blockX, blockY, blockZ;

        // Accurate interpolation
        xStep1 = (sampleNoiseMap[x + 1, y, z] - sampleNoiseMap[x, y, z]) / horizontalNoiseScale;
        xStep2 = (sampleNoiseMap[x + 1, y + 1, z] - sampleNoiseMap[x, y + 1, z]) / horizontalNoiseScale;
        xStep3 = (sampleNoiseMap[x + 1, y, z + 1] - sampleNoiseMap[x, y, z + 1]) / horizontalNoiseScale;
        xStep4 = (sampleNoiseMap[x + 1, y + 1, z + 1] - sampleNoiseMap[x, y + 1, z + 1]) / horizontalNoiseScale;

        for (int k = 0; k < horizontalNoiseScale; k++)
        {
            xLerped1 = sampleNoiseMap[x, y, z] + xStep1 * k;
            xLerped2 = sampleNoiseMap[x, y + 1, z] + xStep2 * k;
            xLerped3 = sampleNoiseMap[x, y, z + 1] + xStep3 * k;
            xLerped4 = sampleNoiseMap[x, y + 1, z + 1] + xStep4 * k;

            for (int j = 0; j < verticalNoiseScale; j++)
            {
                yPercentage = (float)j / verticalNoiseScale;

                interpolatedY1 = Mathf.Lerp(xLerped1, xLerped2, yPercentage);
                interpolatedY2 = Mathf.Lerp(xLerped3, xLerped4, yPercentage);

                for (int i = 0; i < horizontalNoiseScale; i++)
                {
                    zPercentage = (float)i / horizontalNoiseScale;
                    blockX = x * horizontalNoiseScale + k;
                    blockY = y * verticalNoiseScale + j;
                    blockZ = z * horizontalNoiseScale + i;

                    byte blockId = GetBlockIdFromNoise(blockX, blockY, blockZ, Mathf.Lerp(interpolatedY1, interpolatedY2, zPercentage), colCoord);
                    blockMap[blockX, blockY, blockZ] = blockId;

                    // Change top layer to grass
                    if (y == 0)
                        continue;

                    if (blockId == 0 && blockMap[blockX, blockY - 1, blockZ] == 2)
                    {
                        // Spawn grass
                        blockMap[blockX, blockY - 1, blockZ] = 3;
                    }
                }
            }
        }
    }

    /// <summary>
    /// Check if all sampling points are the same considering the treshold 
    /// since no interpolation is needed in that case (full air or full solid)
    /// </summary>
    /// <param name="noiseMap">Reference to the noise map</param>
    /// <param name="x">current x coordinate of the block</param>
    /// <param name="y">current y coordinate of the block</param>
    /// <param name="z">current z coordinate of the block</param>
    /// <returns>Solid index (0 = differences, 1 = all non-solid, 2 = all solid)</returns>
    private int GetAllSolidIndex(ref float[,,] noiseMap, int x, int y, int z)
    {
        int allSolidIndex = noiseMap[x, y, z] > SOLID_TRESHOLD ? 2 : 1;
        int currentSolidIndex;

        for (int i = 0; i < 2; i++)
        {
            for (int j = 0; j < 2; j++)
            {
                for (int k = 0; k < 2; k++)
                {
                    currentSolidIndex = noiseMap[x + i, y + j, z + k] > SOLID_TRESHOLD ? 2 : 1;

                    if (currentSolidIndex != allSolidIndex)
                    {
                        return 0;
                    }
                }
            }
        }

        return allSolidIndex;
    }
}
