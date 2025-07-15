using System.Collections.Generic;
using UnityEngine;

public class FunnelSystem : MonoBehaviour
{
    public enum FunnelMode
    {
        AllAtMaster,
        AllAtMasterNegativeZ,
        AllAtMasterPositiveZ,
        SplitZ
    }

    public Transform Master;
    public List<Transform> Funnels = new List<Transform>();
    public FunnelMode Mode = FunnelMode.AllAtMaster;
    public float ZOffset = 1.0f;
    public Vector2 XRange = new Vector2(-5, 5);
    public Vector2 YRange = new Vector2(-5, 5);

    private FunnelMode _lastMode;

    void Start()
    {
        _lastMode = Mode;
        ApplyMode();
    }

    void Update()
    {
        if (Mode != _lastMode)
        {
            ApplyMode();
            _lastMode = Mode;
        }
    }

    void ApplyMode()
    {
        switch (Mode)
        {
            case FunnelMode.AllAtMaster:
                SetAllAtMaster();
                break;
            case FunnelMode.AllAtMasterNegativeZ:
                SetAllAtMasterNegativeZ();
                break;
            case FunnelMode.AllAtMasterPositiveZ:
                SetAllAtMasterPositiveZ();
                break;
            case FunnelMode.SplitZ:
                SetSplitZ();
                break;
        }
    }

    void SetAllAtMaster()
    {
        foreach (var funnel in Funnels)
        {
            if (funnel != null && Master != null)
            {
                funnel.position = Master.position;
                funnel.rotation = Master.rotation;
            }
        }
    }

    void SetAllAtMasterNegativeZ()
    {
        for (int i = 0; i < Funnels.Count; i++)
        {
            var funnel = Funnels[i];
            if (funnel != null && Master != null)
            {
                float x = Random.Range(XRange.x, XRange.y);
                float y = Random.Range(YRange.x, YRange.y);
                // 以 Master 的 forward 為基準
                Vector3 local = new Vector3(x, y, -ZOffset);
                Vector3 world = Master.TransformPoint(local);
                funnel.position = world;
                funnel.rotation = Master.rotation;
            }
        }
    }

    void SetAllAtMasterPositiveZ()
    {
        for (int i = 0; i < Funnels.Count; i++)
        {
            var funnel = Funnels[i];
            if (funnel != null && Master != null)
            {
                float x = Random.Range(XRange.x, XRange.y);
                float y = Random.Range(YRange.x, YRange.y);
                Vector3 local = new Vector3(x, y, ZOffset);
                Vector3 world = Master.TransformPoint(local);
                funnel.position = world;
                funnel.rotation = Master.rotation;
            }
        }
    }

    void SetSplitZ()
    {
        int half = Funnels.Count / 2;
        for (int i = 0; i < Funnels.Count; i++)
        {
            var funnel = Funnels[i];
            if (funnel != null && Master != null)
            {
                float x = Random.Range(XRange.x, XRange.y);
                float y = Random.Range(YRange.x, YRange.y);
                float z = (i < half) ? ZOffset : -ZOffset;
                Vector3 local = new Vector3(x, y, z);
                Vector3 world = Master.TransformPoint(local);
                funnel.position = world;
                funnel.rotation = Master.rotation;
            }
        }
    }

    void OnDrawGizmos()
    {
        if (Master == null) return;
        // 以 Master 的 forward 為基準畫平面
        Vector3[] plusCorners = new Vector3[4];
        Vector3[] minusCorners = new Vector3[4];
        plusCorners[0] = Master.TransformPoint(new Vector3(XRange.x, YRange.x, ZOffset));
        plusCorners[1] = Master.TransformPoint(new Vector3(XRange.x, YRange.y, ZOffset));
        plusCorners[2] = Master.TransformPoint(new Vector3(XRange.y, YRange.y, ZOffset));
        plusCorners[3] = Master.TransformPoint(new Vector3(XRange.y, YRange.x, ZOffset));
        minusCorners[0] = Master.TransformPoint(new Vector3(XRange.x, YRange.x, -ZOffset));
        minusCorners[1] = Master.TransformPoint(new Vector3(XRange.x, YRange.y, -ZOffset));
        minusCorners[2] = Master.TransformPoint(new Vector3(XRange.y, YRange.y, -ZOffset));
        minusCorners[3] = Master.TransformPoint(new Vector3(XRange.y, YRange.x, -ZOffset));
        // 畫+z平面（綠色）
        Debug.DrawLine(plusCorners[0], plusCorners[1], Color.green);
        Debug.DrawLine(plusCorners[1], plusCorners[2], Color.green);
        Debug.DrawLine(plusCorners[2], plusCorners[3], Color.green);
        Debug.DrawLine(plusCorners[3], plusCorners[0], Color.green);
        // 畫-z平面（紅色）
        Debug.DrawLine(minusCorners[0], minusCorners[1], Color.red);
        Debug.DrawLine(minusCorners[1], minusCorners[2], Color.red);
        Debug.DrawLine(minusCorners[2], minusCorners[3], Color.red);
        Debug.DrawLine(minusCorners[3], minusCorners[0], Color.red);
    }
} 