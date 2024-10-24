using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using JKFrame;
using System;
using Random = UnityEngine.Random;
/// <summary>
/// ��ͼ���ɹ���
/// </summary>
public class MapGenerator
{
    // ������ͼ�Ƿ��ģ����ǵ�ͼ�顢���ӡ���ͼ����������
    private int mapAmount;        // һ�л���һ���ж��ٸ���ͼ��
    private int mapChunkAmount;   // һ����ͼ���ж��ٸ�����
    private float cellSize;     // һ�����Ӷ�����

    private float noiseLacunarity;  // ������϶
    private int mapSeed;            // ��ͼ����
    private int spawnSeed;          // ��ʱ��ͼ���������
    private float marshBorder;       // ����ı߽�
    private MapGrid mapGrid;        // ��ͼ�߼����񡢶�������
    private Material mapMaterial;
    private Material marshMaterial;
    private Mesh chunkMesh;

    private Texture2D forestTexutre;
    private Texture2D[] marshTextures;
    private Dictionary<MapVertexType, List<int>> spawnConfigDic;

    private int forestSpawanWeightTotal;
    private int marshSpawanWeightTotal;

    public MapGenerator(int mapSize, int mapChunkSize, float cellSize, float noiseLacunarity, int mapSeed, int spawnSeed, float marshLimit, Material mapMaterial, Texture2D forestTexutre, Texture2D[] marshTextures, Dictionary<MapVertexType, List<int>> spawnConfigDic)
    {
        this.mapAmount = mapSize;
        this.mapChunkAmount = mapChunkSize;
        this.cellSize = cellSize;
        this.noiseLacunarity = noiseLacunarity;
        this.mapSeed = mapSeed;
        this.spawnSeed = spawnSeed;
        this.marshBorder = marshLimit;
        this.mapMaterial = mapMaterial;
        this.forestTexutre = forestTexutre;
        this.marshTextures = marshTextures;
        this.spawnConfigDic = spawnConfigDic;
    }


    /// <summary>
    /// ���ɵ�ͼ���ݣ���Ҫ�����е�ͼ�鶼ͨ�õ�����
    /// </summary>
    public void GenerateMapData()
    {
        // ��������ͼ
        // Ӧ�õ�ͼ����
        Random.InitState(mapSeed);
        float[,] noiseMap = GenerateNoiseMap(mapAmount * mapChunkAmount, mapAmount * mapChunkAmount, noiseLacunarity);
        // ������������
        mapGrid = new MapGrid(mapAmount * mapChunkAmount, mapAmount * mapChunkAmount, cellSize);
        // ȷ������ ���ӵ���ͼ����
        mapGrid.CalculateMapVertexType(noiseMap, marshBorder);
        // ��ʼ��Ĭ�ϲ��ʵĳߴ�
        mapMaterial.mainTexture = forestTexutre;
        mapMaterial.SetTextureScale("_MainTex", new Vector2(cellSize * mapChunkAmount, cellSize * mapChunkAmount));
        // ʵ����һ���������
        marshMaterial = new Material(mapMaterial);
        marshMaterial.SetTextureScale("_MainTex", Vector2.one);

        chunkMesh = GenerateMapMesh(mapChunkAmount, mapChunkAmount, cellSize);
        // ʹ�������������������
        Random.InitState(spawnSeed);

        List<int> temps = spawnConfigDic[MapVertexType.Forest];
        for (int i = 0; i < temps.Count; i++) forestSpawanWeightTotal += ConfigManager.Instance.GetConfig<MapObjectConfig>(ConfigName.MapObject, temps[i]).Probability;
        temps = spawnConfigDic[MapVertexType.Marsh];
        for (int i = 0; i < temps.Count; i++) marshSpawanWeightTotal += ConfigManager.Instance.GetConfig<MapObjectConfig>(ConfigName.MapObject, temps[i]).Probability;
    }

    /// <summary>
    /// ���ɵ�ͼ��
    /// </summary>
    public MapChunkController GenerateMapChunk(Vector2Int chunkIndex,Transform parent,Action callBackForMapTexture)
    {
        // ���ɵ�ͼ������
        GameObject mapChunkObj = new GameObject("Chunk_" + chunkIndex.ToString());
        MapChunkController mapChunk=mapChunkObj.AddComponent<MapChunkController>();

        // ����Mesh
        mapChunkObj.AddComponent<MeshFilter>().mesh = chunkMesh;
        // �����ײ��
        mapChunkObj.AddComponent<MeshCollider>();

        bool allForest;
        // ���ɵ�ͼ�����ͼ
        Texture2D mapTexture;
        this.StartCoroutine
        (
            GenerateMapTexture(chunkIndex, (tex, isAllForest) => {
            allForest = isAllForest;
            // �����ȫ��ɭ�֣�û��Ҫ��ʵ����һ��������
            if (isAllForest)
            {
                mapChunkObj.AddComponent<MeshRenderer>().sharedMaterial = mapMaterial;
            }
            else
            {
                mapTexture = tex;
                Material material = new Material(marshMaterial);
                material.mainTexture = tex;
                mapChunkObj.AddComponent<MeshRenderer>().material = material;
            }
            callBackForMapTexture?.Invoke();

            // ȷ������
            Vector3 position = new Vector3(chunkIndex.x * mapChunkAmount * cellSize, 0, chunkIndex.y * mapChunkAmount * cellSize);
            mapChunk.transform.position = position;
            mapChunkObj.transform.SetParent(parent);

            // ���ɳ�����������
            List<MapChunkMapObjectModel> mapObjectModelList = SpawnMapObject(chunkIndex);
            mapChunk.Init(chunkIndex, position + new Vector3((mapChunkAmount * cellSize) / 2, 0, (mapChunkAmount * cellSize) / 2), allForest, mapObjectModelList);
            })
        );
       
        return mapChunk;
    }

    /// <summary>
    /// ���ɵ���Mesh
    /// </summary>
    private Mesh GenerateMapMesh(int height,int wdith, float cellSize)
    {
        Mesh mesh = new Mesh();
        // ȷ������������
        mesh.vertices = new Vector3[]
        {
            new Vector3(0,0,0),
            new Vector3(0,0,height*cellSize),
            new Vector3(wdith*cellSize,0,height*cellSize),
            new Vector3(wdith*cellSize,0,0),
        };
        // ȷ����Щ���γ�������
        mesh.triangles = new int[]
        {
            0,1,2,
            0,2,3
        };
        mesh.uv = new Vector2[]
        {
            new Vector3(0,0),
            new Vector3(0,1),
            new Vector3(1,1),
            new Vector3(1,0),
        };
        // ���㷨��
        mesh.RecalculateNormals();
        return mesh;
    }

    /// <summary>
    /// ��������ͼ
    /// </summary>
    private float[,] GenerateNoiseMap(int width, int height, float lacunarity)
    { 

        lacunarity += 0.1f;
        // ���������ͼ��Ϊ�˶�������
        float[,] noiseMap = new float[width-1,height-1];
        float offsetX = Random.Range(-10000f, 10000f);
        float offsetY = Random.Range(-10000f, 10000f);

        for (int x = 0; x < width - 1; x++)
        {
            for (int y = 0; y < height - 1; y++)
            {
                noiseMap[x,y] = Mathf.PerlinNoise(x * lacunarity + offsetX,y * lacunarity + offsetY);
            }
        }
        return noiseMap;
    }

    /// <summary>
    /// ��֡ ���ɵ�ͼ��ͼ
    /// ��������ͼ����ȫ��ɭ�֣�ֱ�ӷ���ɭ����ͼ
    /// </summary>
    private IEnumerator GenerateMapTexture(Vector2Int chunkIndex,System.Action<Texture2D,bool> callBack)
    {
        // ��ǰ�ؿ��ƫ���� �ҵ������ͼ������ÿһ������
        int cellOffsetX = chunkIndex.x * mapChunkAmount + 1;
        int cellOffsetY = chunkIndex.y * mapChunkAmount + 1;

        // �ǲ���һ��������ɭ�ֵ�ͼ��
        bool isAllForest = true;
        // ����Ƿ�ֻ��ɭ�����͵ĸ���
        for (int y = 0; y < mapChunkAmount; y++)
        {
            if (isAllForest == false) break;
            for (int x = 0; x < mapChunkAmount; x++)
            {
                MapCell cell = mapGrid.GetCell(x + cellOffsetX, y + cellOffsetY);
                if (cell != null && cell.TextureIndex != 0)
                {
                    isAllForest = false;
                    break;
                }
            }
        }

        Texture2D mapTexture = null;
        // ����������
        if (!isAllForest)
        {
            // ��ͼ���Ǿ���
            int textureCellSize = forestTexutre.width;
            // ������ͼ��Ŀ��,������
            int textureSize = mapChunkAmount * textureCellSize;
            mapTexture = new Texture2D(textureSize, textureSize, TextureFormat.RGB24, false);

            // ����ÿһ������
            for (int y = 0; y < mapChunkAmount; y++)
            {
                // һִֻ֡��һ�� ֻ����һ�е�����
                yield return null;
                // ����ƫ����
                int pixelOffsetY = y * textureCellSize;
                for (int x = 0; x < mapChunkAmount; x++)
                {

                    int pixelOffsetX = x * textureCellSize;
                    int textureIndex = mapGrid.GetCell(x + cellOffsetX, y + cellOffsetY).TextureIndex - 1;
                    // ����ÿһ�������ڵ�����
                    // ����ÿһ�����ص�
                    for (int y1 = 0; y1 < textureCellSize; y1++)
                    {
                        for (int x1 = 0; x1 < textureCellSize; x1++)
                        {

                            // ����ĳ�����ص����ɫ
                            // ȷ����ɭ�ֻ�������
                            // ����ط���ɭ�� ||
                            // ����ط�����������͸���ģ����������Ҫ����groundTextureͬλ�õ�������ɫ
                            if (textureIndex < 0)
                            {
                                Color color = forestTexutre.GetPixel(x1, y1);
                                mapTexture.SetPixel(x1 + pixelOffsetX, y1 + pixelOffsetY, color);
                            }
                            else
                            {
                                // ��������ͼ����ɫ
                                Color color = marshTextures[textureIndex].GetPixel(x1, y1);
                                if (color.a < 1f)
                                {
                                    mapTexture.SetPixel(x1 + pixelOffsetX, y1 + pixelOffsetY, forestTexutre.GetPixel(x1, y1));
                                }
                                else
                                {
                                    mapTexture.SetPixel(x1 + pixelOffsetX, y1 + pixelOffsetY, color);
                                }
                            }

                        }
                    }
                }
            }
            mapTexture.filterMode = FilterMode.Point;
            mapTexture.wrapMode = TextureWrapMode.Clamp;
            mapTexture.Apply();
        }
        callBack?.Invoke(mapTexture, isAllForest);
    }

    /// <summary>
    /// ���ɸ��ֵ�ͼ����
    /// </summary>
    private List<MapChunkMapObjectModel> SpawnMapObject(Vector2Int chunkIndex)
    {
        List<MapChunkMapObjectModel> mapChunkObjectList = new List<MapChunkMapObjectModel>();

        int offsetX = chunkIndex.x * mapChunkAmount;
        int offsetY = chunkIndex.y * mapChunkAmount;

        // ������ͼ����
        for (int x = 1; x < mapChunkAmount; x++)
        {
            for (int y = 1; y < mapChunkAmount; y++)
            {
                MapVertex mapVertex = mapGrid.GetVertex(x + offsetX, y + offsetY);
                // ���ݸ����������
                List<int> configIDs = spawnConfigDic[mapVertex.VertexType];

                // ȷ��Ȩ�ص��ܺ�
                int weightTotal = mapVertex.VertexType == MapVertexType.Forest?forestSpawanWeightTotal:marshSpawanWeightTotal;

                int randValue = Random.Range(1, weightTotal+1); // ʵ�����������Ǵ�1~weightTotal
                float temp = 0;
                int spawnConfigIndex = 0;   // ����Ҫ���ɵ���Ʒ

                // 30 20 50
                for (int i = 0; i < configIDs.Count; i++)
                {
                    temp +=ConfigManager.Instance.GetConfig<MapObjectConfig>(ConfigName.MapObject, configIDs[i]).Probability;
                    if (randValue < temp)
                    {
                        // ����
                        spawnConfigIndex = i;
                        break;
                    }
                }

                int configID = configIDs[spawnConfigIndex];
                // ȷ����������ʲô��Ʒ
                MapObjectConfig spawnModel = ConfigManager.Instance.GetConfig<MapObjectConfig>(ConfigName.MapObject, configID);
                if (spawnModel.IsEmpty == false)
                {
                    Vector3 position = mapVertex.Position + new Vector3(Random.Range(-cellSize / 2, cellSize / 2), 0, Random.Range(-cellSize / 2, cellSize / 2));
                    mapChunkObjectList.Add(
                        new MapChunkMapObjectModel() { ConfigID = configID, Position = position }
                   );
                }
            }
        }
        return mapChunkObjectList;
    }
}


