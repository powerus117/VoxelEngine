using System.Collections;
using System.Collections.Generic;
using System.Threading;
using UnityEngine;

public class MeshGenerator : MonoBehaviour
{
    public int[] transparentBlockIds;

    public ChunkMeshData[] GenerateChunkMeshData(ChunkColumn chunkCol, int yIndex)
    {
        ChunkMeshData[] chunkMeshData = new ChunkMeshData[2];

        // Initializing mesh data's
        for (int i = 0; i < chunkMeshData.Length; i++)
        {
            chunkMeshData[i] = new ChunkMeshData();
        }

        int yOffset = yIndex * ChunkColumn.chunkSize;

        Color32 currentColor;
        int currentFaceCount;
        int meshBufferIndex;

        for (int z = 0; z < ChunkColumn.chunkSize; z++)
        {
            for (int y = 0; y < ChunkColumn.chunkSize; y++)
            {
                for (int x = 0; x < ChunkColumn.chunkSize; x++)
                {
                    byte currentBlockID = chunkCol.GetBlock(x, y + yOffset, z);
                    if (currentBlockID == 0)
                    {
                        // Block is air, don't generate a mesh
                        continue;
                    }

                    // Block is not air, generate a mesh
                    currentFaceCount = 0;

                    // Assume block is solid
                    meshBufferIndex = 0;

                    // Compare ID to transparent blockID's
                    if (IsTransparentBlock(currentBlockID))
                    {
                        // It's a transparent block, use transparent mesh buffer
                        meshBufferIndex = 1;
                    }

                    // Block above
                    if (ShouldDrawFace(currentBlockID, chunkCol.GetBlock(x, y + yOffset + 1, z), meshBufferIndex))
                    {
                        chunkMeshData[meshBufferIndex].AddCubeFace(x, y, z, Faces.Top);
                        currentFaceCount++;
                    }

                    // North block
                    if (ShouldDrawFace(currentBlockID, chunkCol.GetBlock(x, y + yOffset, z + 1), meshBufferIndex))
                    {
                        chunkMeshData[meshBufferIndex].AddCubeFace(x, y, z, Faces.North);
                        currentFaceCount++;
                    }

                    // East block
                    if (ShouldDrawFace(currentBlockID, chunkCol.GetBlock(x + 1, y + yOffset, z), meshBufferIndex))
                    {
                        chunkMeshData[meshBufferIndex].AddCubeFace(x, y, z, Faces.East);
                        currentFaceCount++;
                    }

                    // South block
                    if (ShouldDrawFace(currentBlockID, chunkCol.GetBlock(x, y + yOffset, z - 1), meshBufferIndex))
                    {
                        chunkMeshData[meshBufferIndex].AddCubeFace(x, y, z, Faces.South);
                        currentFaceCount++;
                    }

                    // West block
                    if (ShouldDrawFace(currentBlockID, chunkCol.GetBlock(x - 1, y + yOffset, z), meshBufferIndex))
                    {
                        chunkMeshData[meshBufferIndex].AddCubeFace(x, y, z, Faces.West);
                        currentFaceCount++;
                    }

                    // Bottom block
                    if (ShouldDrawFace(currentBlockID, chunkCol.GetBlock(x, y + yOffset - 1, z), meshBufferIndex))
                    {
                        chunkMeshData[meshBufferIndex].AddCubeFace(x, y, z, Faces.Bottom);
                        currentFaceCount++;
                    }

                    if(currentFaceCount > 0)
                    {
                        // Block is visible (has faces)
                        currentColor = EvaluateColor(chunkCol.colCoord.x * ChunkColumn.chunkSize + x,
                            yIndex * ChunkColumn.chunkSize + y,
                            chunkCol.colCoord.z * ChunkColumn.chunkSize + z,
                            currentBlockID);

                        chunkMeshData[meshBufferIndex].AddColor(currentColor, currentFaceCount);
                    }
                }
            }
        }
        

        return chunkMeshData;
    }

    /// <summary>
    /// Check if a block is transparent
    /// </summary>
    /// <param name="blockId">BlockId to check for transparency</param>
    /// <returns>True if block is transparent</returns>
    private bool IsTransparentBlock(byte blockId)
    {
        foreach (int transparentId in transparentBlockIds)
        {
            if (blockId == transparentId)
            {
                // It's a transparent block
                return true;
            }
        }

        return false;
    }

    // TODO: Checking meshBufferIndex every face check, only needs to happen once per block...
    private bool ShouldDrawFace(byte currentId, byte otherId, int meshBufferIndex)
    {
        if(meshBufferIndex == 0)
        {
            // A solid block
            if (IsTransparentBlock(otherId))
                return true;
        }
        else
        {
            // A transparent block
            if (IsTransparentBlock(otherId) && currentId != otherId)
                return true;
        }

        return false;
    }

    private Color32 EvaluateColor(int x, int y, int z, byte blockID)
    {
        float evaluatePoint = Mathf.PerlinNoise(x / 27f, z / 27f);

        Color32 calculatedColor = BlockRegistry.instance.blocks[blockID].blockColor.Evaluate(evaluatePoint);

        return calculatedColor;
    }
}

public class ChunkMeshData
{
    private List<Vector3> vertices = new List<Vector3>();
    private List<int> triangles = new List<int>();
    private List<Vector2> uvs = new List<Vector2>();
    private List<Color32> colors = new List<Color32>();

    private int faceCount;

    void ApplyVertices()
    {
        triangles.Add(faceCount * 4);
        triangles.Add(faceCount * 4 + 1);
        triangles.Add(faceCount * 4 + 3);

        triangles.Add(faceCount * 4 + 1);
        triangles.Add(faceCount * 4 + 2);
        triangles.Add(faceCount * 4 + 3);

        uvs.Add(new Vector2(0, 0));
        uvs.Add(new Vector2(0, 1));
        uvs.Add(new Vector2(1, 1));
        uvs.Add(new Vector2(1, 0));

        faceCount++;
    }

    public void AddColor(Color32 color, int amountOfFaces)
    {
        for (int i = 0; i < 4 * amountOfFaces; i++)
        {
            colors.Add(color);
        }
    }

    public void AddCubeFace(int x, int y, int z, Faces face)
    {
        Vector3 halfSize = Vector3.one * ChunkColumn.chunkSize * 0.5f;

        switch (face)
        {
            case Faces.Top:
                vertices.Add((new Vector3(x, y + 1, z) - halfSize) * ChunkColumn.blockSize);
                vertices.Add((new Vector3(x, y + 1, z + 1) - halfSize) * ChunkColumn.blockSize);
                vertices.Add((new Vector3(x + 1, y + 1, z + 1) - halfSize) * ChunkColumn.blockSize);
                vertices.Add((new Vector3(x + 1, y + 1, z) - halfSize) * ChunkColumn.blockSize);

                ApplyVertices();
                break;
            case Faces.North:
                vertices.Add((new Vector3(x + 1, y, z + 1) - halfSize) * ChunkColumn.blockSize);
                vertices.Add((new Vector3(x + 1, y + 1, z + 1) - halfSize) * ChunkColumn.blockSize);
                vertices.Add((new Vector3(x, y + 1, z + 1) - halfSize) * ChunkColumn.blockSize);
                vertices.Add((new Vector3(x, y, z + 1) - halfSize) * ChunkColumn.blockSize);

                ApplyVertices();
                break;
            case Faces.East:
                vertices.Add((new Vector3(x + 1, y, z) - halfSize) * ChunkColumn.blockSize);
                vertices.Add((new Vector3(x + 1, y + 1, z) - halfSize) * ChunkColumn.blockSize);
                vertices.Add((new Vector3(x + 1, y + 1, z + 1) - halfSize) * ChunkColumn.blockSize);
                vertices.Add((new Vector3(x + 1, y, z + 1) - halfSize) * ChunkColumn.blockSize);

                ApplyVertices();
                break;
            case Faces.South:
                vertices.Add((new Vector3(x, y, z) - halfSize) * ChunkColumn.blockSize);
                vertices.Add((new Vector3(x, y + 1, z) - halfSize) * ChunkColumn.blockSize);
                vertices.Add((new Vector3(x + 1, y + 1, z) - halfSize) * ChunkColumn.blockSize);
                vertices.Add((new Vector3(x + 1, y, z) - halfSize) * ChunkColumn.blockSize);

                ApplyVertices();
                break;
            case Faces.West:
                vertices.Add((new Vector3(x, y, z + 1) - halfSize) * ChunkColumn.blockSize);
                vertices.Add((new Vector3(x, y + 1, z + 1) - halfSize) * ChunkColumn.blockSize);
                vertices.Add((new Vector3(x, y + 1, z) - halfSize) * ChunkColumn.blockSize);
                vertices.Add((new Vector3(x, y, z) - halfSize) * ChunkColumn.blockSize);

                ApplyVertices();
                break;
            case Faces.Bottom:
                vertices.Add((new Vector3(x, y, z + 1) - halfSize) * ChunkColumn.blockSize);
                vertices.Add((new Vector3(x, y, z) - halfSize) * ChunkColumn.blockSize);
                vertices.Add((new Vector3(x + 1, y, z) - halfSize) * ChunkColumn.blockSize);
                vertices.Add((new Vector3(x + 1, y, z + 1) - halfSize) * ChunkColumn.blockSize);

                ApplyVertices();
                break;
            default:
                break;
        }
    }

    public Mesh CreateMesh()
    {
        Mesh mesh = new Mesh();
        mesh.vertices = vertices.ToArray();
        mesh.triangles = triangles.ToArray();
        mesh.uv = uvs.ToArray();
        mesh.colors32 = colors.ToArray();
        mesh.RecalculateNormals();

        return mesh;
    }

    public bool IsEmptyMesh()
    {
        return (faceCount <= 0);
    }
}