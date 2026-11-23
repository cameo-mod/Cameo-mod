using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using NUnit.Framework;
using OpenRA.Mods.CA.Traits;
using OpenRA.Mods.CA.Traits.BotModules;

namespace OpenRA.Mods.Cameo.Test
{
	[TestFixture]
	public sealed class CameoLogEventShapeTest
	{
		static readonly HashSet<Type> EventTypes = new()
		{
			typeof(MissionEvent), typeof(PlayerIdentity), typeof(CellXY),
			typeof(BotPassiveSquadEventSnapshot), typeof(BotPassiveUnitPositionSnapshot),
			typeof(BotPassiveAttackCommitSnapshot), typeof(BotPassiveDispatchCheckSnapshot),
			typeof(BotPassiveCandidateFailure), typeof(BotPassiveJobEndSnapshot), typeof(BotPassiveWaveGroup),
			typeof(BotPassiveWaveStartSnapshot), typeof(BotPassiveWaveSampleSnapshot),
			typeof(BotPassiveRallyEndSnapshot), typeof(BotPassiveWaveExitSnapshot), typeof(BotPassiveHealthSnapshot),
		};

		static bool Allowed(Type type, HashSet<Type> seen, HashSet<Type> allowedEvents)
		{
			var underlying = Nullable.GetUnderlyingType(type);
			if (underlying != null)
				return Allowed(underlying, seen, allowedEvents);
			if (type == typeof(string) || type == typeof(bool) || type.IsEnum ||
				type == typeof(byte) || type == typeof(sbyte) || type == typeof(short) || type == typeof(ushort) ||
				type == typeof(int) || type == typeof(uint) || type == typeof(long) || type == typeof(ulong))
				return true;
			if (type.IsGenericType && (type.GetGenericTypeDefinition() == typeof(IReadOnlyList<>) ||
				type.GetGenericTypeDefinition() == typeof(ReadOnlyCollection<>)))
				return Allowed(type.GetGenericArguments()[0], seen, allowedEvents);
			if (!allowedEvents.Contains(type) || (!type.IsValueType && !type.IsSealed))
				return false;
			if (!seen.Add(type))
				return true;
			if (type.GetFields(BindingFlags.Public | BindingFlags.Instance).Length != 0)
				return false;
			foreach (var property in type.GetProperties(BindingFlags.Public | BindingFlags.Instance))
			{
				var setter = property.SetMethod;
				if (setter != null && !setter.ReturnParameter.GetRequiredCustomModifiers().Contains(typeof(IsExternalInit)))
					return false;
				if (!Allowed(property.PropertyType, seen, allowedEvents))
					return false;
			}
			return true;
		}

		[Test]
		public void AllFacadeEventsContainOnlyDetachedValues()
		{
			foreach (var type in EventTypes)
				Assert.That(Allowed(type, new HashSet<Type>(), EventTypes), Is.True, type.FullName);
			Assert.That(typeof(MissionEvent).GetProperty(nameof(MissionEvent.TargetCell)).PropertyType, Is.EqualTo(typeof(CellXY?)));
			Assert.That(typeof(MissionEvent).GetProperty(nameof(MissionEvent.UnitCell)).PropertyType, Is.EqualTo(typeof(CellXY?)));
			foreach (var method in typeof(ICameoLogSink).GetMethods().Where(m => !m.IsSpecialName))
			{
				Assert.That(method.ReturnType, Is.EqualTo(typeof(void)), method.Name);
				foreach (var parameter in method.GetParameters())
					Assert.That(Allowed(parameter.ParameterType, new HashSet<Type>(), EventTypes), Is.True, method.Name + ":" + parameter.Name);
			}
		}

		[Test]
		public void WalkerRejectsMutableAndEngineTypes()
		{
			foreach (var type in new[] { typeof(Player), typeof(Actor), typeof(World), typeof(BotMissionRecord),
				typeof(List<int>), typeof(int[]), typeof(ValueTuple<int, int>), typeof(object) })
				Assert.That(Allowed(type, new HashSet<Type>(), EventTypes), Is.False, type.FullName);
		}

		sealed class Good { public int Value { get; init; } }
		sealed class BadField { public int Value = 1; }
		sealed class BadSetter { public int Value { get; set; } }
		sealed class BadPlayer { public Player Value { get; init; } }
		sealed class BadArray { public int[] Value { get; init; } }
		sealed class BadTuple { public (int, int) Value { get; init; } }

		[Test]
		public void WalkerDetectsMutantsInsideApprovedTypes()
		{
			var synthetic = new HashSet<Type> { typeof(Good), typeof(BadField), typeof(BadSetter),
				typeof(BadPlayer), typeof(BadArray), typeof(BadTuple) };
			Assert.That(Allowed(typeof(Good), new HashSet<Type>(), synthetic), Is.True);
			foreach (var type in synthetic.Where(t => t != typeof(Good)))
				Assert.That(Allowed(type, new HashSet<Type>(), synthetic), Is.False, type.Name);
		}
	}
}
