using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading;
using UnityEngine;
using System.Linq;

public class ThreadPoolGen : MonoBehaviour
{
    public Queue<ThreadMeshInfo> threadMeshQueue;
    public Queue<ThreadMapInfo> threadMapQueue;

    public int maxAmountOfThreads = 8;
    public int amountOfWorkerThreads;

    void Awake()
    {
        threadMeshQueue = new Queue<ThreadMeshInfo>();
        threadMapQueue = new Queue<ThreadMapInfo>();
    }

    void Update()
    {
        if (threadMeshQueue.Count > 0)
        {
            for (int i = 0; i < threadMeshQueue.Count; i++)
            {
                ThreadMeshInfo threadInfo = threadMeshQueue.Dequeue();
                threadInfo.callback(threadInfo.chunkData);
                amountOfWorkerThreads--;
            }
        }

        if (threadMapQueue.Count > 0)
        {
            for (int i = 0; i < threadMapQueue.Count; i++)
            {
                ThreadMapInfo threadInfo = threadMapQueue.Dequeue();
                threadInfo.callback(threadInfo.chunkMapData);
                amountOfWorkerThreads--;
            }
        }
    }

    public void RequestMeshData(System.Action<ChunkMeshData[]> callback, ChunkColumn chunkCol, int yIndex)
    {
        ThreadMeshObject threadTask = new ThreadMeshObject(callback, chunkCol, yIndex);
        ThreadPool.QueueUserWorkItem(new WaitCallback(MeshDataThread), threadTask);
        amountOfWorkerThreads++;
    }

    public void MeshDataThread(object threadTask)
    {
        ThreadMeshObject threadTaskInfo = (ThreadMeshObject)threadTask;
        try
        {
            ChunkMeshData[] chunkMeshData = World.meshGen.GenerateChunkMeshData(threadTaskInfo.chunkCol, threadTaskInfo.yIndex);
            ThreadMeshInfo threadInfo = new ThreadMeshInfo(threadTaskInfo.callback, chunkMeshData);

            lock (threadMeshQueue)
            {
                threadMeshQueue.Enqueue(threadInfo);
            }
        }
        catch
        {
            Debug.Log("Chunk Mesh Error Occured!");
        }
    }

    public void RequestMapData(System.Action<ChunkColumn.MapDataInfo> callback, ColumnCoord colCoord)
    {
        ThreadMapObject threadTask = new ThreadMapObject(callback, colCoord);
        ThreadPool.QueueUserWorkItem(new WaitCallback(MapDataThread), threadTask);
        amountOfWorkerThreads++;
    }

    public void MapDataThread(object threadTask)
    {
        ThreadMapObject threadTaskInfo = (ThreadMapObject)threadTask;

        ChunkColumn.MapDataInfo chunkMapData = World.perlinMapGen.GenerateNoiseMap(threadTaskInfo.colCoord);
        ThreadMapInfo threadInfo = new ThreadMapInfo(threadTaskInfo.callback, chunkMapData);
        lock (threadMapQueue)
        {
            threadMapQueue.Enqueue(threadInfo);
        }
    }

    public struct ThreadMeshInfo
    {
        public readonly System.Action<ChunkMeshData[]> callback;
        public readonly ChunkMeshData[] chunkData;

        public ThreadMeshInfo(System.Action<ChunkMeshData[]> callback, ChunkMeshData[] chunkData)
        {
            this.callback = callback;
            this.chunkData = chunkData;
        }
    }

    public struct ThreadMapInfo
    {
        public readonly System.Action<ChunkColumn.MapDataInfo> callback;
        public readonly ChunkColumn.MapDataInfo chunkMapData;

        public ThreadMapInfo(System.Action<ChunkColumn.MapDataInfo> callback, ChunkColumn.MapDataInfo chunkMapData)
        {
            this.callback = callback;
            this.chunkMapData = chunkMapData;
        }
    }

    public class ThreadMeshObject
    {
        public System.Action<ChunkMeshData[]> callback;
        public ChunkColumn chunkCol;
        public int yIndex;

        public ThreadMeshObject(System.Action<ChunkMeshData[]> callback, ChunkColumn chunkCol, int yIndex)
        {
            this.callback = callback;
            this.chunkCol = chunkCol;
            this.yIndex = yIndex;
        }
    }

    public class ThreadMapObject
    {
        public System.Action<ChunkColumn.MapDataInfo> callback;
        public ColumnCoord colCoord;

        public ThreadMapObject(System.Action<ChunkColumn.MapDataInfo> callback, ColumnCoord colCoord)
        {
            this.callback = callback;
            this.colCoord = colCoord;
        }
    }
}
