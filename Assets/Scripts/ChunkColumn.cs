using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ChunkColumn : MonoBehaviour{
    [HideInInspector]
    public byte[,,] chunkBlockData;
    public bool[] emptyChunks;

    public const int chunkSize = 32;
    public const int worldHeight = 6;
    public const float blockSize = 0.5f;

    public ColumnCoord colCoord;
    public GameObject chunkPrefab;

    private Chunk[] chunks;
    public bool hasMapData;

    private bool isVisible;

    public float treeChance = 0.01f;

    public GameObject[] grassObjects;
    public GameObject decorationHolder;

    private int amountOfNeighboursWithData = 0;

    public void Init(ColumnCoord colCoord)
    {
        this.colCoord = colCoord;
    }

    public void StartGenerating()
    {
        World.threadWorker.RequestMapData(OnChunkMapDataReceived, colCoord);
    }

    public void OnChunkMapDataReceived(MapDataInfo chunkMapData)
    {
        chunks = new Chunk[worldHeight];

        chunkBlockData = chunkMapData.voxelData;
        emptyChunks = chunkMapData.isEmpty;
        hasMapData = true;


        TryBuildMeshes();
        NotifyNeighbours();
    }

    private void BuildTrees()
    {
        System.Random prng = new System.Random(MapGenerator.instance.seed * 31 + colCoord.x * 12 + colCoord.z * 24);

        for (int z = 0; z < chunkSize; z++)
        {
            for (int x = 0; x < chunkSize; x++)
            {
                if(prng.NextDouble() < treeChance)
                {
                    //Gen tree
                    for (int y = worldHeight * chunkSize - 1; y >= 0; y--)
                    {
                        if(chunkBlockData[x, y, z] != 0)
                        {
                            // Only spawn on grass
                            if (chunkBlockData[x, y, z] != 3)
                                break;

                            BuildTree(x, y, z, prng);
                            break;
                        }
                    }
                }
            }
        }
    }

    int minAmountOfPatches = 1;
    int maxAmountOfPatches = 4;
    int minAmountOfGrassPerPatch = 5;
    int maxAmountOfGrassPerPatch = 10;
    int grassPatchSize = 5;

    float grassSizePercentageStep;

    private void BuildGrassPatches()
    {
        System.Random prng = new System.Random(MapGenerator.instance.seed * 67 + colCoord.x * 9 + colCoord.z * 7);

        int amountOfPatches = prng.Next(minAmountOfPatches, maxAmountOfPatches + 1);
        grassSizePercentageStep = 1.0f / grassObjects.Length;

        for (int i = 0; i < amountOfPatches; i++)
        {
            int randomX = prng.Next(0, chunkSize);
            int randomZ = prng.Next(0, chunkSize);

            BuildGrassPatch(randomX, randomZ, prng);
        }
    }

    private void BuildGrassPatch(int x, int z, System.Random prng)
    {
        int amountOfGrass = prng.Next(minAmountOfGrassPerPatch, maxAmountOfGrassPerPatch);

        for (int i = 0; i < amountOfGrass; i++)
        {
            int placeX = prng.Next(-grassPatchSize, grassPatchSize + 1);
            int placeZ = prng.Next(-grassPatchSize, grassPatchSize + 1);

            PlaceGrass(placeX, placeZ, x, z);
        }
    }

    private void PlaceGrass(int x, int z, int originX, int originZ)
    {
        //Spawn grass
        for (int y = worldHeight * chunkSize - 1; y >= 0; y--)
        {
            if (GetBlock(x + originX, y, z + originZ) != 0)
            {
                // Only spawn on grass
                if (GetBlock(x + originX, y, z + originZ) != 3)
                    break;

                float distPercentage = Mathf.Clamp01((float)Mathf.Sqrt(x * x + z * z) / grassPatchSize);

                for (int j = 0; j < grassObjects.Length; j++)
                {
                    if(distPercentage <= (j + 1) * grassSizePercentageStep)
                    {
                        Vector3 spawnPos = new Vector3(transform.position.x + (-chunkSize / 2 + originX + x + 0.5f) * blockSize,
                                                       (y + 1 - chunkSize / 2) * blockSize,
                                                       transform.position.z + (-chunkSize / 2 + originZ + z + 0.5f) * blockSize);

                        Instantiate(grassObjects[j], spawnPos, Quaternion.identity, decorationHolder.transform);
                        break;
                    }
                }
                break;
            }
        }
    }

    int minTreeHeight = 7;
    int maxTreeHeight = 15;
    int minBushAmount = 1;
    int maxBushAmount = 6;
    int bushStartHeight = 8;

    private void BuildTree(int x, int y, int z, System.Random prng)
    {
        if (!IsSpaceEmpty(x - 5, y + 5, z - 5, x + 5, y + 7, z + 5))
            return;

        int treeHeight = prng.Next(minTreeHeight, maxTreeHeight);

        // Trunk
        byte[] blockIdsTrunkReplaces = new byte[] { 0, 3 };
        SetRectBlocks(x, y, z, x + 1, y + treeHeight - 2, z + 1, 4);
        // TODO: Maybe ellipse
        ReplaceRectBlocks(x - 1, y - 2, z, x - 1, y + prng.Next(2), z + 1, blockIdsTrunkReplaces, 4);
        ReplaceRectBlocks(x + 2, y - 2, z, x + 2, y + prng.Next(2), z + 1, blockIdsTrunkReplaces, 4);
        ReplaceRectBlocks(x, y - 2, z - 1, x + 1, y + prng.Next(2), z - 1, blockIdsTrunkReplaces, 4);
        ReplaceRectBlocks(x, y - 2, z + 2, x + 1, y + prng.Next(2), z + 2, blockIdsTrunkReplaces, 4);

        int numberOfBushes = Mathf.RoundToInt(Mathf.InverseLerp(minTreeHeight, maxTreeHeight - 1, treeHeight) * (maxBushAmount - minBushAmount) + minBushAmount);

        // Initial bush
        FillEllipsoid(x + prng.Next(0, 2), y + treeHeight + prng.Next(-1, 2), z + prng.Next(0, 2),
                        prng.Next(4, 8), prng.Next(3, 5), prng.Next(4, 8), 5);
        numberOfBushes--;

        for (int i = 0; i < numberOfBushes; i++)
        {
            // Get a random point
            int bushHeight = Mathf.RoundToInt((float)(i + 1) / numberOfBushes * (treeHeight + 2 - bushStartHeight) + bushStartHeight);
            int xPos = prng.Next(x - 3, x + 5);
            int zPos = prng.Next(z - 3, z + 5);
            int width = prng.Next(3, 8);
            int depth = prng.Next(3, 8);
            int height = prng.Next(3, 5);

            // Build the bush
            FillEllipsoid(xPos, bushHeight + y, zPos, width, height, depth, 5);

            // Build a branch
            int toYPosBranch = y + bushHeight - height + 1;
            int fromYPosBranch = Mathf.Clamp(toYPosBranch - prng.Next(2, 5), y, y + treeHeight);
            int trunkXPos = Mathf.Clamp(xPos, x, x + 1);
            int trunkZPos = Mathf.Clamp(zPos, z, z + 1);

            DrawLine(trunkXPos, fromYPosBranch, trunkZPos, xPos, toYPosBranch, zPos, 4);
        }
    }

    private void FillEllipsoid(int x, int y, int z, int xRad, int yRad, int zRad, byte blockId)
    {
        for (int i = -xRad; i <= xRad; i++)
        {
            for (int j = -yRad; j <= yRad; j++)
            {
                for (int k = -zRad; k <= zRad; k++)
                {
                    float ellipsoidVal = Mathf.Pow((float)i / xRad, 2) + Mathf.Pow((float)j / yRad, 2) + Mathf.Pow((float)k / zRad, 2);
                    if(ellipsoidVal < 1)
                    {
                        SetBlock(x + i, y + j, z + k, blockId);
                    }
                }
            }
        }
    }

    private void DrawLine(int x, int y, int z, int xTo, int yTo, int zTo, byte blockId)
    {
        int dX = xTo - x;
        float xStep = Mathf.Sign(dX);
        float tDeltaX = 1.0f / Mathf.Abs(dX);
        float tMaxX = tDeltaX;

        int dY = yTo - y;
        float yStep = Mathf.Sign(dY);
        float tDeltaY = 1.0f / Mathf.Abs(dY);
        float tMaxY = tDeltaY;

        int dZ = zTo - z;
        float zStep = Mathf.Sign(dZ);
        float tDeltaZ = 1.0f / Mathf.Abs(dZ);
        float tMaxZ = tDeltaZ;

        SetBlock(x, y, z, blockId);

        while (x != xTo || y != yTo || z != zTo)
        {
            if(tMaxX < tMaxY)
            {
                if (tMaxX < tMaxZ)
                {
                    tMaxX += tDeltaX;
                    x += (int)xStep;
                } else
                {
                    tMaxZ += tDeltaZ;
                    z += (int)zStep;
                }   
            } else
            {
                if (tMaxY < tMaxZ)
                {
                    tMaxY += tDeltaY;
                    y += (int)yStep;
                }
                else
                {
                    tMaxZ += tDeltaZ;
                    z += (int)zStep;
                }
            }
            SetBlock(x, y, z, blockId);
        }
    }

    public void TryBuildMeshes()
    {
        if(hasMapData)
        {
            // Check all neighbouring chunks to see if they have their data. If so, we can generate a mesh
            if(CheckNeighbourData())
            {
                BuildTrees();
                BuildMeshes();
                BuildGrassPatches();
            }
        }
    }

    private void NotifyNeighbours()
    {
        for (int x = -1; x <= 1; x++)
        {
            for (int z = -1; z <= 1; z++)
            {
                // Current chunk
                if (x == 0 && z == 0)
                    continue;

                ChunkColumn aNeighbourChunk = World.GetColumn(colCoord.GetRelativePos(x, z));

                // If we find a chunk without data, we don't generate
                if (aNeighbourChunk != null)
                    aNeighbourChunk.IncrementNeighbourDataCount();
            }
        }
    }

    private void BuildMeshes()
    {
        for (int i = 0; i < chunks.Length; i++)
        {
            GameObject newChunk = Instantiate(chunkPrefab, new Vector3(transform.position.x, i * chunkSize * blockSize, transform.position.z), Quaternion.identity);
            newChunk.transform.parent = transform;
            newChunk.name = "aChunk " + (i + 1);
            chunks[i] = newChunk.GetComponent<Chunk>();
            chunks[i].InitChunk(this, i);
            chunks[i].UpdateMesh();
        }

        World.chunksVisibleLastUpdate.Add(this);
        isVisible = true;
    }

    public void SetRectBlocks(int fromX, int fromY, int fromZ, int toX, int toY, int toZ, byte blockId)
    {
        for (int i = fromX; i <= toX; i++)
        {
            for (int j = fromY; j <= toY; j++)
            {
                for (int k = fromZ; k <= toZ; k++)
                {
                    SetBlock(i, j, k, blockId);
                }
            }
        }
    }

    public void ReplaceRectBlocks(int fromX, int fromY, int fromZ, int toX, int toY, int toZ, byte[] blockIdsToReplace, byte newBlockId)
    {
        for (int i = fromX; i <= toX; i++)
        {
            for (int j = fromY; j <= toY; j++)
            {
                for (int k = fromZ; k <= toZ; k++)
                {
                    for (int index = 0; index < blockIdsToReplace.Length; index++)
                    {
                        if (GetBlock(i, j, k) == blockIdsToReplace[index])
                        {
                            SetBlock(i, j, k, newBlockId);
                            break;
                        }
                    }
                }
            }
        }
    }

    private bool IsSpaceEmpty(int fromX, int fromY, int fromZ, int toX, int toY, int toZ)
    {
        for (int i = fromX; i <= toX; i++)
        {
            for (int j = fromY; j <= toY; j++)
            {
                for (int k = fromZ; k <= toZ; k++)
                {
                    if (GetBlock(i, j, k) != 0)
                        return false;
                }
            }
        }

        return true;
    }

    public void SetBlock(int x, int y, int z, byte blockID)
    {
        if(hasMapData)
        {
            if (y >= 0 && y < worldHeight * chunkSize)
            {
                if (isInRange(x) && isInRange(z))
                {
                    //Local coords
                    chunkBlockData[x, y, z] = blockID;

                    // Grab the chunk segment based on height
                    int chunkIndex = y / chunkSize;
                    Chunk changedChunk = chunks[chunkIndex];

                    if (blockID != 0)
                        emptyChunks[chunkIndex] = false;

                    if (changedChunk != null && (changedChunk.hasMeshData || changedChunk.hasRequestedMeshData))
                    {
                        // If chunk exists and generated meshdata, flag it as dirty (changed)
                        changedChunk.hasChanged = true;
                        UpdateNeighbourChunksNecessary(x, y, z);
                    }
                }
                else
                {
                    //Outside, set it in world
                    World.SetBlock(colCoord.x * chunkSize + x, y, colCoord.z * chunkSize + z, blockID);
                }
            }
            else
            {
                Debug.Log("Block set too high or too low: " + y);
            }
        }
        else
        {
            Debug.Log("Block set on chunk without mapdata");
        }
    }

    public void UpdateChunk(int chunkIndex)
    {
        Chunk changedChunk = chunks[chunkIndex];

        if (changedChunk != null && changedChunk.hasMeshData)
        {
            // Flag chunk for remeshing
            changedChunk.hasChanged = true;
        }
    }

    /// <summary>
    /// Check if a block set is done on the edge, meaning a mesh update has to happen to the corresponding neighbour
    /// </summary>
    /// <param name="x"></param>
    /// <param name="y"></param>
    /// <param name="z"></param>
    private void UpdateNeighbourChunksNecessary(int x, int y, int z)
    {
        if(x == 0)
        {
            ChunkColumn chunkCol = World.GetColumn(colCoord.GetRelativePos(-1, 0));

            if (chunkCol != null)
            {
                chunkCol.UpdateChunk(y / chunkSize);
            }
        }
        else if(x == chunkSize - 1)
        {
            ChunkColumn chunkCol = World.GetColumn(colCoord.GetRelativePos(1, 0));

            if (chunkCol != null)
            {
                chunkCol.UpdateChunk(y / chunkSize);
            }
        }

        if (y == 0)
        {
            UpdateChunk(y / chunkSize - 1);
        }
        else if (y == chunkSize - 1)
        {
            UpdateChunk(y / chunkSize + 1);
        }

        if (z == 0)
        {
            ChunkColumn chunkCol = World.GetColumn(colCoord.GetRelativePos(0, -1));

            if (chunkCol != null)
            {
                chunkCol.UpdateChunk(y / chunkSize);
            }
        }
        else if (z == chunkSize - 1)
        {
            ChunkColumn chunkCol = World.GetColumn(colCoord.GetRelativePos(0, 1));

            if (chunkCol != null)
            {
                chunkCol.UpdateChunk(y / chunkSize);
            }
        }
    }

    public bool CheckNeighbourData()
    {
        amountOfNeighboursWithData = 0;
        bool chunksHaveData = true;

        // Check all neighbouring chunks to see if they have their data. If so, we can generate a mesh
        for (int x = -1; x <= 1; x++)
        {
            for (int z = -1; z <= 1; z++)
            {
                if (x == 0 && z == 0)
                    continue;

                // If we find a chunk without data, we don't generate
                if (World.ColumnHasMapdata(colCoord.GetRelativePos(x, z)))
                    amountOfNeighboursWithData += 1;
                else
                    chunksHaveData = false;
            }
        }

        return chunksHaveData;
    }

    public byte GetBlock(int x, int y, int z)
    {
        try
        {
            if (!isInRange(x) || !isInRange(z))
            {
                return World.GetBlock(colCoord.x * chunkSize + x, y, colCoord.z * chunkSize + z);
            }

            if (y < 0)
                return 2;
            else if (y >= chunkSize * worldHeight)
                return 0;

            return chunkBlockData[x, y, z];
        }
        catch
        {
            Debug.Log("Chunk error on getting block");
        }
        return 1;
    }

    public bool isInRange(int x)
    {
        if (x < 0 || x >= chunkSize)
            return false;

        return true;
    }

    public void SetVisible()
    {
        if (chunks == null || chunks[0] == null || isVisible)
            return;

        for (int i = 0; i < chunks.Length; i++)
        {
            chunks[i].SetVisible(true);
        }

        decorationHolder.SetActive(true);

        World.chunksVisibleLastUpdate.Add(this);
        isVisible = true;
    }

    public void SetInVisible()
    {
        if (chunks == null || chunks[0] == null || !isVisible)
            return;

        for (int i = 0; i < chunks.Length; i++)
        {
            chunks[i].SetVisible(false);
        }

        decorationHolder.SetActive(false);

        World.chunksVisibleLastUpdate.Remove(this);
        isVisible = false;
    }

    public void IncrementNeighbourDataCount()
    {
        amountOfNeighboursWithData++;

        // 8 is the amount of neighbouring chunks (always 8)
        if(amountOfNeighboursWithData >= 8)
        {
            TryBuildMeshes();
        }
    }

    public class MapDataInfo
    {
        public byte[,,] voxelData;
        public bool[] isEmpty;

        public MapDataInfo()
        {
            isEmpty = new bool[ChunkColumn.worldHeight];
        }

        public void SetAllEmpty()
        {
            for (int i = 0; i < isEmpty.Length; i++)
            {
                isEmpty[i] = true;
            }
        }
    }
}
