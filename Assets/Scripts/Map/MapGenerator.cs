using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using JKFrame;
using System;
using Random = UnityEngine.Random;
/// <summary>
/// 地图生成工具
/// </summary>
public class MapGenerator
{
    // 整个地图是方的，就是地图块、格子、贴图都是正方形
    private int mapAmount;        // 一行或者一列有多少个地图块
    private int mapChunkAmount;   // 一个地图块有多少个格子
    private float cellSize;     // 一个格子多少米

    private float noiseLacunarity;  // 噪音间隙
    private int mapSeed;            // 地图种子
    private int spawnSeed;          // 随时地图对象的种子
    private float marshBorder;       // 沼泽的边界
    private MapGrid mapGrid;        // 地图逻辑网格、顶点数据
    private Material mapMaterial;
    private Material marshMaterial;
    private Mesh chunkMesh;

    private Texture2D forestTexutre;
    private Texture2D[] marshTextures;
    // 某个类型可以生成那些配置的ID
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
    /// 生成地图数据，主要是所有地图块都通用的数据
    /// </summary>
    public void GenerateMapData()
    {
        // 生成噪声图
        // 应用地图种子
        Random.InitState(mapSeed);
        float[,] noiseMap = GenerateNoiseMap(mapAmount * mapChunkAmount, mapAmount * mapChunkAmount, noiseLacunarity);
        // 生成网格数据
        mapGrid = new MapGrid(mapAmount * mapChunkAmount, mapAmount * mapChunkAmount, cellSize);
        // 确定网格 格子的贴图索引
        mapGrid.CalculateMapVertexType(noiseMap, marshBorder);
        // 初始化默认材质的尺寸
        mapMaterial.mainTexture = forestTexutre;
        mapMaterial.SetTextureScale("_MainTex", new Vector2(cellSize * mapChunkAmount, cellSize * mapChunkAmount));
        // 实例化一个沼泽材质
        marshMaterial = new Material(mapMaterial);
        marshMaterial.SetTextureScale("_MainTex", Vector2.one);

        chunkMesh = GenerateMapMesh(mapChunkAmount, mapChunkAmount, cellSize);
        // 使用种子来进行随机生成
        Random.InitState(spawnSeed);

        List<int> temps = spawnConfigDic[MapVertexType.Forest];
        for (int i = 0; i < temps.Count; i++) forestSpawanWeightTotal += ConfigManager.Instance.GetConfig<MapObjectConfig>(ConfigName.MapObject, temps[i]).Probability;
        temps = spawnConfigDic[MapVertexType.Marsh];
        for (int i = 0; i < temps.Count; i++) marshSpawanWeightTotal += ConfigManager.Instance.GetConfig<MapObjectConfig>(ConfigName.MapObject, temps[i]).Probability;
    }

    /// <summary>
    /// 生成地图块
    /// </summary>
    public MapChunkController GenerateMapChunk(Vector2Int chunkIndex, Transform parent, Action callBackForMapTexture)
    {
        // 生成地图块物体
        GameObject mapChunkObj = new GameObject("Chunk_" + chunkIndex.ToString());
        MapChunkController mapChunk=mapChunkObj.AddComponent<MapChunkController>();

        // 生成Mesh
        mapChunkObj.AddComponent<MeshFilter>().mesh = chunkMesh;
        // 添加碰撞体
        mapChunkObj.AddComponent<MeshCollider>();

        bool allForest;
        // 生成地图块的贴图
        Texture2D mapTexture;
        this.StartCoroutine
        (
            GenerateMapTexture(chunkIndex, (tex, isAllForest) => {
            allForest = isAllForest;
            // 如果完全是森林，没必要在实例化一个材质球
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

            // 确定坐标
            Vector3 position = new Vector3(chunkIndex.x * mapChunkAmount * cellSize, 0, chunkIndex.y * mapChunkAmount * cellSize);
            mapChunk.transform.position = position;
            mapChunkObj.transform.SetParent(parent);

            // 生成场景物体数据
            List<MapChunkMapObjectModel> mapObjectModelList = SpawnMapObject(chunkIndex);
            mapChunk.Init(chunkIndex, position + new Vector3((mapChunkAmount * cellSize) / 2, 0, (mapChunkAmount * cellSize) / 2), allForest, mapObjectModelList);
            })
        );
       
        return mapChunk;
    }

    /// <summary>
    /// 生成地形Mesh
    /// </summary>
    private Mesh GenerateMapMesh(int height,int wdith, float cellSize)
    {
        Mesh mesh = new Mesh();
        // 确定顶点在哪里
        mesh.vertices = new Vector3[]
        {
            new Vector3(0,0,0),
            new Vector3(0,0,height*cellSize),
            new Vector3(wdith*cellSize,0,height*cellSize),
            new Vector3(wdith*cellSize,0,0),
        };
        // 确定哪些点形成三角形
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
        // 计算法线
        mesh.RecalculateNormals();
        return mesh;
    }

    /// <summary>
    /// 生成噪声图
    /// </summary>
    private float[,] GenerateNoiseMap(int width, int height, float lacunarity)
    { 

        lacunarity += 0.1f;
        // 这里的噪声图是为了顶点服务的
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
    /// 分帧 生成地图贴图
    /// 如果这个地图块完全是森林，直接返回森林贴图
    /// </summary>
    private IEnumerator GenerateMapTexture(Vector2Int chunkIndex,System.Action<Texture2D,bool> callBack)
    {
        // 当前地块的偏移量 找到这个地图块具体的每一个格子
        int cellOffsetX = chunkIndex.x * mapChunkAmount + 1;
        int cellOffsetY = chunkIndex.y * mapChunkAmount + 1;

        // 是不是一张完整的森林地图块
        bool isAllForest = true;
        // 检查是否只有森林类型的格子
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
        // 有沼泽的情况
        if (!isAllForest)
        {
            // 贴图都是矩形
            int textureCellSize = forestTexutre.width;
            // 整个地图块的宽高,正方形
            int textureSize = mapChunkAmount * textureCellSize;
            mapTexture = new Texture2D(textureSize, textureSize, TextureFormat.RGB24, false);

            // 遍历每一个格子
            for (int y = 0; y < mapChunkAmount; y++)
            {
                // 一帧只执行一列 只绘制一列的像素
                yield return null;
                // 像素偏移量
                int pixelOffsetY = y * textureCellSize;
                for (int x = 0; x < mapChunkAmount; x++)
                {

                    int pixelOffsetX = x * textureCellSize;
                    int textureIndex = mapGrid.GetCell(x + cellOffsetX, y + cellOffsetY).TextureIndex - 1;
                    // 绘制每一个格子内的像素
                    // 访问每一个像素点
                    for (int y1 = 0; y1 < textureCellSize; y1++)
                    {
                        for (int x1 = 0; x1 < textureCellSize; x1++)
                        {

                            // 设置某个像素点的颜色
                            // 确定是森林还是沼泽
                            // 这个地方是森林 ||
                            // 这个地方是沼泽但是是透明的，这种情况需要绘制groundTexture同位置的像素颜色
                            if (textureIndex < 0)
                            {
                                Color color = forestTexutre.GetPixel(x1, y1);
                                mapTexture.SetPixel(x1 + pixelOffsetX, y1 + pixelOffsetY, color);
                            }
                            else
                            {
                                // 是沼泽贴图的颜色
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
    /// 生成各种地图对象
    /// </summary>
    private List<MapChunkMapObjectModel> SpawnMapObject(Vector2Int chunkIndex)
    {
        List<MapChunkMapObjectModel> mapChunkObjectList = new List<MapChunkMapObjectModel>();

        int offsetX = chunkIndex.x * mapChunkAmount;
        int offsetY = chunkIndex.y * mapChunkAmount;

        // 遍历地图顶点
        for (int x = 1; x < mapChunkAmount; x++)
        {
            for (int y = 1; y < mapChunkAmount; y++)
            {
                MapVertex mapVertex = mapGrid.GetVertex(x + offsetX, y + offsetY);
                // 根据概率配置随机
                List<int> configIDs = spawnConfigDic[mapVertex.VertexType];

                // 确定权重的总和
                int weightTotal = mapVertex.VertexType == MapVertexType.Forest?forestSpawanWeightTotal:marshSpawanWeightTotal;

                int randValue = Random.Range(1, weightTotal+1); // 实际命中数字是从1~weightTotal
                float temp = 0;
                int spawnConfigIndex = 0;   // 最终要生成的物品

                // 30 20 50
                for (int i = 0; i < configIDs.Count; i++)
                {
                    temp +=ConfigManager.Instance.GetConfig<MapObjectConfig>(ConfigName.MapObject, configIDs[i]).Probability;
                    if (randValue < temp)
                    {
                        // 命中
                        spawnConfigIndex = i;
                        break;
                    }
                }

                int configID = configIDs[spawnConfigIndex];
                // 确定到底生成什么物品
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


