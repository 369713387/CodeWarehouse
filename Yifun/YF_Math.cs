using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

public class YF_Math
{
    // 法二 面积法
    /// <summary>
    /// 面积法求点到直线的距离.
    /// </summary>
    /// <param name="p">待求点.</param>
    /// <param name="p1">直线端点</param>
    /// <param name="p2">直线端点</param>
    /// <returns></returns>
    public static float DistanceByArea(Vector3 pB, Vector3 pA1, Vector3 pA2)
    {
        Vector3 a2A1 = pA1 - pA2;
        Vector3 a2B = pB - pA2;

        Vector3 normal = Vector3.Cross(a2A1, a2B);
        Vector3 a2A1Temp = Vector3.Cross(a2A1, normal).normalized;

        return Mathf.Abs(Vector3.Dot(a2B, a2A1Temp));
    }

    /// <summary>
    /// 三个控制点的贝塞尔曲线
    /// </summary>
    /// <param name="handles">0,起点,1控制点,2终点</param>
    /// <param name="vertexCount">路径点的个数</param>
    /// <returns>返回贝塞尔曲线路径点表</returns>
    public static List<Vector3> BezierCurveWithThree(Vector3[] handles, int vertexCount)
    {
        List<Vector3> pointList = new List<Vector3>();
        for (float ratio = 0; ratio <= 1; ratio += 1.0f / vertexCount)
        {
            Vector3 tangentLineVertex1 = Vector3.Lerp(handles[0], handles[1], ratio);
            Vector3 tangentLineVertex2 = Vector3.Lerp(handles[1], handles[2], ratio);
            Vector3 bezierPoint = Vector3.Lerp(tangentLineVertex1, tangentLineVertex2, ratio);
            pointList.Add(bezierPoint);
        }
        pointList.Add(handles[2]);

        return pointList;
    }

    /// <summary>
    /// 获取从起点到末点的固定点表路径-包含起点和末点,包含起点
    /// </summary>
    /// <param name="pointWay">路径点表，</param>
    /// <param name="space">点与点之间的距离</param>
    /// <returns></returns>
    public static List<Vector3> GetEqualySpacePoints(List<Vector3> pointWay, float space)
    {
        List<Vector3> points = new List<Vector3>();
        if (pointWay == null || pointWay.Count == 0) return points;
        Vector3 indexPoint = pointWay[0];
        float tempSpaceDistance = 0;
        points.Add(pointWay[0]);
        foreach (Vector3 point in pointWay)
        {
            while (indexPoint != point)
            {
                Vector3 tempPoint = indexPoint;
                indexPoint = Vector3.MoveTowards(indexPoint, point, 0.02f);
                tempSpaceDistance += Vector3.Distance(tempPoint, indexPoint);
                if (tempSpaceDistance >= space)
                {
                    tempSpaceDistance = 0;
                    points.Add(indexPoint);
                }
            }
        }

        return points;
    }

    /// <summary>
    /// 获取从起点到末点的固定点表路径-包含起点和末点,包含起点
    /// </summary>
    /// <param name="pointWay">路径点表</param>
    /// <param name="space">间隔距离</param>
    /// <returns></returns>
    public static List<Vector3> GetEqualySpacePointsQuickly(List<Vector3> pointWay, float space)
    {
        List<Vector3> points = new List<Vector3>();

        #region 移动索引点
        int index = 0;
        float offset = 0;
        #endregion

        float distance = 0;
        while (index < pointWay.Count - 1)
        {
            if (space <= 0) break;

            Vector3 indexPoint = pointWay[index] + (pointWay[index + 1] - pointWay[index]).normalized * offset;
            float dis = Vector3.Distance(indexPoint, pointWay[index + 1]);
            float tempDistance = dis + distance;
            if (tempDistance >= space)
            {
                offset += (space - distance);
                if (distance == 0 && offset != 0) points.Add(indexPoint);//此处offset==0说明发生了两个相邻的点重合的情况，所以相邻的后一个点不需要被加入表
                distance = 0;
            }
            else
            {
                distance = tempDistance;
                if (offset != 0 || index == 0) points.Add(indexPoint);
                offset = 0;
                index += 1;
            }
        }

        return points;
    }

    /// <summary>
    /// 从一个点表中获取一个线性距离距离内的点，最后一点可以是截取的一个点
    /// </summary>
    /// <param name="pointWay"></param>
    /// <param name="distance"></param>
    /// <returns></returns>
    public static List<Vector3> GetDistancePoints(List<Vector3> pointWay, float distance)
    {
        List<Vector3> points = new List<Vector3>();
        float tDistance = 0;
        for (int i = 0; i < pointWay.Count; i++)
        {
            if (points.Count == 0)
            {
                points.Add(pointWay[0]);
                continue;
            }

            float dis = Vector3.Distance(pointWay[i - 1], pointWay[i]);
            float tDis = tDistance + dis;

            if (tDis < distance)
            {
                points.Add(pointWay[i]);
                tDistance = tDis;
            }
            else if (tDis == distance)
            {
                points.Add(pointWay[i]);
                tDistance = tDis;
                break;
            }
            else if (tDis > distance) //生成新的点，并添加到points
            {
                Vector3 dir = pointWay[i] - pointWay[i - 1];
                Vector3 newPoint = pointWay[i - 1] + dir.normalized * (distance - tDistance);
                points.Add(newPoint);
                tDistance = distance;
                break;
            }
        }
        if (tDistance != distance) points = new List<Vector3>();
        return points;
    }

    /// <summary>
    /// 获取一个路径的线性长度
    /// </summary>
    /// <param name="pointWay"></param>
    /// <returns></returns>
    public static float GetPathLength(List<Vector3> pointWay)
    {
        float distance = 0;
        Vector3 tempPoint = Vector3.zero;
        if (pointWay.Count > 0) tempPoint = pointWay[0];
        foreach (Vector3 v in pointWay)
        {
            distance += Vector3.Distance(v, tempPoint);
            tempPoint = v;
        }
        return distance;
    }

    /// <summary>
    /// 获取一个线性布局的点集（1010101），点之间等距，靠中间布局，即当原子数量只有一个的时候，放在中间
    /// </summary>
    /// <param name="centerPos">中心位置</param>
    /// <param name="dir">布局方向</param>
    /// <param name="length">布局长度</param>
    /// <param name="count">原子数量</param>
    /// <returns></returns>
    public static List<Vector3> GetLineGridWithCenterPoints(Vector3 centerPos, Vector3 dir, float length, int count, UnityAction<Vector3> del = null)
    {
        List<Vector3> points = new List<Vector3>();

        float angleSpace = length / (count - 1);
        float firstAngle = length / 2;

        if (count == 1)
        {
            points.Add(centerPos);

            if (del != null) del(centerPos);
            return points;
        }

        for (int i = 0; i < count; i++)
        {
            Vector3 pos = centerPos + dir * (firstAngle - angleSpace * i);
            points.Add(pos);
            if (del != null) del(pos);
        }
        return points;
    }

    /// <summary>
    /// 获取一个扇形上的固定数量的点,坐标系为世界坐标
    /// 1010101010101
    /// </summary>
    /// <param name="count"></param>
    /// <param name="pos"></param>
    /// <param name="angel"></param>
    /// <param name="radius"></param>
    /// <param name="del"></param>
    /// <returns></returns>
    public static List<TransformGeoData> GetSectorStaticCountDatas(int count, Vector3 pos, float angel, float radius, UnityAction<TransformGeoData> del = null)
    {
        List<TransformGeoData> trans = new List<TransformGeoData>();

        //0101010
        float angleSpace = angel / (count - 1);
        float firstAngle = angel / 2;
        if (count == 1)
        {
            TransformGeoData transformGeoData = new TransformGeoData();
            transformGeoData.position = pos + Vector3.forward * radius;
            transformGeoData.rotation = Quaternion.Euler(0, 0, 0);
            if (del != null) del(transformGeoData);
            trans.Add(transformGeoData);
            return trans;
        }

        for (int i = 0; i < count; i++)
        {
            TransformGeoData transformGeoData = new TransformGeoData();
            transformGeoData.rotation = Quaternion.Euler(0, firstAngle - angleSpace * i, 0);
            transformGeoData.position = pos + (transformGeoData.rotation * Vector3.forward) * radius;
            if (del != null) del(transformGeoData);
            trans.Add(transformGeoData);
        }

        return trans;

    }

    /// <summary>
    /// 获取一个环上的点表，相邻点之间的距离相等
    /// </summary>
    /// <param name="centerPos"></param>
    /// <param name="radius"></param>
    /// <param name="count"></param>
    /// <param name="del"></param>
    /// <returns></returns>
    public static List<Vector3> GetRingStaticCountPoints(Vector3 centerPos, float radius, int count, Vector3 forward, UnityAction<Vector3> del = null)
    {
        List<Vector3> points = new List<Vector3>();

        float length = radius * Mathf.PI * 2;//获取圆圈周长
        if (count == 0) return points;

        float angle = 360f / count;

        for (int i = 0; i < count; i++)
        {
            Vector3 dir;
            if (i == 0)
            {
                dir = forward;
            }
            else
            {
                dir = Quaternion.Euler(0f, angle * i, 0f) * forward;
            }
            Vector3 pos = centerPos + dir * radius;
            if (del != null) del(pos);
            points.Add(pos);
        }

        return points;
    }

    /// <summary>
    /// 目标是否在扇形区域内(也可认为是锥形区域)
    /// </summary>
    /// <param name="centerPos">扇形中心</param>
    /// <param name="forward">中心方向</param>
    /// <param name="angle">角度范围</param>
    /// <param name="radius">半径</param>
    /// <param name="targetPos">目标位置</param>
    /// <returns></returns>
    public static bool InAngleRange(Vector3 centerPos, Vector3 forward, float angle, float radius, Vector3 targetPos)
    {
        Vector3 dir = targetPos - centerPos;
        float ang1 = Vector3.Angle(forward, dir);
        if (ang1 > Mathf.Abs(angle / 2)) return false;
        float dis1 = Vector3.Distance(targetPos, centerPos);
        if (dis1 > radius) return false;
        return true;
    }
}

/// <summary>
/// 一个Transfor的几何信息
/// </summary>
public struct TransformGeoData
{
    public Vector3 position;
    public Quaternion rotation;
}
