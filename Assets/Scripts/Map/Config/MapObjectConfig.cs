using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using JKFrame;
using Sirenix.OdinInspector;

[CreateAssetMenu(fileName = "��ͼ��������", menuName = "Config/��ͼ��������")]
public class MapObjectConfig : ConfigBase
{
    [LabelText("�յ� ��������Ʒ")]
    public bool IsEmpty = false;
    [LabelText("���ڵĵ�ͼ��������")]
    public MapVertexType MapVertexType;
    [LabelText("���ɵ�Ԥ����")]
    public GameObject Prefab;
    [LabelText("���ɸ��� Ȩ������")]
    public int Probability;
}
