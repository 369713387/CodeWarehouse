using System.Runtime.CompilerServices;

namespace RandomWithSeed
{
	/// <summary>
	/// 无状态、可索引、跨端一致的确定性随机工具。
	/// 将随机值视为 (seed, frame, stream, index) 的纯函数，避免调用顺序依赖。
	/// </summary>
	public static class DeterministicRand
	{
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		private static ulong SplitMix64(ulong x)
		{
			x += 0x9E3779B97F4A7C15UL;
			x ^= x >> 30;
			x *= 0xBF58476D1CE4E5B9UL;
			x ^= x >> 27;
			x *= 0x94D049BB133111EBUL;
			x ^= x >> 31;
			return x;
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		private static ulong Stateless(ulong seed, ulong frame, ulong stream, ulong index)
		{
			ulong x = seed;
			x ^= frame * 0x9E3779B97F4A7C15UL;
			x ^= stream * 0xBF58476D1CE4E5B9UL;
			x ^= index * 0x94D049BB133111EBUL;
			return SplitMix64(x);
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static uint UInt(uint seed, uint frame, uint stream = 0, uint index = 0)
		{
			return (uint)Stateless(seed, frame, stream, index);
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static ulong ULong(ulong seed, ulong frame, ulong stream = 0, ulong index = 0)
		{
			return Stateless(seed, frame, stream, index);
		}

		/// <summary>
		/// [0,1) 浮点。使用 53 位双精度构造后转 float，跨平台更稳。
		/// </summary>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static float Float01(uint seed, uint frame, uint stream = 0, uint index = 0)
		{
			const double inv53 = 1.0 / 9007199254740992.0; // 2^53
			ulong r = Stateless(seed, frame, stream, index);
			return (float)(((r >> 11) * inv53));
		}

		/// <summary>
		/// 整数范围 [min, max)（乘高位缩放，避免模偏差）。
		/// </summary>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static int Range(int min, int max, uint seed, uint frame, uint stream = 0, uint index = 0)
		{
			uint r = UInt(seed, frame, stream, index);
			uint range = (uint)(max - min);
			uint scaled = (uint)(((ulong)r * range) >> 32);
			return min + (int)scaled;
		}

		/// <summary>
		/// Q16.16 定点随机 [0, 65535.ffff]，用于跨平台一致的数值逻辑。
		/// </summary>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static int FixedQ16_16(uint seed, uint frame, uint stream = 0, uint index = 0)
		{
			uint r = UInt(seed, frame, stream, index);
			return (int)(r >> 16);
		}

		/// <summary>
		/// 将 [0,1) 映射到 [min, max) 的浮点（如需逻辑完全一致，优先用整数/定点）。
		/// </summary>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static float Range01ToFloat(float min, float max, uint seed, uint frame, uint stream = 0, uint index = 0)
		{
			float t = Float01(seed, frame, stream, index);
			return min + (max - min) * t;
		}
	}
}


