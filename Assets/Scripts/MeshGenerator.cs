using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MeshGenerator : MonoBehaviour
{
    public static MeshData GenerateTerrainMesh(float[,] heightMap, float heightMultiplier, AnimationCurve meshHeightCurve, int levelOfDetailReduction)
    {
        AnimationCurve heighCurve = new AnimationCurve(meshHeightCurve.keys);
        
        int width = heightMap.GetLength(0);
        int height = heightMap.GetLength(1);
        
        // alignement sur système avec 0 au centre
        float topLeftX = (width - 1) / -2f;
        float topLeftZ = (height - 1) / 2f;

        // le niveau de réduction de précision : 
        // pas de 0 => 1
        // de 2 à 12 par paire
        int meshSimplification = (levelOfDetailReduction == 0) ? 1 : levelOfDetailReduction * 2;
        int verticesPerLine = (width - 1) / meshSimplification + 1;

        MeshData meshData = new MeshData(verticesPerLine, verticesPerLine);
        int vertexIndex = 0;

        for (int y = 0; y < height; y+= meshSimplification)
        {
            for (int x = 0; x < width; x+= meshSimplification)
            {                
                meshData.vertices[vertexIndex] = new Vector3(topLeftX + x, heighCurve.Evaluate(heightMap[x, y]) * heightMultiplier, topLeftZ - y);               

                meshData.uvs[vertexIndex] = new Vector2(x / (float)width, y / (float)height);

                // enregistrement des triangles

                // on ignore les vertices tout à droite et en bas
                if (x < width - 1 && y < height - 1)
                {
                    meshData.AddTriangle(vertexIndex, vertexIndex + verticesPerLine + 1, vertexIndex + verticesPerLine);
                    meshData.AddTriangle(vertexIndex + verticesPerLine + 1, vertexIndex, vertexIndex + 1);
                }

                vertexIndex++;
            }
        }

        return meshData;
    }
}

public class MeshData
{
    public Vector3[] vertices;
    public int[] triangles;
    public Vector2[] uvs;

    private int triangleIndex;

    public MeshData(int meshWidth, int meshHeight)
    {
        vertices = new Vector3[meshWidth * meshHeight];
        uvs = new Vector2[meshWidth * meshHeight];
        triangles = new int[(meshWidth - 1) * (meshHeight - 1) * 6];
    }

    public void AddTriangle(int a, int b, int c)
    {
        // pour être sûr qu'ils se suivent dans l'ordre
        triangles[triangleIndex] = a;
        triangles[triangleIndex+1] = b;
        triangles[triangleIndex+2] = c;

        triangleIndex += 3;
    }

    public Mesh CreateMesh()
    {
        Mesh mesh = new Mesh
        {
            vertices = vertices,
            triangles = triangles,
            uv = uvs
        };
        mesh.RecalculateNormals();
        return mesh;
    }
}