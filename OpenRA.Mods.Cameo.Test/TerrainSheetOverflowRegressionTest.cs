using System;
using System.Collections;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using NUnit.Framework;
using OpenRA.Graphics;
using OpenRA.Primitives;

namespace OpenRA.Mods.Cameo.Test
{
	// Exercises the pinned engine's real batch selector without a GPU or game.
	[TestFixture, NonParallelizable]
	public sealed class TerrainSheetOverflowRegressionTest
	{
		sealed class Buffer : IVertexBuffer<Vertex>
		{
			public void Bind() { }
			public void SetData(Vertex[] vertices, int length) { }
			public void SetData(ref Vertex[] vertices, int length) { }
			public void SetData(Vertex[] vertices, int offset, int start, int length) { }
			public void Dispose() { }
		}
		public class Context : DispatchProxy
		{
			protected override object Invoke(MethodInfo method, object[] args) =>
				method.Name == "CreateEmptyVertexBuffer" ? new Buffer() : throw new NotSupportedException(method.Name);
		}
		const BindingFlags Fields = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
		static FieldInfo Field(Type type, string name) => type.GetField(name, Fields)
			?? throw new MissingFieldException(type.FullName, name);
		static void Set(object target, string field, object value) => Field(target.GetType(), field).SetValue(target, value);
		static Sheet Sheet() => new(SheetType.Indexed, new Size(1, 1));

		[TestCase(false)]
		[TestCase(true)]
		public void FourteenDistinctSheetsArePartitionedWithoutLayerOverflow(bool secondary)
		{
			var oldRenderer = Game.Renderer;
			try
			{
				var renderer = (Renderer)RuntimeHelpers.GetUninitializedObject(typeof(Renderer));
				Set(renderer, "<Context>k__BackingField", DispatchProxy.Create<IGraphicsContext, Context>());
				Game.Renderer = renderer;
				var map = (Map)RuntimeHelpers.GetUninitializedObject(typeof(Map));
				typeof(Map).GetProperty("MapSize").SetValue(map, new Size(14, 1));
				var layer = (TerrainSpriteLayer)RuntimeHelpers.GetUninitializedObject(typeof(TerrainSpriteLayer));
				Set(layer, "map", map);
				Set(layer, "worldRenderer", RuntimeHelpers.GetUninitializedObject(typeof(WorldRenderer)));
				Set(layer, "vertexRowStride", 56);
				var cellBatches = Enumerable.Repeat(-1, 14).ToArray();
				Set(layer, "batchForCell", cellBatches);
				var batches = (IList)Activator.CreateInstance(Field(typeof(TerrainSpriteLayer), "batches").FieldType);
				Set(layer, "batches", batches);
				var select = typeof(TerrainSpriteLayer).GetMethod("FindBatch", Fields);
				Assert.That(select, Is.Not.Null, "The pinned engine must retain the multi-batch overflow fix.");
				var primaries = Enumerable.Range(0, 14).Select(_ => Sheet()).ToArray();
				var secondaries = secondary ? Enumerable.Range(0, 14).Select(_ => Sheet()).ToArray() : new Sheet[14];
				for (var i = 0; i < 14; i++)
				{
					var batch = select.Invoke(layer, new object[] { new MPos(i, 0), primaries[i], secondaries[i] });
					var add = batch.GetType().GetMethod("GetOrAddSheetIndex", Fields);
					Assert.DoesNotThrow(() => add.Invoke(batch, new object[] { primaries[i] }));
					Assert.DoesNotThrow(() => add.Invoke(batch, new object[] { secondaries[i] }));
					cellBatches[i] = batches.IndexOf(batch);
				}
				Assert.That(batches.Count, Is.EqualTo(secondary ? 4 : 2));
				foreach (var batch in batches)
					Assert.That((int)Field(batch.GetType(), "SheetCount").GetValue(batch), Is.LessThanOrEqualTo(SpriteRenderer.SheetCount));
				var count = batches.Count;
				for (var i = 0; i < 14; i++)
					Assert.That(select.Invoke(layer, new object[] { new MPos(i, 0), primaries[i], secondaries[i] }),
						Is.SameAs(batches[cellBatches[i]]));
				Assert.That(batches.Count, Is.EqualTo(count), "Reusing resource frames must not allocate another batch.");
			}
			finally { Game.Renderer = oldRenderer; }
		}
	}
}
