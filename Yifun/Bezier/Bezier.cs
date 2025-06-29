using JetBrains.Annotations;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Bezier : MonoBehaviour
{
    /// <summary>
    /// 三个控制点的贝塞尔曲线
    /// </summary>
    /// <param name="handles"></param>
    /// <param name="vertexCount"></param>
    /// <returns>返回贝塞尔曲线路径点表</returns>
    public static List<Vector3> BezierCurveWithThree(Transform[] handles, int vertexCount)
    {
        List<Vector3> pointList = new List<Vector3>();
        for (float ratio = 0; ratio <= 1; ratio += 1.0f / vertexCount)
        {
            Vector3 tangentLineVertex1 = Vector3.Lerp(handles[0].position, handles[1].position, ratio);
            Vector3 tangentLineVertex2 = Vector3.Lerp(handles[1].position, handles[2].position, ratio);
            Vector3 bezierPoint = Vector3.Lerp(tangentLineVertex1, tangentLineVertex2, ratio);
            pointList.Add(bezierPoint);
        }
        pointList.Add(handles[2].position);

        return pointList;
    }

    /// <summary>
    /// 超过三个控制点的贝塞尔曲线
    /// </summary>
    /// <param name="handlesPositions"></param>
    /// <param name="vertexCount"></param>
    public static List<Vector3> BezierCurveWithUnlimitPoints(Vector3[] handlesPositions, int vertexCount)
    {
        List<Vector3> pointList = new List<Vector3>();
        for (float ratio = 0; ratio <= 1; ratio += 1.0f / vertexCount)
        {
            pointList.Add(UnlimitBezierCurve(handlesPositions, ratio));
        }
        pointList.Add(handlesPositions[handlesPositions.Length - 1]);

        return pointList;
    }
    public static Vector3 UnlimitBezierCurve(Vector3[] posArr, float t)
    {
        Vector3[] temp = new Vector3[posArr.Length];
        for (int i = 0; i < temp.Length; i++)
        {
            temp[i] = posArr[i];
        }
        int n = temp.Length - 1;
        for (int i = 0; i < n; i++)
        {
            for (int j = 0; j < n - i; j++)
            {
                temp[j] = Vector3.Lerp(temp[j], temp[j + 1], t);
            }
        }
        return temp[0];
    }
}