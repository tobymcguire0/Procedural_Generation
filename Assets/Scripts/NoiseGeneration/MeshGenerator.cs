using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class MeshGenerator
{
    
    public static MeshData GenerateTerrainMesh(float[,] heightMap,MeshSettings meshSettings, int LOD)
    {    
        int meshSimplificationIncrement = (LOD == 0) ? 1 : LOD * 2;

        int borderedSize = heightMap.GetLength(0);
        int meshSize = borderedSize - 2*meshSimplificationIncrement;
        int meshSizeUnsimplified = borderedSize - 2;

        float topLeftX = (meshSizeUnsimplified - 1) * -0.5f;
        float topLeftZ = (meshSizeUnsimplified - 1) * 0.5f;

        //If the LOD is 0 then set mSI to 1, otherwise set mSI to 2*LOD
        
        int verticiesPerLine = ((meshSize - 1) / meshSimplificationIncrement) + 1;
        MeshData meshData = new MeshData(verticiesPerLine,meshSettings.useFlatShading);

        int[,] vertexIndiciesMap = new int[borderedSize, borderedSize];
        int meshVertexIndex = 0;
        int borderedVertexIndex = -1;

        for (int x = 0; x < borderedSize; x += meshSimplificationIncrement)
        {
            for (int y = 0; y < borderedSize; y += meshSimplificationIncrement)
            {
                bool isBorderVertex = y == 0 || y == borderedSize - 1 || x == 0 || x == borderedSize - 1;
                if (isBorderVertex)
                {
                    vertexIndiciesMap[x, y] = borderedVertexIndex;
                    borderedVertexIndex--;
                }
                else
                {
                    vertexIndiciesMap[x, y] = meshVertexIndex;
                    meshVertexIndex++;
                }
            }
        }

        for (int x = 0; x<borderedSize; x+= meshSimplificationIncrement)
        {
            for(int y = 0; y<borderedSize; y+= meshSimplificationIncrement)
            {
                int vertexIndex = vertexIndiciesMap[x, y];
                Vector2 percent = new Vector2((x - meshSimplificationIncrement) / (float)meshSize, (y - meshSimplificationIncrement) / (float)meshSize);
                float height = heightMap[x, y];
                Vector3 vertexPosition = new Vector3((topLeftX+percent.x* meshSizeUnsimplified)*meshSettings.meshScale, height , (topLeftZ-percent.y* meshSizeUnsimplified) * meshSettings.meshScale);

                meshData.AddVertex(vertexPosition, percent, vertexIndex);

                //Ignore right/bottom edges of the map
                if(x<borderedSize-1 && y < borderedSize - 1)
                {
                    //Adding the indexes of the points that create the two triangles for that square
                    int a = vertexIndiciesMap[x, y];
                    int b = vertexIndiciesMap[x+meshSimplificationIncrement, y];
                    int c = vertexIndiciesMap[x, y+ meshSimplificationIncrement];
                    int d = vertexIndiciesMap[x+ meshSimplificationIncrement, y+ meshSimplificationIncrement];
                    meshData.AddTriangle(a,d,c);
                    meshData.AddTriangle(d,a,b);
                }

                vertexIndex++;
            }
        }
        //Finalizing mesh data in the thread instead of the main game thread later on
        meshData.FinalizeMesh();

        //Returning the meshData instead of the Mesh to help with threading
        return meshData;
    }
}

public class MeshData
{
    //Data of all the verticies of the mesh
    Vector3[] vertices;

    //1D triangle array that stores vertex data to form triangles
    //Ex. (0,3,2 , 3,0,1) are two triangles in a 2x2 matrix that create one square
    int[] triangles;

    //UVs used to handle textures. Contains the percentages of each vertex
    Vector2[] uvs;


    Vector3[] borderVertices;
    int[] borderTriangles;

    //The current index of the triangles array
    int triangleIndex;
    int borderTriangleIndex;


    Vector3[] bakedNormals;
    bool useFlatShading;

    public MeshData(int verticesPerLine, bool flatShading)
    {
        useFlatShading = flatShading;
        vertices = new Vector3[verticesPerLine*verticesPerLine];
        uvs = new Vector2[verticesPerLine * verticesPerLine];
        triangles = new int[(verticesPerLine - 1) * (verticesPerLine - 1) * 6];

        borderVertices = new Vector3[verticesPerLine * 4 + 4];
        borderTriangles = new int[verticesPerLine * 24];

    }
    //Defining a vertex as either a border vertex or a useable vertex with a uv
    public void AddVertex(Vector3 vertexPos, Vector2 uv, int vertexInd)
    {
        if (vertexInd < 0)
        {
            borderVertices[-vertexInd - 1] = vertexPos;
        } else
        {
            vertices[vertexInd] = vertexPos;
            uvs[vertexInd] = uv;
        }
    }

    //Puts the points together to create a triangle
    public void AddTriangle(int a, int b,int c)
    {
        if(a<0 || b<0 || c < 0) //Triangles on the unrendered border
        {
            borderTriangles[borderTriangleIndex] = a;
            borderTriangles[borderTriangleIndex + 1] = b;
            borderTriangles[borderTriangleIndex + 2] = c;
            borderTriangleIndex += 3;
        }
        else //Rendered triangles
        {
            triangles[triangleIndex] = a;
            triangles[triangleIndex + 1] = b;
            triangles[triangleIndex + 2] = c;
            triangleIndex += 3;
        }
        

    }

    Vector3[] CalculateNormals()
    {
        Vector3[] vertexNormals = new Vector3[vertices.Length];
        int triangleCount = triangles.Length / 3;
        for(int i = 0; i < triangleCount; i++)
        {
            int normalTraingleIndex = i * 3;
            int vertexIndexA = triangles[normalTraingleIndex];
            int vertexIndexB = triangles[normalTraingleIndex+1];
            int vertexIndexC = triangles[normalTraingleIndex+2];

            Vector3 triangleNormal = SurfaceNormalFromIndices(vertexIndexA, vertexIndexB, vertexIndexC);

            vertexNormals[vertexIndexA] += triangleNormal;
            vertexNormals[vertexIndexB] += triangleNormal;
            vertexNormals[vertexIndexC] += triangleNormal;

        }

        int borderTriangleCount = borderTriangles.Length / 3;
        for (int i = 0; i < borderTriangleCount; i++)
        {
            int normalTraingleIndex = i * 3;
            int vertexIndexA = borderTriangles[normalTraingleIndex];
            int vertexIndexB = borderTriangles[normalTraingleIndex + 1];
            int vertexIndexC = borderTriangles[normalTraingleIndex + 2];

            Vector3 triangleNormal = SurfaceNormalFromIndices(vertexIndexA, vertexIndexB, vertexIndexC);
            if (vertexIndexA >= 0)
            {
                vertexNormals[vertexIndexA] += triangleNormal;
            }
            if (vertexIndexB >= 0)
            {
                vertexNormals[vertexIndexB] += triangleNormal;
            }
            if (vertexIndexC >= 0)
            {
                vertexNormals[vertexIndexC] += triangleNormal;
            }

        }

        for (int i = 0; i<vertexNormals.Length; i++)
        {
            vertexNormals[i].Normalize();
        }
        return vertexNormals;
    }

    //Using the cross product to get vectors normal to the surface
    Vector3 SurfaceNormalFromIndices(int indexA, int indexB, int indexC)
    {
        Vector3 pointA = (indexA<0)?borderVertices[-indexA-1]:vertices[indexA];
        Vector3 pointB = (indexB < 0) ? borderVertices[-indexB - 1] : vertices[indexB];
        Vector3 pointC = (indexC < 0) ? borderVertices[-indexC - 1] : vertices[indexC];

        Vector3 sideAB = pointB - pointA;
        Vector3 sideAC = pointC - pointA;

        return Vector3.Cross(sideAB, sideAC).normalized;
    }

    public void FinalizeMesh()
    {
        if (useFlatShading)
        {
            FlatShading();
        } else
        {
            BakeNormals();
        }
    }
    void BakeNormals()
    {
        bakedNormals = CalculateNormals();
    }


    void FlatShading()
    {
        Vector2[] flatShadedUvs = new Vector2[triangles.Length];
        Vector3[] flatShadedVertices = new Vector3[triangles.Length];

        for(int i = 0; i< triangles.Length; i++)
        {
            flatShadedUvs[i] = uvs[triangles[i]];
            flatShadedVertices[i] = vertices[triangles[i]];
            triangles[i] = i;
        }

        vertices = flatShadedVertices;
        uvs = flatShadedUvs;
    }

    public Mesh CreateMesh()
    {
        Mesh mesh = new Mesh();
        mesh.vertices = vertices;
        mesh.triangles = triangles;
        mesh.uv = uvs;
        if (useFlatShading)
        {
            mesh.RecalculateNormals();
        }
        else
        {
            mesh.normals = bakedNormals;
        }
        return mesh;
    }
}
