using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class CustomPerlinNoise
{
    public static float PerlinNoise(float x, float y, float z = 0f)
    {
        // Gridposition locked to 0 - 255 because of permutation array length ( & 255 only takes last 8 bits which range 0 - 255)
        int PosGrid_x = Mathf.FloorToInt(x) & 255;
        int PosGrid_y = Mathf.FloorToInt(y) & 255;
        int PosGrid_z = Mathf.FloorToInt(z) & 255;

        // Position within unit square
        float localPos_x = x - Mathf.Floor(x);
        float localPos_y = y - Mathf.Floor(y);
        float localPos_z = z - Mathf.Floor(z);

        // Faded coords to prevent liniear interpolation when lerping
        float faded_x = Fade(localPos_x);
        float faded_y = Fade(localPos_y);
        float faded_z = Fade(localPos_z);

        // Setting hash value for the 8 gridpoints using the permutation table (pseudorandom)
        // a means first point and b the second in the dimension (first a/b is x dimension)

        int aaa, aba, aab, abb, baa, bba, bab, bbb;
        aaa = permutationTable[permutationTable[permutationTable[PosGrid_x] + PosGrid_y] + PosGrid_z];
        aba = permutationTable[permutationTable[permutationTable[PosGrid_x] + PosGrid_y + 1] + PosGrid_z];
        aab = permutationTable[permutationTable[permutationTable[PosGrid_x] + PosGrid_y] + PosGrid_z + 1];
        abb = permutationTable[permutationTable[permutationTable[PosGrid_x] + PosGrid_y + 1] + PosGrid_z + 1];
        baa = permutationTable[permutationTable[permutationTable[PosGrid_x + 1] + PosGrid_y] + PosGrid_z];
        bba = permutationTable[permutationTable[permutationTable[PosGrid_x + 1] + PosGrid_y + 1] + PosGrid_z];
        bab = permutationTable[permutationTable[permutationTable[PosGrid_x + 1] + PosGrid_y] + PosGrid_z + 1];
        bbb = permutationTable[permutationTable[permutationTable[PosGrid_x + 1] + PosGrid_y + 1] + PosGrid_z + 1];


        float x1, x2, y1, y2;
        x1 = Lerp(grad(aaa, localPos_x, localPos_y, localPos_z),
                    grad(baa, localPos_x - 1, localPos_y, localPos_z),
                    faded_x);
        x2 = Lerp(grad(aba, localPos_x, localPos_y - 1, localPos_z),
                    grad(bba, localPos_x - 1, localPos_y - 1, localPos_z),
                      faded_x);
        y1 = Lerp(x1, x2, faded_y);

        x1 = Lerp(grad(aab, localPos_x, localPos_y, localPos_z - 1),
                grad(bab, localPos_x - 1, localPos_y, localPos_z - 1),
                faded_x);
        x2 = Lerp(grad(abb, localPos_x, localPos_y - 1, localPos_z - 1),
                      grad(bbb, localPos_x - 1, localPos_y - 1, localPos_z - 1),
                      faded_x);
        y2 = Lerp(x1, x2, faded_y);

        return (Lerp(y1, y2, faded_z) + 1) / 2;
    }

    private static readonly int[] permutation = { 151,160,137,91,90,15,                 // Hash lookup table as defined by Ken Perlin.  This is a randomly
    131,13,201,95,96,53,194,233,7,225,140,36,103,30,69,142,8,99,37,240,21,10,23,    // arranged array of all numbers from 0-255 inclusive.
    190, 6,148,247,120,234,75,0,26,197,62,94,252,219,203,117,35,11,32,57,177,33,
    88,237,149,56,87,174,20,125,136,171,168, 68,175,74,165,71,134,139,48,27,166,
    77,146,158,231,83,111,229,122,60,211,133,230,220,105,92,41,55,46,245,40,244,
    102,143,54, 65,25,63,161, 1,216,80,73,209,76,132,187,208, 89,18,169,200,196,
    135,130,116,188,159,86,164,100,109,198,173,186, 3,64,52,217,226,250,124,123,
    5,202,38,147,118,126,255,82,85,212,207,206,59,227,47,16,58,17,182,189,28,42,
    223,183,170,213,119,248,152, 2,44,154,163, 70,221,153,101,155,167, 43,172,9,
    129,22,39,253, 19,98,108,110,79,113,224,232,178,185, 112,104,218,246,97,228,
    251,34,242,193,238,210,144,12,191,179,162,241, 81,51,145,235,249,14,239,107,
    49,192,214, 31,181,199,106,157,184, 84,204,176,115,121,50,45,127, 4,150,254,
    138,236,205,93,222,114,67,29,24,72,243,141,128,195,78,66,215,61,156,180
    };

    private static readonly int[] permutationTable;

    static CustomPerlinNoise()
    {
        permutationTable = new int[512];
        for (int i = 0; i < 512; i++)
        {
            permutationTable[i] = permutation[i % 256];
        }
    }

    private static float Lerp(float x, float y, float w)
    {
        return x + w * (y - x);
    }

    private static float Fade(float t)
    {
        return t * t * t * (t * (t * 6 - 15) + 10);
    }

    public static float grad(int hash, float x, float y, float z)
    {
        switch (hash & 0xF)
        {
            case 0x0: return x + y;
            case 0x1: return -x + y;
            case 0x2: return x - y;
            case 0x3: return -x - y;
            case 0x4: return x + z;
            case 0x5: return -x + z;
            case 0x6: return x - z;
            case 0x7: return -x - z;
            case 0x8: return y + z;
            case 0x9: return -y + z;
            case 0xA: return y - z;
            case 0xB: return -y - z;
            case 0xC: return y + x;
            case 0xD: return -y + z;
            case 0xE: return y - x;
            case 0xF: return -y - z;
            default: return 0; // never happens
        }
    }
}
