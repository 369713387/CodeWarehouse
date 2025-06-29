using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using UnityEngine;

public class BezierController : MonoBehaviour
{
    public bool isRunning;
    public bool isShowGizmos;
    public bool isShowLineRenderer;
    public bool isShowFixedPoints;
    public Transform player;
    public Transform ball;
    [Header("控制手柄")]
    public Vector3[] handles;
    Vector3[] handlesOriginalPoint;//控制带你的原始位置
    public int vertexCount;
    List<Vector3> pointList = new List<Vector3>();
    [Header("固定点距离点集合")]
    public List<Vector3> fixedSpacePoints = new List<Vector3>();
    public float fixedSpace;
    [Header("线渲染工具")]
    public LineRenderer lineRenderer;

    bool isCurve = false;

    private void Start()
    {
        player = GameObject.Find("Head").transform;

        isCurve = true;
        Init();
        handlesOriginalPoint = new Vector3[handles.Length];
        for (int i = 0; i < handles.Length; i++)
        {
            handlesOriginalPoint[i] = handles[i];
        }
        Running();
    }

    private void Update()
    {
        Init();
        if (CheckHandlesMove() && isRunning)
        {
            Running();
        }
    }

    void Init()
    {
        Vector3 mid = Vector3.zero;

        Ray ray = new Ray(player.position, player.transform.forward);

        RaycastHit hit;

        if (Physics.Raycast(ray, out hit, int.MaxValue, 1 << LayerMask.NameToLayer("Wall")))
        {
            mid = hit.point;

            Debug.Log(mid);
        }

        handles = new Vector3[] { ball.position, mid, player.position };

        vertexCount = (int)Mathf.Clamp(Vector3.Distance(ball.position, player.position) * 2, 2, 30);
    }

    void Running()
    {
        pointList = Bezier.BezierCurveWithUnlimitPoints(handles, vertexCount);
        fixedSpacePoints = YF_Math.GetEqualySpacePoints(pointList, fixedSpace);

        if (isShowLineRenderer)
        {
            lineRenderer.positionCount = pointList.Count;
            lineRenderer.SetPositions(pointList.ToArray());
        }

        Debug.Log(Vector3.Angle(ball.position, player.forward));

        if (Vector3.Angle(ball.position, player.forward) > 340
            || Vector3.Angle(ball.position, player.forward) < 20)
        {
            isCurve = false;
        }
        if (isCurve)
        {
            ball.Translate((pointList[1] - pointList[0]).normalized * 2f * Time.deltaTime);
        }
        else
        {
            ball.Translate((player.position - ball.position).normalized * 2f * Time.deltaTime);
        }

    }

    //获取路径点
    public List<Vector3> GetPointList()
    {
        return Bezier.BezierCurveWithUnlimitPoints(handles, vertexCount);
    }

    //获取固定间隔距离位置
    public List<Vector3> GetFixedSpacePoints()
    {
        CheckHandlesMove();
        List<Vector3> list = Bezier.BezierCurveWithUnlimitPoints(handles, vertexCount);
        return YF_Math.GetEqualySpacePoints(list, fixedSpace);
    }

    //控制点是否发生更新： 是否改变了控制点的数量或者控制点是否发生了位移
    bool CheckHandlesMove()
    {
        bool hasMove = false;

        if (handlesOriginalPoint == null) handlesOriginalPoint = new Vector3[] { };
        if (handlesOriginalPoint.Length != handles.Length)
        {
            handlesOriginalPoint = new Vector3[handles.Length];
            for (int i = 0; i < handles.Length; i++)
            {
                handlesOriginalPoint[i] = handles[i];
            }
            return true;
        }

        for (int i = 0; i < handles.Length; i++)
        {
            if (handles[i] != handlesOriginalPoint[i])
            {
                hasMove = true;
                break;
            }
        }
        return hasMove;
    }

    private void OnDrawGizmos()
    {
        if (isShowGizmos)
        {
            if (handles.Length > 3)
            {
                #region 无限制顶点数

                Gizmos.color = Color.green;

                for (int i = 0; i < handles.Length - 1; i++)
                {
                    Gizmos.DrawLine(handles[i], handles[i + 1]);
                }

                Gizmos.color = Color.red;

                Vector3[] temp = new Vector3[handles.Length];
                for (int i = 0; i < temp.Length; i++)
                {
                    temp[i] = handles[i];
                }
                int n = temp.Length - 1;
                for (float ratio = 0.5f / vertexCount; ratio < 1; ratio += 1.0f / vertexCount)
                {
                    for (int i = 0; i < n - 2; i++)
                    {
                        Gizmos.DrawLine(Vector3.Lerp(temp[i], temp[i + 1], ratio), Vector3.Lerp(temp[i + 2], temp[i + 3], ratio));
                    }
                }
                #endregion
            }
            else
            {
                #region 顶点数为3

                Gizmos.color = Color.green;

                Gizmos.DrawLine(handles[0], handles[1]);

                Gizmos.color = Color.green;

                Gizmos.DrawLine(handles[1], handles[2]);

                Gizmos.color = Color.red;

                for (float ratio = 0.5f / vertexCount; ratio < 1; ratio += 1.0f / vertexCount)
                {

                    Gizmos.DrawLine(Vector3.Lerp(handles[0], handles[1], ratio), Vector3.Lerp(handles[1], handles[2], ratio));

                }

                #endregion
            }
        }

        if (isShowFixedPoints)
        {
            #region 显示固定距离的点列表
            Gizmos.color = Color.green;
            foreach (Vector3 point in fixedSpacePoints)
            {
                Gizmos.DrawSphere(point, 0.3f);
            }
            #endregion
        }

    }
}