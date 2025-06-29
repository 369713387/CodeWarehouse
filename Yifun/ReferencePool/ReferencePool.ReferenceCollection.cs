using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Threading;

namespace Framework.Pool
{
    public static partial class ReferencePool
    {
        private sealed class ReferenceCollection
        {
            // 使用 ConcurrentQueue 替代 Queue + lock
            private readonly ConcurrentQueue<IReference> m_References;
            private readonly Type m_ReferenceType;
            private readonly Func<IReference> m_Factory;
            
            // 使用 Interlocked 操作替代锁
            private int m_UsingReferenceCount;
            private int m_AcquireReferenceCount;
            private int m_ReleaseReferenceCount;
            private int m_AddReferenceCount;
            private int m_RemoveReferenceCount;

            // 严格检查时使用的已释放引用集合
            private readonly HashSet<IReference> m_ReleasedReferences;
            private readonly object m_ReleasedReferencesLock;

            public ReferenceCollection(Type referenceType)
            {
                m_References = new ConcurrentQueue<IReference>();
                m_ReferenceType = referenceType;
                m_Factory = GetOrCreateFactory(referenceType);
                
                if (m_EnableStrictCheck)
                {
                    m_ReleasedReferences = new HashSet<IReference>();
                    m_ReleasedReferencesLock = new object();
                }
            }

            public Type ReferenceType
            {
                [MethodImpl(MethodImplOptions.AggressiveInlining)]
                get { return m_ReferenceType; }
            }

            public int UnusedReferenceCount
            {
                [MethodImpl(MethodImplOptions.AggressiveInlining)]
                get { return m_References.Count; }
            }

            public int UsingReferenceCount
            {
                [MethodImpl(MethodImplOptions.AggressiveInlining)]
                get { return Thread.VolatileRead(ref m_UsingReferenceCount); }
            }

            public int AcquireReferenceCount
            {
                [MethodImpl(MethodImplOptions.AggressiveInlining)]
                get { return Thread.VolatileRead(ref m_AcquireReferenceCount); }
            }

            public int ReleaseReferenceCount
            {
                [MethodImpl(MethodImplOptions.AggressiveInlining)]
                get { return Thread.VolatileRead(ref m_ReleaseReferenceCount); }
            }

            public int AddReferenceCount
            {
                [MethodImpl(MethodImplOptions.AggressiveInlining)]
                get { return Thread.VolatileRead(ref m_AddReferenceCount); }
            }

            public int RemoveReferenceCount
            {
                [MethodImpl(MethodImplOptions.AggressiveInlining)]
                get { return Thread.VolatileRead(ref m_RemoveReferenceCount); }
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public T Acquire<T>() where T : class, IReference, new()
            {
                if (typeof(T) != m_ReferenceType)
                {
                    //Log.Error("Type is invalid.");
                }

                Interlocked.Increment(ref m_UsingReferenceCount);
                Interlocked.Increment(ref m_AcquireReferenceCount);
                
                if (m_References.TryDequeue(out IReference reference))
                {
                    return (T)reference;
                }

                Interlocked.Increment(ref m_AddReferenceCount);
                return new T();
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public IReference Acquire()
            {
                Interlocked.Increment(ref m_UsingReferenceCount);
                Interlocked.Increment(ref m_AcquireReferenceCount);
                
                if (m_References.TryDequeue(out IReference reference))
                {
                    return reference;
                }

                Interlocked.Increment(ref m_AddReferenceCount);
                return m_Factory();
            }

            public void Release(IReference reference)
            {
                reference.Clear();
                
                if (m_EnableStrictCheck && m_ReleasedReferences != null)
                {
                    lock (m_ReleasedReferencesLock)
                    {
                        if (!m_ReleasedReferences.Add(reference))
                        {
                            // Log.Error("The reference has been released.");
                            return;
                        }
                    }
                }

                m_References.Enqueue(reference);
                Interlocked.Increment(ref m_ReleaseReferenceCount);
                Interlocked.Decrement(ref m_UsingReferenceCount);
            }

            public void Add<T>(int count) where T : class, IReference, new()
            {
                if (typeof(T) != m_ReferenceType)
                {
                    //Log.Error("Type is invalid.");
                    return;
                }

                Interlocked.Add(ref m_AddReferenceCount, count);
                for (int i = 0; i < count; i++)
                {
                    m_References.Enqueue(new T());
                }
            }

            public void Add(int count)
            {
                Interlocked.Add(ref m_AddReferenceCount, count);
                for (int i = 0; i < count; i++)
                {
                    m_References.Enqueue(m_Factory());
                }
            }

            public void Remove(int count)
            {
                int actualCount = 0;
                for (int i = 0; i < count && m_References.TryDequeue(out _); i++)
                {
                    actualCount++;
                }
                
                if (actualCount > 0)
                {
                    Interlocked.Add(ref m_RemoveReferenceCount, actualCount);
                }
            }

            public void RemoveAll()
            {
                int removedCount = 0;
                while (m_References.TryDequeue(out _))
                {
                    removedCount++;
                }
                
                if (removedCount > 0)
                {
                    Interlocked.Add(ref m_RemoveReferenceCount, removedCount);
                }
                
                if (m_EnableStrictCheck && m_ReleasedReferences != null)
                {
                    lock (m_ReleasedReferencesLock)
                    {
                        m_ReleasedReferences.Clear();
                    }
                }
            }
        }
    }
}

