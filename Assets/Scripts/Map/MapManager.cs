using System.Collections;
using System.Collections.Generic;
using JKFrame;
using UnityEngine;
using UnityEngine.Serialization;

public class MapManager : MonoBehaviour
{
    // ��ͼ�ߴ�
    public int chunkAmount;        // һ�л���һ���ж��ٸ���ͼ��
    public int mapChunkAmount;   // һ����ͼ���ж��ٸ�����
    public float cellSize;     // һ�����Ӷ�����

    // ��ͼ���������
    public float noiseLacunarity;  // ������϶
    public int mapSeed;            // ��ͼ����
    public int spawnSeed;          // ��ʱ��ͼ���������
    public float marshLimit;       // ����ı߽�

    // ��ͼ��������Դ
    public Material mapMaterial;
    public Texture2D forestTexture;
    public Texture2D[] marshTextures;
    public MapConfig mapConfig;   //��ͼ����

    private MapGenerator mapGenerator;
    public int viewDinstance;       // ��ҿ��Ӿ��룬��λ��Chunk
    public Transform viewer;        // �۲���
    private Vector3 lastViewerPos = Vector3.one * -1;
    public Dictionary<Vector2Int, MapChunkController> mapChunkDic;  // ȫ�����еĵ�ͼ��

    public float updateChunkTime = 1f;
    private bool canUpdateChunk = true;
    private float mapSizeOnWorld;    // ��������ʵ�ʵĵ�ͼ����ߴ� ��λ��
    private float chunkSizeOnWorld;  // ��������ʵ�ʵĵ�ͼ��ߴ� ��λ��
    private List<MapChunkController> lastVisibleChunkList = new List<MapChunkController>();

    // ĳ�����Ϳ���������Щ����ID
    private Dictionary<MapVertexType, List<int>> spawnConfigDic;
    void Start()
    {
        // ȷ������
        Dictionary<int, ConfigBase> tempDic = ConfigManager.Instance.GetConfigs(ConfigName.MapObject);
        spawnConfigDic = new Dictionary<MapVertexType, List<int>>();
        spawnConfigDic.Add(MapVertexType.Forest, new List<int>());
        spawnConfigDic.Add(MapVertexType.Marsh, new List<int>());
        foreach (var item in tempDic)
        {
            MapVertexType mapVertexType = (item.Value as MapObjectConfig).MapVertexType;
            spawnConfigDic[mapVertexType].Add(item.Key);
        }
        // ��ʼ����ͼ������
        mapGenerator = new MapGenerator(chunkAmount, mapChunkAmount, cellSize, noiseLacunarity, mapSeed, spawnSeed,
            marshLimit, mapMaterial, forestTexture, marshTextures, spawnConfigDic);
        mapGenerator.GenerateMapData();
        mapChunkDic = new Dictionary<Vector2Int, MapChunkController>();
        chunkSizeOnWorld = mapChunkAmount * cellSize;
        mapSizeOnWorld = chunkSizeOnWorld * chunkAmount;
    }

    // Update is called once per frame
    void Update()
    {
        UpdateVisibleChunk();

        if (Input.GetKeyDown(KeyCode.M))
        {
            if (isShowMaping) CloseMapUI();
            else ShowMapUI();
            isShowMaping = !isShowMaping;
        }
    }

    // ���ݹ۲��ߵ�λ����ˢ����Щ��ͼ����Կ���
    private void UpdateVisibleChunk()
    {
        // ����۲���û���ƶ���������Ҫˢ��
        if (viewer.position == lastViewerPos) return;
        // ���ʱ��û�� ���������
        if (canUpdateChunk == false) return;

        // ��ǰ�۲������ڵĵ�ͼ�죬
        Vector2Int currChunkIndex = GetMapChunkIndexByWorldPosition(viewer.position);

        // �ر�ȫ������Ҫ��ʾ�ĵ�ͼ��
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
        // ������Ҫ��ʾ�ĵ�ͼ��
        for (int x = 0; x < 2*viewDinstance+1; x++)
        {
            for (int y = 0; y < 2 * viewDinstance + 1; y++)
            {
                canUpdateChunk = false;
                Invoke("RestCanUpdateChunkFlag", updateChunkTime);
                Vector2Int chunkIndex = new Vector2Int(startX + x, startY + y);
                // ֮ǰ���ع�
                if (mapChunkDic.TryGetValue(chunkIndex,out MapChunkController chunk))
                {
                    // �����ͼ�ǲ����Ѿ�����ʾ�б�
                    if (lastVisibleChunkList.Contains(chunk) == false)
                    {
                        lastVisibleChunkList.Add(chunk);
                        chunk.SetActive(true);
                    }
                }
                // ֮ǰû�м���
                else
                {
                    chunk = GenerateMapChunk(chunkIndex);
                    if (chunk!=null)
                    {
                        chunk.SetActive(true);
                        lastVisibleChunkList.Add(chunk);
                    }
                }
            }
        }
    }

    /// <summary>
    /// �������������ȡ��ͼ�������
    /// </summary>
    private Vector2Int GetMapChunkIndexByWorldPosition(Vector3 worldPostion)
    {
        int x = Mathf.Clamp(Mathf.RoundToInt(worldPostion.x / chunkSizeOnWorld), 1, chunkAmount);
        int y = Mathf.Clamp(Mathf.RoundToInt(worldPostion.z / chunkSizeOnWorld), 1, chunkAmount);
        return new Vector2Int(x,y);
    }

    /// <summary>
    /// ���ɵ�ͼ��
    /// </summary>
    private MapChunkController GenerateMapChunk(Vector2Int index)
    {
        // �������ĺϷ���
        if (index.x > chunkAmount - 1 || index.y > chunkAmount - 1) return null;
        if (index.x < 0 || index.y < 0) return null;
        MapChunkController chunk = mapGenerator.GenerateMapChunk(index, transform, () =>
        {
            mapUIUpdateChunkIndexList.Add(index);
        });
        mapChunkDic.Add(index, chunk);
        return chunk;
    }

    private void RestCanUpdateChunkFlag()
    {
        canUpdateChunk = true;
    }

    #region ��ͼUI���
    private bool mapUIInitialized = false;
    private bool isShowMaping = false;
    private List<Vector2Int> mapUIUpdateChunkIndexList = new List<Vector2Int>();    // �����µ��б�
    private UI_MapWindow mapUI;

    // ��ʾ��ͼUI
    private void ShowMapUI()
    {
        mapUI = UIManager.Instance.Show<UI_MapWindow>();
        if (!mapUIInitialized)
        {
            mapUI.InitMap(chunkAmount, mapSizeOnWorld, forestTexture);
            mapUIInitialized = true;
        }
        // ����
        UpdateMapUI();
    }
    private void UpdateMapUI()
    {
        for (int i = 0; i < mapUIUpdateChunkIndexList.Count; i++)
        {
            Vector2Int chunkIndex = mapUIUpdateChunkIndexList[i];
            Texture2D texture = null;
            MapChunkController mapChunk = mapChunkDic[chunkIndex];
            if (!mapChunk.IsAllForest)
            {
                texture = (Texture2D)mapChunk.GetComponent<MeshRenderer>().material.mainTexture;
            }
            
        }
    }
    // �رյ�ͼUI
    private void CloseMapUI()
    {
        UIManager.Instance.Close<UI_MapWindow>();
    }
    #endregion
}
