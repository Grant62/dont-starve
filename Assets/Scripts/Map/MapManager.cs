using System.Collections.Generic;
using UnityEngine;
using JKFrame;

public class MapManager : SingletonMono<MapManager>
{
    // 地图尺寸
    public int mapAmount;        // 一行或者一列有多少个地图块
    public int mapChunkAmount;   // 一个地图块有多少个格子
    public float cellSize;     // 一个格子多少米

    // 地图的随机参数
    public float noiseLacunarity;  // 噪音间隙
    public int mapSeed;            // 地图种子
    public int spawnSeed;          // 随时地图对象的种子
    public float marshBorder;       // 沼泽的边界

    // 地图的美术资源
    public Material mapMaterial;
    public Texture2D forestTexutre;
    public Texture2D[] marshTextures;
    public MapConfig mapConfig;   //地图配置

    private MapGenerator mapGenerator;
    public int viewDinstance;       // 玩家可视距离，单位是Chunk
    public Transform viewer;        // 观察者
    private Vector3 lastViewerPos = Vector3.one * -1;
    public Dictionary<Vector2Int, MapChunkController> mapChunkDic;  // 全部已有的地图块

    public float updateChunkTime = 1f;
    private bool canUpdateChunk = true;
    public float mapSizeOnWorld;    // 在世界中实际的地图整体尺寸
    private float chunkSizeOnWorld;  // 在世界中实际的地图块尺寸 单位米
    private List<MapChunkController> lastVisibleChunkList = new List<MapChunkController>();

    // 某个类型可以生成那些配置的ID
    private Dictionary<MapVertexType, List<int>> spawnConfigDic;

    protected override void Awake()
    {
        base.Awake();
        Init();
    }

    void Init()
    {
        // 确定配置
        Dictionary<int, ConfigBase> tempDic = ConfigManager.Instance.GetConfigs(ConfigName.MapObject);
        spawnConfigDic = new Dictionary<MapVertexType, List<int>>();
        spawnConfigDic.Add(MapVertexType.Forest, new List<int>());
        spawnConfigDic.Add(MapVertexType.Marsh, new List<int>());
        foreach (var item in tempDic)
        {
            MapVertexType mapVertexType = (item.Value as MapObjectConfig).MapVertexType;
            spawnConfigDic[mapVertexType].Add(item.Key);
        }

        // 初始化地图生成器
        mapGenerator = new MapGenerator(mapAmount, mapChunkAmount, cellSize, noiseLacunarity, mapSeed, spawnSeed, marshBorder, mapMaterial, forestTexutre, marshTextures, spawnConfigDic);
        mapGenerator.GenerateMapData();
        mapChunkDic = new Dictionary<Vector2Int, MapChunkController>();
        chunkSizeOnWorld = mapChunkAmount * cellSize;
        mapSizeOnWorld = chunkSizeOnWorld * mapAmount;
    }

    void Update()
    {
        UpdateVisibleChunk();

        if (Input.GetKeyDown(KeyCode.M))
        {
            if (isShowMaping) CloseMapUI();
            else ShowMapUI();
            isShowMaping = !isShowMaping;
        }

        if (isShowMaping)
        {
            UpdateMapUI();
        }
    }

    // 根据观察者的位置来刷新那些地图块可以看到
    private void UpdateVisibleChunk()
    {
        // 如果观察者没有移动过，不需要刷新
        if (viewer.position == lastViewerPos) return;

        // 更新地图UI的坐标
        if (isShowMaping) mapUI.UpdatePivot(viewer.position);

        // 如果时间没到 不允许更新
        if (canUpdateChunk == false) return;

        // 当前观察者所在的地图快，
        Vector2Int currChunkIndex = GetMapChunkIndexByWorldPosition(viewer.position);

        // 关闭全部不需要显示的地图块
        for (int i = lastVisibleChunkList.Count-1; i >= 0; i--)
        {
            Vector2Int chunkIndex = lastVisibleChunkList[i].ChunkIndex;
            if (Mathf.Abs(chunkIndex.x - currChunkIndex.x)>viewDinstance
                || Mathf.Abs(chunkIndex.y - currChunkIndex.y)>viewDinstance)
            {
                lastVisibleChunkList[i].SetActive(false);
                lastVisibleChunkList.RemoveAt(i);
            }
        }

        int startX = currChunkIndex.x - viewDinstance;
        int startY = currChunkIndex.y - viewDinstance;
        // 开启需要显示的地图块
        for (int x = 0; x < 2*viewDinstance+1; x++)
        {
            for (int y = 0; y < 2 * viewDinstance + 1; y++)
            {
                canUpdateChunk = false;
                Invoke("RestCanUpdateChunkFlag", updateChunkTime);
                Vector2Int chunkIndex = new Vector2Int(startX + x, startY + y);
                // 在地图字典中，也就是之前加载过，但是不一定加载完成了，因为贴图会在协程中执行，执行完成后才算初始化完毕
                if (mapChunkDic.TryGetValue(chunkIndex, out MapChunkController chunk))
                {
                    // 上一次显示的地图列表中并不包含这个地图块 && 同时它已经完成了初始化
                    if (lastVisibleChunkList.Contains(chunk) == false && chunk.IsInitialized)
                    {
                        lastVisibleChunkList.Add(chunk);
                        chunk.SetActive(true);
                    }
                }
                // 之前没有加载
                else
                {
                    chunk = GenerateMapChunk(chunkIndex);
                }
            }
        }
    }

    /// <summary>
    /// 根据世界坐标获取地图块的索引
    /// </summary>
    private Vector2Int GetMapChunkIndexByWorldPosition(Vector3 worldPostion)
    {
        int x = Mathf.Clamp(Mathf.RoundToInt(worldPostion.x / chunkSizeOnWorld), 1, mapAmount);
        int y = Mathf.Clamp(Mathf.RoundToInt(worldPostion.z / chunkSizeOnWorld), 1, mapAmount);
        return new Vector2Int(x,y);
    }

    /// <summary>
    /// 生成地图块
    /// </summary>
    private MapChunkController GenerateMapChunk(Vector2Int index)
    {
        // 检查坐标的合法性
        if (index.x > mapAmount-1 || index.y > mapAmount-1) return null;
        if (index.x < 0 || index.y < 0) return null;
        MapChunkController chunk = mapGenerator.GenerateMapChunk(index, transform, () => mapUIUpdateChunkIndexList.Add(index));
        mapChunkDic.Add(index, chunk);
        return chunk;
    }

    private void RestCanUpdateChunkFlag()
    {
        canUpdateChunk = true;
    }

    #region 地图UI相关
    private bool mapUIInitialized = false;
    private bool isShowMaping = false;
    private List<Vector2Int> mapUIUpdateChunkIndexList = new List<Vector2Int>();    // 待更新的列表
    private UI_MapWindow mapUI;

    // 显示地图UI
    private void ShowMapUI()
    {
        mapUI = UIManager.Instance.Show<UI_MapWindow>();
        if (!mapUIInitialized)
        {
            mapUI.InitMap(mapAmount, mapChunkAmount, mapSizeOnWorld, forestTexutre);
            mapUIInitialized = true;
        }
        // 更新
        UpdateMapUI();
    }
    private void UpdateMapUI()
    {
        for (int i = 0; i < mapUIUpdateChunkIndexList.Count; i++)
        {
            Vector2Int chunkIndex = mapUIUpdateChunkIndexList[i];
            Texture2D texture = null;
            MapChunkController mapchunk = mapChunkDic[chunkIndex];
            if (!mapchunk.IsAllForest)
            {
                texture = (Texture2D)mapchunk.GetComponent<MeshRenderer>().material.mainTexture;
            }
            mapUI.AddMapChunk(chunkIndex, mapchunk.mapChunkData.MapObjectList, texture);
        }
        mapUIUpdateChunkIndexList.Clear();
        // Content的坐标
        mapUI.UpdatePivot(viewer.position);
    }
    // 关闭地图UI
    private void CloseMapUI()
    {
        UIManager.Instance.Close<UI_MapWindow>();
    }
    #endregion
}