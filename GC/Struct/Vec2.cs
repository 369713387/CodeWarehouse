using System;
using UnityEngine;

// 优化版本的Vec2结构体 - 实现IEquatable<T>避免装箱
public struct Vec2Optimized : IEquatable<Vec2Optimized>
{
    public int x, y, decimals;
    
    public Vec2Optimized(int x, int y, int decimals)
    {
        this.x = x;
        this.y = y;
        this.decimals = decimals;
    }
    
    // 重写GetHashCode，避免默认实现的装箱
    public override int GetHashCode()
    {
        return HashCode.Combine(x, y, decimals);
    }
    
    // 重写object版本的Equals - 这里会发生装箱，因为参数是object
    public override bool Equals(object obj)
    {
        //Debug.Log("[Boxing] Vec2Optimized.Equals(object obj) 被调用 - 发生装箱!");
        if (obj is Vec2Optimized other)
            return Equals(other);
        return false;
    }
    
    // 实现IEquatable<Vec2Optimized>.Equals - 这个版本不会装箱
    public bool Equals(Vec2Optimized other)
    {
        //Debug.Log("[No Boxing] Vec2Optimized.Equals(Vec2Optimized other) 被调用 - 无装箱");
        return x == other.x && y == other.y && decimals == other.decimals;
    }
    
    public override string ToString()
    {
        return $"Vec2Optimized({x}, {y}, {decimals})";
    }
}

public struct Vec2
{
    public int x;

    public int y;

    public int decimals;

    public Vec2(int _x,int _y,int _decimals)
    {
        this.x = _x;
        this.y = _y;
        this.decimals = _decimals;
    }

    public override int GetHashCode()
    {
        int code = HashCode.Combine(x, y, decimals);
        //Debug.Log($"哈希值：{code}");
        return code;
    }

    public override bool Equals(object obj)
    {
        //Debug.Log("调用object");
        return base.Equals(obj);
    }

    public override string ToString()
    {
        return base.ToString();
    }
}