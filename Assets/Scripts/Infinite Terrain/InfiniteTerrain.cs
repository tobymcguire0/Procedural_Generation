using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class InfiniteTerrain : MonoBehaviour
{

    const float playerMoveThresholdForChunkUpdate = 25f;
    const float sqrPlayerMoveThresholdForChunkUpdate = playerMoveThresholdForChunkUpdate* playerMoveThresholdForChunkUpdate;
    public Transform viewer;

    public Material mapMaterial;
    public LODInfo[] detailLevels;
    public static float maxViewDistance;



    public static Vector2 viewerPosition;
    Vector2 viewerPositionOld;
    static GenerateMap mapGenerator;
    int chunkSize;
    int chunksVisible;

    Dictionary<Vector2, TerrainChunk> chunkDictionary = new Dictionary<Vector2, TerrainChunk>();
    static List<TerrainChunk> chunksVisibleLastUpdate = new List<TerrainChunk>();

    private void Start()
    {
        mapGenerator = FindObjectOfType<GenerateMap>();
        chunkSize = mapGenerator.mapChunkSize - 1;
        maxViewDistance = detailLevels[detailLevels.Length - 1].visibleDistanceThreshold;
        chunksVisible = Mathf.RoundToInt(maxViewDistance / chunkSize);
        
        UpdateVisibleChunks();
    }

    private void Update()
    {
        
        //Constantly get the updated position of the viewer and update chunks
        viewerPosition = new Vector2(viewer.position.x, viewer.position.z)/mapGenerator.terrainData.uniformScale;
        if ((viewerPositionOld - viewerPosition).sqrMagnitude > sqrPlayerMoveThresholdForChunkUpdate)
        {
            viewerPositionOld = viewerPosition;
            UpdateVisibleChunks();
        }
    }

    void UpdateVisibleChunks()
    {
        Debug.Log("Updating Chunks");
        //Disable every previously viewed chunk every update
        for(int i = 0; i<chunksVisibleLastUpdate.Count; i++)
        {
            chunksVisibleLastUpdate[i].setVisible(false);
        }
        chunksVisibleLastUpdate.Clear();
        //Get the position of the current chunk in terms of a chunk grid, not the unity grid
        int currentChunkCoordX = Mathf.RoundToInt(viewerPosition.x / chunkSize);
        int currentChunkCoordY = Mathf.RoundToInt(viewerPosition.y / chunkSize);
        //Looping through every chunk that has the potential to be within the view distance
        for(int xOffset = -chunksVisible; xOffset<=chunksVisible; xOffset++)
        {
            for (int yOffset = -chunksVisible; yOffset <= chunksVisible; yOffset++)
            {
                //Getting the current selected chunk's coordinates
                Vector2 viewedChunkCoord = new Vector2(currentChunkCoordX + xOffset, currentChunkCoordY + yOffset);

                //If the selected chunk was already created, update it and if it is visible store it to disable it next update
                if (chunkDictionary.ContainsKey(viewedChunkCoord))
                {
                    chunkDictionary[viewedChunkCoord].UpdateChunk();
                }
                else //If the chunk has not been created before, create it.
                {
                    chunkDictionary.Add(viewedChunkCoord, new TerrainChunk(viewedChunkCoord,detailLevels,chunkSize,transform,mapMaterial));
                }

            }
        }

    }

    

    public class TerrainChunk
    {
        GameObject meshObject;
        Vector2 pos;
        Bounds bounds;
        MeshRenderer meshRenderer;
        MeshFilter meshFilter;
        LODInfo[] detailLevels;
        LODMesh[] detailMeshes;
        LODMesh collisionLODMesh;
        MeshCollider meshCollider;

        MapData mapData;
        bool mapDataRecieved;

        int prevLodIndex = -1;

        public TerrainChunk(Vector2 coord, LODInfo[] detail, int size, Transform parent, Material material)
        {
            //Initializing the position and view bounds of the chunk
            pos = coord*size;
            bounds = new Bounds(pos, Vector2.one * size);
            Vector3 posV3 = new Vector3(pos.x, 0, pos.y);

            //Initializing the information about the different LOD values
            detailLevels = detail;

            //Creating the chunk mesh and deactivating it by default
            meshObject = new GameObject("Terrain Chunk");
            meshRenderer = meshObject.AddComponent<MeshRenderer>();
            meshFilter = meshObject.AddComponent<MeshFilter>();
            meshRenderer.material = material;
            meshCollider = meshObject.AddComponent<MeshCollider>();
            meshObject.transform.position = posV3 * mapGenerator.terrainData.uniformScale;
            meshObject.transform.parent = parent;
            meshObject.transform.localScale = Vector3.one * mapGenerator.terrainData.uniformScale;
            setVisible(false);

            //Initializing the different meshes for each LOD
            detailMeshes = new LODMesh[detailLevels.Length];
            for(int i = 0; i<detailLevels.Length; i++)
            {
                detailMeshes[i] = new LODMesh(detailLevels[i].LOD, UpdateChunk);
                if (detailLevels[i].useForCollider)
                {
                    collisionLODMesh = detailMeshes[i];
                }
            }

            //Start a thread for getting map data
            mapGenerator.RequestMapData(pos,OnMapDataRecieved);
        }

        public void UpdateChunk()
        {
            //Make sure the chunk has map data before updating
            if (mapDataRecieved)
            {
                //Only make the chunk visible if it is within the maximum view distance
                float viewerDstFromNearestEdge = Mathf.Sqrt(bounds.SqrDistance(viewerPosition));
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
                    if(lodIndex == 0)
                    {
                        if (collisionLODMesh.hasMesh)
                        {
                            meshCollider.sharedMesh = collisionLODMesh.mesh;
                        } else if(!collisionLODMesh.hasRequestedMesh){
                            collisionLODMesh.RequestMesh(mapData);
                        }
                    }
                    chunksVisibleLastUpdate.Add(this);
                }
                //Change the visibility of the chunk
                setVisible(visible);
            }
            
        }

        //Callback when mapGenerator returns matData, updates the chunk
        void OnMapDataRecieved(MapData mapData)
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
        System.Action updateCallback;

        public LODMesh(int lod, System.Action updCall)
        {
            updateCallback = updCall;
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
        public void RequestMesh(MapData mapData)
        {
            hasRequestedMesh = true;
            mapGenerator.RequestMeshData(mapData, lod,OnMeshDataRecieved);
        }
    }
    [System.Serializable]
    public struct LODInfo
    {
        public bool useForCollider;
        public int LOD;
        public float visibleDistanceThreshold;

    }
}
