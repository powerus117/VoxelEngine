using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading;
using UnityEngine;

public class World : MonoBehaviour
{
    public GameObject columnPrefab;

    public GameObject viewer;
    public int viewDistance;
    private float sqrViewTreshold;
    private Vector3 oldViewerPos;

    public static MapGenerator perlinMapGen;
    public static MeshGenerator meshGen;
    public static ThreadPoolGen threadWorker;

    private static Dictionary<ColumnCoord, ChunkColumn> columns;
    private List<ColumnCoord> columnsToBuild;

    public static List<ChunkColumn> chunksVisibleLastUpdate;
    const float ViewerMoveTresholdForUpdateChunk = 5f * ChunkColumn.blockSize;
    const float sqrViewerMoveTresholdForUpdateChunk = ViewerMoveTresholdForUpdateChunk * ViewerMoveTresholdForUpdateChunk;

    void Start()
    {
        perlinMapGen = GetComponent<MapGenerator>();
        meshGen = GetComponent<MeshGenerator>();
        threadWorker = GetComponent<ThreadPoolGen>();

        columns = new Dictionary<ColumnCoord, ChunkColumn>();
        chunksVisibleLastUpdate = new List<ChunkColumn>();

        columnsToBuild = new List<ColumnCoord>();
        sqrViewTreshold = viewDistance * viewDistance;

        UpdateVisibleChunks();
        oldViewerPos = viewer.transform.position;
    }

    void Update()
    {
        if ((oldViewerPos - viewer.transform.position).sqrMagnitude > sqrViewerMoveTresholdForUpdateChunk)
        {
            oldViewerPos = viewer.transform.position;
            UpdateVisibleChunks();
        }
        
        if (columnsToBuild.Count > 0)
        {
            BuildNextColumn();
        }
    }

    private void UpdateVisibleChunks()
    {
        int currentChunkCoordX = Mathf.RoundToInt(viewer.transform.position.x / (ChunkColumn.chunkSize * ChunkColumn.blockSize));
        int currentChunkCoordZ = Mathf.RoundToInt(viewer.transform.position.z / (ChunkColumn.chunkSize * ChunkColumn.blockSize));
        ColumnCoord playerCoord = new ColumnCoord(currentChunkCoordX, currentChunkCoordZ);
        
        for (int i = chunksVisibleLastUpdate.Count - 1; i >= 0; i--)
        {
            if(chunksVisibleLastUpdate[i].colCoord.GetSqrDistance(playerCoord) > sqrViewTreshold)
            {
                // Chunk is out of view distance
                chunksVisibleLastUpdate[i].SetInVisible();
            }
        }
        //chunksVisibleLastUpdate.Clear();
        
        columnsToBuild.Clear();
        
        for (int zOffset = -viewDistance; zOffset <= viewDistance; zOffset++)
        {
            for (int xOffset = -viewDistance; xOffset <= viewDistance; xOffset++)
            {
                ColumnCoord currentViewedColumnCoord = new ColumnCoord(currentChunkCoordX + xOffset, currentChunkCoordZ + zOffset);

                if(currentViewedColumnCoord.GetSqrDistance(playerCoord) < sqrViewTreshold)
                {
                    ChunkColumn currentColumn;
                    if (columns.TryGetValue(currentViewedColumnCoord, out currentColumn))
                    {
                         currentColumn.SetVisible();
                    }
                    else
                    {
                        GenerateColumn(currentViewedColumnCoord);
                    }
                }
            }
        }

        OrderList(playerCoord);
    }

    private void OrderList(ColumnCoord playerCoord)
    {
        columnsToBuild.Sort(delegate(ColumnCoord colCoord, ColumnCoord colCoord2)
        {
            return colCoord.GetSqrDistance(playerCoord).CompareTo(colCoord2.GetSqrDistance(playerCoord));
        });
    }

    public void GenerateColumn(ColumnCoord colCoord)
    {
        columnsToBuild.Add(colCoord);
    }

    public void BuildNextColumn()
    {
        ColumnCoord currentViewedColumnCoord = columnsToBuild[0];
        columnsToBuild.RemoveAt(0);
        GameObject newColumn = Instantiate(columnPrefab, new Vector3(currentViewedColumnCoord.x * ChunkColumn.chunkSize * ChunkColumn.blockSize, 0, currentViewedColumnCoord.z * ChunkColumn.chunkSize * ChunkColumn.blockSize), Quaternion.identity);
        newColumn.transform.parent = transform;
        newColumn.name = "aChunkColumn " + currentViewedColumnCoord.x + " " + currentViewedColumnCoord.z;
        ChunkColumn chunkColScript = newColumn.GetComponent<ChunkColumn>();
        columns.Add(currentViewedColumnCoord, chunkColScript);
        chunkColScript.Init(currentViewedColumnCoord);
        chunkColScript.StartGenerating();
    }

    public static byte GetBlock(int x, int y, int z)
    {
        ColumnCoord colCoord = GetColumnCoord(x, z);
        try
        {
            ChunkColumn chunkCol = null;
            if (columns.TryGetValue(colCoord, out chunkCol))
            {
                if(chunkCol.hasMapData)
                {
                    return chunkCol.GetBlock(x - colCoord.x * ChunkColumn.chunkSize, y, z - colCoord.z * ChunkColumn.chunkSize);
                }
            }
        }
        catch(Exception e)
        {
            Debug.Log("Chunk error!! " + e);
        }

        return 0;
    }

    public static void SetBlock(int x, int y, int z, byte blockID)
    {
        ColumnCoord colCoord = GetColumnCoord(x, z);
        try
        {
            ChunkColumn chunkCol = null;
            if (columns.TryGetValue(colCoord, out chunkCol))
            {
                if (chunkCol.hasMapData)
                {
                    chunkCol.SetBlock(x - colCoord.x * ChunkColumn.chunkSize, y, z - colCoord.z * ChunkColumn.chunkSize, blockID);
                }
                else
                {
                    Debug.LogError("Block set on chunk without data");
                }
            }
            else
            {
                Debug.LogError("Block set on non existing chunk");
            }
        }
        catch (Exception e)
        {
            Debug.Log("Set Block chunk error " + e);
        }
    }

    public static ChunkColumn GetColumn(ColumnCoord colCoord)
    {
        if (columns.ContainsKey(colCoord))
        {
            // Chunk exists
            return columns[colCoord];
        }

        return null;
    }

    public static bool ColumnHasMapdata(ColumnCoord colCoord)
    {
        ChunkColumn chunkCol = GetColumn(colCoord);

        if(chunkCol != null)
        {
            return chunkCol.hasMapData;
        }

        return false;
    }

    public static ColumnCoord GetColumnCoord(int x, int z)
    {
        ColumnCoord colCoord = new ColumnCoord(Mathf.FloorToInt((float)x / ChunkColumn.chunkSize), Mathf.FloorToInt((float)z / ChunkColumn.chunkSize));
        return colCoord;
    }
}

[System.Serializable]
public struct ColumnCoord
{
    public int x, z;

    public ColumnCoord(int x, int z)
    {
        this.x = x;
        this.z = z;
    }

    public ColumnCoord GetRelativePos(int x, int z)
    {
        return new ColumnCoord(this.x + x, this.z + z);
    }

    public float GetSqrDistance(ColumnCoord colCoord)
    {
        return (x - colCoord.x) * (x - colCoord.x) + (z - colCoord.z) * (z - colCoord.z);
    }
}