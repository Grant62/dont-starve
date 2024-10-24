using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using JKFrame;

/// <summary>
/// UI��ͼ����
/// </summary>
[UIElement(true, "UI/UI_MapWindow",4)]
public class UI_MapWindow : UI_WindowBase
{
    [SerializeField] private RectTransform contentTrans; // ���е�ͼ�顢Icon��ʾ�ĸ�����
    private float contentSize;

    [SerializeField] private GameObject mapItemPrefab; // ������ͼ����UI�е�Ԥ����
    [SerializeField] private GameObject mapIconPrefab; // ����Icon��UI�е�Ԥ����
    [SerializeField] private RectTransform playerIcon; // �������λ�õ�Icon

    private Dictionary<Vector2Int, Image> mapImageDic = new Dictionary<Vector2Int, Image>();    // ��ͼͼƬ�ֵ� Key������

    private float mapChunkImageSize;  // UI��ͼ��ĳߴ�
    private int mapChunkAmount;   // һ����ͼ���ж��ٸ�����   
    private float mapSizeOnWorld;// 3D��ͼ�������е�����
    private Sprite forestSprite;// ɭ�ֵؿ�ľ���

    private float minScale; // ��С�ķŴ���
    private float maxScale = 10; // ���ķŴ���

    public override void Init()
    {
        transform.Find("Scroll View").GetComponent<ScrollRect>().onValueChanged.AddListener(UpdatePlayerIconPos);
    }

    private void Update()
    {
        float scroll = Input.GetAxis("Mouse ScrollWheel");
        if (scroll != 0)
        {
            float newScale = Mathf.Clamp(contentTrans.localScale.x + scroll, minScale, maxScale);
            contentTrans.localScale = new Vector3(newScale, newScale, 0);
        }
    }

    /// <summary>
    /// ��ʼ����ͼ
    /// </summary>
    /// <param name="mapAmount">һ����ͼһ�л�һ���ж��ٸ�Image/Chunk</param>
    /// <param name="mapSizeOnWord">��ͼ��������һ�л�һ���ж��</param>
    /// <param name="forestTexture">ɭ�ֵ���ͼ</param>
    public void InitMap(float mapAmount,int mapChunkSize, float mapSizeOnWord,Texture2D forestTexture)
    { 
        this.mapSizeOnWorld = mapSizeOnWord;
        forestSprite = CreateMapSprite(forestTexture);
        this.mapChunkAmount = mapChunkSize;
        // ���ݳߴ�
        contentSize = mapSizeOnWord * 10;
        contentTrans.sizeDelta = new Vector2(contentSize, contentSize);

        // һ��UI��ͼ��ĳߴ�
        mapChunkImageSize = contentSize / mapAmount;
        minScale = 1050f / contentSize;
    }

    /// <summary>
    /// �������ĵ㣬Ϊ��������ŵ�ʱ�����ĵ���������ڵ�����
    /// </summary>
    /// <param name="viewerPosition"></param>
    public void UpdatePivot(Vector3 viewerPosition)
    {
        float x = viewerPosition.x / mapSizeOnWorld;
        float y = viewerPosition.z / mapSizeOnWorld;
        // �޸�Content��ᵼ��Scroll Rect ����� ��ֵ�޸��¼�=��UpdatePlayerIconPos
        contentTrans.pivot = new Vector2(x, y);
    }

    public void UpdatePlayerIconPos(Vector2 value)
    {
        // ��ҵ�Icon��ȫ����Content�����ĵ�
        playerIcon.anchoredPosition3D = contentTrans.anchoredPosition3D;
    }

    /// <summary>
    /// ���һ����ͼ��
    /// </summary>
    public void AddMapChunk(Vector2Int chunkIndex,List<MapChunkMapObjectModel> mapObjectList,Texture2D texture = null)
    { 
        RectTransform mapChunkRect = Instantiate(mapItemPrefab,contentTrans).GetComponent<RectTransform>();
        // ȷ����ͼ���Image������Ϳ��
        mapChunkRect.anchoredPosition = new Vector2(chunkIndex.x * mapChunkImageSize, chunkIndex.y * mapChunkImageSize);
        mapChunkRect.sizeDelta = new Vector2(mapChunkImageSize, mapChunkImageSize);

        Image mapChunkImage = mapChunkRect.GetComponent<Image>();
        // ɭ�ֵ����
        if (texture == null)
        {
            mapChunkImage.type = Image.Type.Tiled;
            // ��������ש�ı�����Ҫ��һ��Image����ʾ �����ͼ���������ĸ�������
            // ��ͼ��Image�ı���
            float ratio = forestSprite.texture.width / mapChunkImageSize;
            // һ����ͼ�����ж��ٸ�����
            mapChunkImage.pixelsPerUnitMultiplier = mapChunkAmount * ratio;
            mapChunkImage.sprite = forestSprite;
        }
        else mapChunkImage.sprite = CreateMapSprite(texture);

        // TODO:��������ICON

        // TODO:���ع�����Ϊ�϶�����Ҫ����ICON����Ϣ���������Ƴ�����ΪICON����Ļ�����ľ�п��ܻ���ʧ��
        mapImageDic.Add(chunkIndex, mapChunkImage);
    }


    /// <summary>
    /// ���ɵ�ͼ����
    /// </summary>
    private Sprite CreateMapSprite(Texture2D texture)
    {
        return Sprite.Create(texture, new Rect(0, 0, texture.width, texture.height), new Vector2(0.5f, 0.5f));
    }

}
