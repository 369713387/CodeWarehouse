using NUnit.Framework;

namespace RandomWithSeed.Tests
{
	public class DeterministicRandTests
	{
		[Test]
		public void UInt_ShouldBeDeterministic_ForSameInputs()
		{
			uint seed = 12345u;
			uint frame = 6789u;
			uint stream = 42u;
			uint index = 3u;

			uint a = DeterministicRand.UInt(seed, frame, stream, index);
			uint b = DeterministicRand.UInt(seed, frame, stream, index);

			Assert.AreEqual(a, b);
		}

		[Test]
		public void Float01_InRange_AndDeterministic()
		{
			uint seed = 1u;
			uint frame = 2u;
			uint stream = 3u;
			uint index = 4u;

			float a = DeterministicRand.Float01(seed, frame, stream, index);
			float b = DeterministicRand.Float01(seed, frame, stream, index);

			Assert.AreEqual(a, b);
			Assert.GreaterOrEqual(a, 0f);
			Assert.Less(a, 1f);
		}

		[Test]
		public void Range_Int_InclusiveExclusiveBehavior()
		{
			uint seed = 777u;
			uint frame = 0u;
			int min = -5;
			int max = 10; // exclusive

			for (uint i = 0; i < 1000; i++)
			{
				int v = DeterministicRand.Range(min, max, seed, frame, 0u, i);
				Assert.GreaterOrEqual(v, min);
				Assert.Less(v, max);
			}
		}

		[Test]
		public void DifferentFrames_ProduceDifferentValues()
		{
			uint seed = 999u;
			uint stream = 0u;
			uint index = 0u;

			uint f0 = DeterministicRand.UInt(seed, 0u, stream, index);
			uint f1 = DeterministicRand.UInt(seed, 1u, stream, index);

			Assert.AreNotEqual(f0, f1);
		}

		[Test]
		public void DifferentStreams_ProduceDifferentValues_SameFrame()
		{
			uint seed = 456u;
			uint frame = 10u;

			uint s0 = DeterministicRand.UInt(seed, frame, 0u, 0u);
			uint s1 = DeterministicRand.UInt(seed, frame, 1u, 0u);

			Assert.AreNotEqual(s0, s1);
		}

		[Test]
		public void IndexActsAsSequenceCounter()
		{
			uint seed = 5u;
			uint frame = 100u;
			uint stream = 2u;

			uint v0 = DeterministicRand.UInt(seed, frame, stream, 0u);
			uint v1 = DeterministicRand.UInt(seed, frame, stream, 1u);
			uint v2 = DeterministicRand.UInt(seed, frame, stream, 2u);

			Assert.AreNotEqual(v0, v1);
			Assert.AreNotEqual(v1, v2);
			Assert.AreNotEqual(v0, v2);
		}

		[Test]
		public void FixedQ16_16_WithinExpectedRange()
		{
			uint seed = 321u;
			uint frame = 123u;

			for (uint i = 0; i < 256; i++)
			{
				int fx = DeterministicRand.FixedQ16_16(seed, frame, 0u, i);
				Assert.GreaterOrEqual(fx, 0);
				Assert.LessOrEqual(fx, 65535);
			}
		}

		[Test]
		public void Range01ToFloat_InRange()
		{
			float min = -2.5f;
			float max = 7.5f;

			for (uint i = 0; i < 1024; i++)
			{
				float v = DeterministicRand.Range01ToFloat(min, max, 11u, 22u, 33u, i);
				Assert.GreaterOrEqual(v, min);
				Assert.Less(v, max);
			}
		}
	}
}


