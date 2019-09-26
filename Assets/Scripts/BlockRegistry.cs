using System.Collections;
using System.Collections.Generic;
using UnityEngine;


// Singleton
public class BlockRegistry : MonoBehaviour {

    public static BlockRegistry instance = null;

    public List<Block> blocks;

    private void Awake()
    {
        //Singleton pattern implementation
        if (instance == null)
            instance = this;
        else if (instance != this)
            Destroy(gameObject);
    }
}

[System.Serializable]
public struct Block
{
    public string name;
    public Gradient blockColor;
}