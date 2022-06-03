using System;
using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;

public class EndlessTerrain : MonoBehaviour
{
    public const float maxViewDist = 500;
    public Transform viewer;
    public Material mapMaterial;

    public static Vector2 viewerPosition;
    private int chunkSize;
    private int chunksVisibleInViewDist;

    static MapGenerator mapGenerator;

    // dico contenant les chunck générés
    private Dictionary<Vector2, TerrainChunk> terainChunkDico = new Dictionary<Vector2, TerrainChunk>();
    private List<TerrainChunk> terrainChunkVisibleLastUpdate = new List<TerrainChunk>();

    private void Start()
    {
        mapGenerator = FindObjectOfType<MapGenerator>();
        chunkSize = MapGenerator.mapchunkSize - 1;
        chunksVisibleInViewDist = Mathf.RoundToInt(maxViewDist / chunkSize);
    }

    private void Update()
    {
        viewerPosition = new Vector2(viewer.position.x, viewer.position.z);
        UpdateVisibleChunks();
    }

    private void UpdateVisibleChunks()
    {
        for (int i = 0; i < terrainChunkVisibleLastUpdate.Count; i++)
        {
            terrainChunkVisibleLastUpdate[i].SetVisible(false);
        }
        terrainChunkVisibleLastUpdate.Clear();
        
        
        Vector2 currentchunkCoord = new Vector2(Mathf.RoundToInt(viewerPosition.x / chunkSize),
            Mathf.RoundToInt(viewerPosition.y / chunkSize));

        // on itère sur les chunks
        for (int yOffset = -chunksVisibleInViewDist; yOffset < chunksVisibleInViewDist; yOffset++)
        {
            for (int xOffset = -chunksVisibleInViewDist; xOffset < chunksVisibleInViewDist; xOffset++)
            {
                Vector2 viewedChunkCoord = new Vector2(currentchunkCoord.x + xOffset, currentchunkCoord.y + yOffset);

                if (terainChunkDico.ContainsKey(viewedChunkCoord))
                { 
                    terainChunkDico[viewedChunkCoord].UpdateTerrainChunk();
                    if (terainChunkDico[viewedChunkCoord].IsVisible)
                        terrainChunkVisibleLastUpdate.Add(terainChunkDico[viewedChunkCoord]);
                }
                else
                {
                    terainChunkDico.Add(viewedChunkCoord, new TerrainChunk(viewedChunkCoord, chunkSize,transform, mapMaterial));
                }
            }
        }
    }

    public class TerrainChunk
    {
        private readonly GameObject meshObject;
        MapData mapData;
        MeshRenderer meshRenderer;
        MeshFilter meshFilter;

        private Vector2 position;
        private Bounds bounds; // pour calculer le point le plus proche entre le viewer et le chunk

        public TerrainChunk(Vector2 coord, int size, Transform parent, Material material)
        {
            position = coord * size;
            bounds = new Bounds(position, Vector2.one * size);
            Vector3 positionV3 = new Vector3(position.x, 0, position.y);

            meshObject = new GameObject("Terrain Chunk");
            meshRenderer = meshObject.AddComponent<MeshRenderer>();
            meshFilter = meshObject.AddComponent<MeshFilter>();
            meshRenderer.material = material;

            meshObject.transform.position = positionV3;
            meshObject.transform.parent = parent;
            SetVisible(false);

            // abonnement au callback
            mapGenerator.RequestMapData(OnMapDataReceived);
        }

        // lorsque la mapData est reçue, on lance le thread pour la construction des mesh
        void OnMapDataReceived(MapData mapData)
        {
            mapGenerator.RequestMeshData(mapData, OnMeshDataReceived);
        }

        void OnMeshDataReceived(MeshData meshData)
        {
            meshFilter.mesh = meshData.CreateMesh();
        }

        public void UpdateTerrainChunk()
        {
            float viewerDstFromNearestEdge = Mathf.Sqrt(bounds.SqrDistance(viewerPosition));
            bool visible = viewerDstFromNearestEdge <= maxViewDist;
            SetVisible(visible);
        }

        public void SetVisible(bool visible)
        {
            meshObject.SetActive(visible);
        }

        public bool IsVisible => meshObject.activeSelf;
    }
}