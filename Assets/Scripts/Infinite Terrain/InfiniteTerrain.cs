using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class InfiniteTerrain : MonoBehaviour
{

    const float playerMoveThresholdForChunkUpdate = 25f;
    const float sqrPlayerMoveThresholdForChunkUpdate = playerMoveThresholdForChunkUpdate* playerMoveThresholdForChunkUpdate;
    const float colliderGenerationDistanceThreshold = 5f;
    public Transform viewer;

    public Material mapMaterial;
    public LODInfo[] detailLevels;
    public static float maxViewDistance;
    public int colliderLevel;



    public static Vector2 viewerPosition;
    Vector2 viewerPositionOld;
    static GenerateMap mapGenerator;
    float meshWorldSize;
    int chunksVisible;

    Dictionary<Vector2, TerrainChunk> chunkDictionary = new Dictionary<Vector2, TerrainChunk>();
    static List<TerrainChunk> visibleTerrainChunks = new List<TerrainChunk>();
    
#if UNITY_EDITOR
    private void OnValidate()
    {
        if (colliderLevel > detailLevels.Length - 1)
        {
            colliderLevel = detailLevels.Length - 1;
        } else if(colliderLevel < 0)
        {
            colliderLevel = 0;
        }
    }
#endif

    private void Start()
    {
        mapGenerator = FindObjectOfType<GenerateMap>();
        meshWorldSize = mapGenerator.meshSettings.meshWorldSize;
        maxViewDistance = detailLevels[detailLevels.Length - 1].visibleDistanceThreshold;
        chunksVisible = Mathf.RoundToInt(maxViewDistance / meshWorldSize);
        
        UpdateVisibleChunks();
    }

    private void Update()
    {
        if(viewerPosition != viewerPositionOld)
        {
            foreach(TerrainChunk chunk in visibleTerrainChunks)
            {
                chunk.UpdateCollisionMesh();
            }
        }
        //Constantly get the updated position of the viewer and update chunks
        viewerPosition = new Vector2(viewer.position.x, viewer.position.z);
        if ((viewerPositionOld - viewerPosition).sqrMagnitude > sqrPlayerMoveThresholdForChunkUpdate)
        {
            viewerPositionOld = viewerPosition;
            UpdateVisibleChunks();
        }
    }

    void UpdateVisibleChunks()
    {
        HashSet<Vector2> alreadyUpdatedChunkCoords = new HashSet<Vector2>();
        //Updating the chunks (including its visibility)
        //Going in reverse because of the possibility UpdateChunk removes a chunk from the list
        for(int i = visibleTerrainChunks.Count-1; i>= 0; i--)
        {
            alreadyUpdatedChunkCoords.Add(visibleTerrainChunks[i].coordinate);
            visibleTerrainChunks[i].UpdateChunk();
        }

        //Get the position of the current chunk in terms of a chunk grid, not the unity grid
        int currentChunkCoordX = Mathf.RoundToInt(viewerPosition.x / meshWorldSize);
        int currentChunkCoordY = Mathf.RoundToInt(viewerPosition.y / meshWorldSize);
        //Looping through every chunk that has the potential to be within the view distance
        for(int xOffset = -chunksVisible; xOffset<=chunksVisible; xOffset++)
        {
            for (int yOffset = -chunksVisible; yOffset <= chunksVisible; yOffset++)
            {
                //Getting the current selected chunk's coordinates
                Vector2 viewedChunkCoord = new Vector2(currentChunkCoordX + xOffset, currentChunkCoordY + yOffset);


                //Only Updating the chunk if it hasn't already been updated in the previous loop
                if (!alreadyUpdatedChunkCoords.Contains(viewedChunkCoord))
                {
                    //If the selected chunk was already created, update it and if it is visible store it to disable it next update
                    if (chunkDictionary.ContainsKey(viewedChunkCoord))
                    {
                        chunkDictionary[viewedChunkCoord].UpdateChunk();
                    }
                    else //If the chunk has not been created before, create it.
                    {
                        chunkDictionary.Add(viewedChunkCoord, new TerrainChunk(viewedChunkCoord, detailLevels, meshWorldSize, transform, mapMaterial, colliderLevel));
                    }
                }
                

            }
        }

    }

    

    public class TerrainChunk
    {
        GameObject meshObject;
        Vector2 sampleCenter;
        Bounds bounds;
        MeshRenderer meshRenderer;
        MeshFilter meshFilter;
        LODInfo[] detailLevels;
        LODMesh[] detailMeshes;

        LODMesh collisionLODMesh;
        MeshCollider meshCollider;
        bool hasSetCollider;
        int colliderLevel;

        HeightMap mapData;
        bool mapDataRecieved;

        public Vector2 coordinate;
        

        int prevLodIndex = -1;

        public TerrainChunk(Vector2 coord, LODInfo[] detail, float meshWorldSize, Transform parent, Material material, int colLevel)
        {
            //Initializing the position and view bounds of the chunk
            sampleCenter = coord*meshWorldSize/mapGenerator.meshSettings.meshScale;
            Vector2 position = coord * meshWorldSize;
            bounds = new Bounds(sampleCenter, Vector2.one*meshWorldSize);
            
            coordinate = coord;

            colliderLevel = colLevel;
            

            //Initializing the information about the different LOD values
            detailLevels = detail;

            //Creating the chunk mesh and deactivating it by default
            meshObject = new GameObject("Terrain Chunk");
            meshRenderer = meshObject.AddComponent<MeshRenderer>();
            meshFilter = meshObject.AddComponent<MeshFilter>();
            meshRenderer.material = material;
            meshCollider = meshObject.AddComponent<MeshCollider>();
            meshObject.transform.position = new Vector3(position.x,0,position.y);
            meshObject.transform.parent = parent;
            setVisible(false);

            //Initializing the different meshes for each LOD
            detailMeshes = new LODMesh[detailLevels.Length];
            for(int i = 0; i<detailLevels.Length; i++)
            {
                detailMeshes[i] = new LODMesh(detailLevels[i].LOD);
                detailMeshes[i].updateCallback += UpdateChunk;
                if (i == colliderLevel)
                {
                    detailMeshes[i].updateCallback += UpdateCollisionMesh;
                }
                
            }

            //Start a thread for getting map data
            mapGenerator.RequestHeightMap(sampleCenter,OnHeightMapRecieved);
        }

        public void UpdateChunk()
        {
            //Make sure the chunk has map data before updating
            if (mapDataRecieved)
            {
                //Only make the chunk visible if it is within the maximum view distance
                float viewerDstFromNearestEdge = Mathf.Sqrt(bounds.SqrDistance(viewerPosition));

                bool wasVisible = isVisible();

                bool visible = viewerDstFromNearestEdge <= maxViewDistance;

                if (visible)
                {
                    //Assigning the current chunk to a LOD based on how far it is away from the viewer
                    int lodIndex = 0;
                    for (int i = 0; i < detailLevels.Length - 1; i++)
                    {
                        if (viewerDstFromNearestEdge > detailLevels[i].visibleDistanceThreshold) //LOD too small
                        {
                            lodIndex = i + 1;
                        }
                        else //Correct LOD
                        {
                            break;
                        }
                    }
                    //If the LOD is different from the last update, change the mesh to represent the current LOD
                    if (lodIndex != prevLodIndex)
                    {
                        LODMesh lodMesh = detailMeshes[lodIndex];
                        if (lodMesh.hasMesh)
                        {
                            prevLodIndex = lodIndex;
                            meshFilter.mesh = lodMesh.mesh;
                        }
                        else if (!lodMesh.hasRequestedMesh)
                        {
                            lodMesh.RequestMesh(mapData);
                        }
                    }
                }
                if (wasVisible != visible)
                {
                    if (visible)
                    {
                        visibleTerrainChunks.Add(this);
                    } else
                    {
                        visibleTerrainChunks.Remove(this);
                    }
                }
                //Change the visibility of the chunk
                setVisible(visible);
            }
            
        }

        public void UpdateCollisionMesh()
        {
            float sqDstFromViewerToEdge = bounds.SqrDistance(viewerPosition);
            if (!hasSetCollider)
            {
                if (sqDstFromViewerToEdge < detailLevels[colliderLevel].sqrVisibleDstThreshold)
                {
                    if (!detailMeshes[colliderLevel].hasRequestedMesh)
                    {
                        detailMeshes[colliderLevel].RequestMesh(mapData);
                    }
                }
            }
            

            if(sqDstFromViewerToEdge< colliderGenerationDistanceThreshold * colliderGenerationDistanceThreshold)
            {
                if (detailMeshes[colliderLevel].hasMesh)
                {
                    meshCollider.sharedMesh = detailMeshes[colliderLevel].mesh;
                    hasSetCollider = true;
                }
            }
        }

        //Callback when mapGenerator returns matData, updates the chunk
        void OnHeightMapRecieved(HeightMap mapData)
        {
            this.mapData = mapData;
            mapDataRecieved = true;

            UpdateChunk();
        }

        //Callback when mapGenerator returns meshData
        void OnMeshDataRecieved(MeshData meshData)
        {
            meshFilter.mesh = meshData.CreateMesh();
        }

        public void setVisible(bool visible)
        {
            meshObject.SetActive(visible);
        }

        public bool isVisible()
        {
            return meshObject.activeSelf;
        }
    }

    //Stores different LOD meshes
    class LODMesh
    {
        public Mesh mesh;
        public bool hasRequestedMesh;
        public bool hasMesh;
        
        int lod;
        public event System.Action updateCallback;

        public LODMesh(int lod)
        {
            this.lod = lod;
        }

        //Callback from mapGeneratro when a meshData is recieved
        void OnMeshDataRecieved(MeshData meshData)
        {
            mesh = meshData.CreateMesh();
            hasMesh = true;
            //Updates the chunks when a mesh is recieved
            updateCallback();
        }

        //Requests a mesh from mapGenerator on a new thread
        public void RequestMesh(HeightMap mapData)
        {
            hasRequestedMesh = true;
            mapGenerator.RequestMeshData(mapData, lod,OnMeshDataRecieved);
        }
    }
    [System.Serializable]
    public struct LODInfo
    {
        [Range(0, MeshSettings.levelOfSupportedLOD - 1)]
        public int LOD;
        public float visibleDistanceThreshold;

        public float sqrVisibleDstThreshold
        {
            get
            {
                return visibleDistanceThreshold * visibleDistanceThreshold;
            }
        }
    }
}
