using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class WorldBlockEditor : MonoBehaviour {

	
	void Update () {
		if(Input.GetMouseButtonDown(0))
        {
            Ray ray = new Ray(transform.position, transform.forward);

            RaycastHit hit;

            if(Physics.Raycast(ray, out hit))
            {
                //0.5 is half the blocksize
                Vector3 hitPos = hit.point - hit.normal * 0.5f * ChunkColumn.blockSize;
                Vector3 offset = Vector3.one * (ChunkColumn.chunkSize / 2) * ChunkColumn.blockSize;
                hitPos += offset;

                hitPos /= ChunkColumn.blockSize;

                int x = Mathf.FloorToInt(hitPos.x);
                int y = Mathf.FloorToInt(hitPos.y);
                int z = Mathf.FloorToInt(hitPos.z);

                World.SetBlock(x, y, z, 0);
            }
        }

        if (Input.GetMouseButtonDown(1))
        {
            Ray ray = new Ray(transform.position, transform.forward);

            RaycastHit hit;

            if (Physics.Raycast(ray, out hit))
            {
                //0.5 is half the blocksize
                Vector3 hitPos = hit.point + hit.normal * 0.5f;
                Vector3 offset = Vector3.one * (ChunkColumn.chunkSize / 2);
                hitPos += offset;

                int x = Mathf.FloorToInt(hitPos.x);
                int y = Mathf.FloorToInt(hitPos.y);
                int z = Mathf.FloorToInt(hitPos.z);

                World.SetBlock(x, y, z, 1);
            }
        }
    }
}
