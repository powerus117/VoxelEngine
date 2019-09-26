using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Chunk : MonoBehaviour
{
    private MeshFilter meshFilter;
    private MeshCollider meshCollider;

    private ChunkMeshData chunkMeshData;

    private ChunkColumn chunkCol;
    private int yIndex;

    public bool hasRequestedMeshData;
    public bool hasMeshData;
    public bool hasChanged;

    public GameObject transparencyHolder;

    private void Awake()
    {
        meshFilter = GetComponent<MeshFilter>();
        meshCollider = GetComponent<MeshCollider>();
        hasRequestedMeshData = false;
        hasChanged = false;
        hasMeshData = false;
    }

    public void InitChunk(ChunkColumn chunkCol, int yIndex)
    {
        this.chunkCol = chunkCol;
        this.yIndex = yIndex;
    }

    private void Update()
    {
        if(hasChanged && !hasRequestedMeshData)
        {
            UpdateMesh();
            hasChanged = false;
        }
    }

    public void UpdateMesh()
    {
        if (!hasRequestedMeshData)
        {
            if (!chunkCol.emptyChunks[yIndex])
            {
                World.threadWorker.RequestMeshData(OnChunkMeshDataReceived, chunkCol, yIndex);
                hasRequestedMeshData = true;
            }
            else
            {
                // When chunk is empty, skip meshing
                hasMeshData = true;
            }
        }
        else
        {
            Debug.LogError("Trying to update mesh while already generating mesh");
        }
    }

    public void OnChunkMeshDataReceived(ChunkMeshData[] chunkMeshData)
    {
        hasRequestedMeshData = false;

        meshFilter.mesh = chunkMeshData[0].CreateMesh();

        meshCollider.sharedMesh = meshFilter.mesh;

        if(!chunkMeshData[1].IsEmptyMesh())
        {
            // Chunk contains water, generate a object to hold the mesh with a different material
            if (transform.childCount > 0)
            {
                transform.GetChild(0).GetComponent<MeshFilter>().mesh = chunkMeshData[1].CreateMesh();
            }
            else
            {
                GameObject holder = Instantiate(transparencyHolder, transform);
                holder.GetComponent<MeshFilter>().mesh = chunkMeshData[1].CreateMesh();
            }
        }

        hasMeshData = true;
    }

    public void SetVisible(bool visible)
    {
        gameObject.SetActive(visible);
    }
}

public enum Faces { Top, North, East, South, West, Bottom }