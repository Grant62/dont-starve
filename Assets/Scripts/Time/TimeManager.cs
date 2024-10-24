using System.Collections;
using UnityEngine;
using JKFrame;
using System;
using Sirenix.OdinInspector;

/// <summary>
/// ʱ��״̬����
/// </summary>
[Serializable]
public class TimeStateData
{
    // ����ʱ��
    public float durationTime;
    // ����ǿ��
    public float sunIntensity;
    // ������ɫ
    public Color sunColor;
    // ̫���ĽǶ�
    [OnValueChanged(nameof(SetRotation))]
    public Vector3 sunRotation;
    [HideInInspector]
    public Quaternion sunQuaternion;

    private void SetRotation()
    {
        sunQuaternion = Quaternion.Euler(sunRotation);
    }

    /// <summary>
    /// ��鲢�Ҽ���ʱ��
    /// </summary>
    /// <returns>�Ƿ��ڵ�ǰ״̬</returns>
    public bool CheckAndCalTime(float currTime, TimeStateData nextState, out Quaternion rotation, out Color color, out float sunIntensity)
    {
        // 0~1֮��
        float ratio = 1f - (currTime / durationTime);
        rotation = Quaternion.Slerp(this.sunQuaternion, nextState.sunQuaternion, ratio);
        color = Color.Lerp(this.sunColor, nextState.sunColor, ratio);
        sunIntensity = UnityEngine.Mathf.Lerp(this.sunIntensity, nextState.sunIntensity, ratio);
        // ���ʱ�����0���Ի��ڱ�״̬
        return currTime > 0;
    }
}

/// <summary>
/// ʱ�������
/// </summary>
public class TimeManager : LogicManagerBase<TimeManager>
{
    [SerializeField] private Light mainLight;                   // ̫��
    [SerializeField] private TimeStateData[] timeStateDatas;    // ʱ������
    private int currentStateIndex = 0;
    private float currTime = 0;
    private int dayNum;

    [SerializeField,Range(0,30)] private float timeScale = 1;
    protected override void RegisterEventListener()
    {

    }
    protected override void CancelEventListener()
    {
    }
    private void Start()
    {
        StartCoroutine(UpdateTime());
    }

    private IEnumerator UpdateTime()
    {
        currentStateIndex = 0;   // Ĭ��������
        int nextIndex = currentStateIndex + 1;
        currTime = timeStateDatas[currentStateIndex].durationTime;
        dayNum = 0; // ����
        while (true)
        {
            yield return null;
            currTime -= Time.deltaTime * timeScale;
            // ���㲢�ҵõ�������ص�����
            if (!timeStateDatas[currentStateIndex].CheckAndCalTime(currTime, timeStateDatas[nextIndex], out Quaternion quaternion, out Color color, out float sunIntensity))
            {
                // �л�����һ��״̬
                currentStateIndex = nextIndex;
                // ���߽磬�����ʹ�0��ʼ
                nextIndex = currentStateIndex + 1 >= timeStateDatas.Length ? 0 : currentStateIndex + 1;
                // ������������ϣ�Ҳ����currentStateIndex==0����ô��ζ�ţ�������1
                if (currentStateIndex == 0) dayNum++;
                currTime = timeStateDatas[currentStateIndex].durationTime;
            }
            mainLight.transform.rotation = quaternion;
            mainLight.color = color;
            SetLight(sunIntensity);
        }
    }

    private void SetLight(float intensity)
    { 
        mainLight.intensity = intensity;
        // ���û����������
        RenderSettings.ambientIntensity = intensity;
    }
}
