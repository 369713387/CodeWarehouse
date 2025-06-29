using System;
using System.Collections.Generic;
using System.Text;

namespace Framework.Pool
{
    /// <summary>
    /// 引用池监控器
    /// </summary>
    public static class ReferencePoolMonitor
    {
        /// <summary>
        /// 获取引用池使用报告
        /// </summary>
        public static string GetUsageReport()
        {
            var infos = ReferencePool.GetAllReferencePoolInfos();
            var sb = new StringBuilder();
            
            sb.AppendLine("=== Reference Pool Usage Report ===");
            sb.AppendFormat("Total Pools: {0}\n", infos.Length);
            sb.AppendLine();
            
            foreach (var info in infos)
            {
                sb.AppendFormat("Type: {0}\n", info.Type.Name);
                sb.AppendFormat("  Unused: {0}, Using: {1}\n", info.UnusedReferenceCount, info.UsingReferenceCount);
                sb.AppendFormat("  Acquired: {0}, Released: {1}\n", info.AcquireReferenceCount, info.ReleaseReferenceCount);
                sb.AppendFormat("  Added: {0}, Removed: {1}\n", info.AddReferenceCount, info.RemoveReferenceCount);
                
                // 计算泄漏检测
                int expectedUsing = info.AcquireReferenceCount - info.ReleaseReferenceCount;
                if (expectedUsing != info.UsingReferenceCount)
                {
                    sb.AppendFormat("  ⚠️ POTENTIAL LEAK: Expected {0}, Actual {1}\n", expectedUsing, info.UsingReferenceCount);
                }
                sb.AppendLine();
            }
            
            return sb.ToString();
        }

        /// <summary>
        /// 检查内存泄漏
        /// </summary>
        public static Dictionary<Type, int> CheckLeaks()
        {
            var leaks = new Dictionary<Type, int>();
            var infos = ReferencePool.GetAllReferencePoolInfos();
            
            foreach (var info in infos)
            {
                int expectedUsing = info.AcquireReferenceCount - info.ReleaseReferenceCount;
                int actualUsing = info.UsingReferenceCount;
                
                if (expectedUsing != actualUsing)
                {
                    leaks[info.Type] = actualUsing - expectedUsing;
                }
            }
            
            return leaks;
        }
    }
} 