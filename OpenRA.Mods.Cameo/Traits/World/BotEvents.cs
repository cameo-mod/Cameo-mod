#region Copyright & License Information
/*
 * Copyright (c) The OpenRA Developers and Contributors
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation, either version 3 of
 * the License, or (at your option) any later version. For more
 * information, see COPYING.
 */
#endregion

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using OpenRA.Primitives;
using OpenRA.Traits;

namespace OpenRA.Mods.Cameo.Traits
{
	// STAGGER_DESIGN_06 section 7.4: a recipient is a seat plus a module, resolved to an object at delivery.
	// Module = "<trait info type name>@<yaml instance name>"; an empty module means the bot itself.
	public readonly struct ModuleKey : IEquatable<ModuleKey>
	{
		public readonly string Seat;
		public readonly string Module;

		public ModuleKey(string seat, string module)
		{
			Seat = seat ?? "";
			Module = module ?? "";
		}

		public bool Equals(ModuleKey other) => Seat == other.Seat && Module == other.Module;
		public override bool Equals(object obj) => obj is ModuleKey k && Equals(k);
		public override int GetHashCode() => HashCode.Combine(Seat, Module);

		// The separator cannot appear in a player InternalName or a trait type name.
		public override string ToString() => Seat + "|" + Module;

		public static ModuleKey Parse(string text)
		{
			var i = text.IndexOf('|');
			return i < 0 ? new ModuleKey(text, "") : new ModuleKey(text[..i], text[(i + 1)..]);
		}
	}

	// A closed, sealed-by-convention hierarchy: every kind can be saved (7.3), there is no Action-carrying
	// kind, and the scheduler's Post accepts only a BotEvent. Test S1 enumerates the subclasses by reflection.
	public abstract class BotEvent
	{
		public long Seq;
		public int ArrivalTick;
		public ModuleKey Recipient;

		public abstract string Kind { get; }

		protected abstract void SaveFields(List<MiniYamlNode> into);
		protected abstract void LoadFields(MiniYaml data);

		public MiniYamlNode Save()
		{
			var fields = new List<MiniYamlNode>
			{
				new("Seq", Fmt(Seq)),
				new("ArrivalTick", Fmt(ArrivalTick)),
				new("Recipient", Recipient.ToString()),
			};
			SaveFields(fields);
			return new MiniYamlNode("Event", new MiniYaml(Kind, fields));
		}

		// Returns null for an unknown kind: the caller refuses the load (7.5).
		public static BotEvent Load(MiniYaml data)
		{
			BotEvent ev = data.Value switch
			{
				AttackEvent.KindName => new AttackEvent(),
				AppliedDamageEvent.AppliedKindName => new AppliedDamageEvent(),
				LeaseLostEvent.KindName => new LeaseLostEvent(),
				OrderDispositionEvent.KindName => new OrderDispositionEvent(),
				QueueTransitionEvent.KindName => new QueueTransitionEvent(),
				BeaconEvent.KindName => new BeaconEvent(),
				_ => null,
			};

			if (ev == null)
				return null;

			ev.Seq = long.Parse(Get(data, "Seq"), CultureInfo.InvariantCulture);
			ev.ArrivalTick = int.Parse(Get(data, "ArrivalTick"), CultureInfo.InvariantCulture);
			ev.Recipient = ModuleKey.Parse(Get(data, "Recipient"));
			ev.LoadFields(data);
			return ev;
		}

		// Actor references that a load can no longer resolve are D-3 case 2 (the host counts them).
		public virtual IEnumerable<uint> ActorIds => Enumerable.Empty<uint>();

		protected static string Fmt(long v) => v.ToString(CultureInfo.InvariantCulture);
		protected static string Get(MiniYaml d, string key) => d.Nodes.First(n => n.Key == key).Value.Value;
		protected static int GetInt(MiniYaml d, string key) => int.Parse(Get(d, key), CultureInfo.InvariantCulture);
		protected static uint GetUInt(MiniYaml d, string key) => uint.Parse(Get(d, key), CultureInfo.InvariantCulture);
		protected static long GetLong(MiniYaml d, string key) => long.Parse(Get(d, key), CultureInfo.InvariantCulture);
	}

	// ModularBot.Damaged (:392-415). The in-memory AttackInfo is kept for a continuous game so delivery is
	// identical to today's synchronous call; the integer fields are what a save writes and a load rebuilds.
	public class AttackEvent : BotEvent
	{
		public const string KindName = "AttackEvent";

		public uint DamagedActorId;
		public uint AttackerActorId;
		public int DamageValue;
		public string[] DamageTypes = [];
		public DamageState DamageState;
		public DamageState PreviousDamageState;
		public string ProjectileType = "";

		// Not saved: the live references of the continuous game.
		public Actor Self;
		public AttackInfo Info;

		public override string Kind => KindName;
		public override IEnumerable<uint> ActorIds
		{
			get
			{
				Capture();
				return AttackerActorId == 0 ? new[] { DamagedActorId } : new[] { DamagedActorId, AttackerActorId };
			}
		}

		// Cheap on purpose: damage events are frequent. The integer fields are captured only when a save needs them.
		public static AttackEvent From(Actor self, AttackInfo e) => new() { Self = self, Info = e };

		void Capture()
		{
			if (Info == null || Self == null)
				return;

			DamagedActorId = Self.ActorID;
			AttackerActorId = Info.Attacker?.ActorID ?? 0;
			DamageValue = Info.Damage?.Value ?? 0;
			DamageTypes = Info.Damage == null ? [] : Info.Damage.DamageTypes.OrderBy(x => x, StringComparer.Ordinal).ToArray();
			DamageState = Info.DamageState;
			PreviousDamageState = Info.PreviousDamageState;
			ProjectileType = Info.ProjectileType ?? Info.Damage?.ProjectileType ?? "";
		}

		// Rebuild the live objects after a load. False when an actor id no longer resolves (D-3 case 2).
		public bool Resolve(World world)
		{
			Self = world.GetActorById(DamagedActorId);
			if (Self == null)
				return false;

			Actor attacker = null;
			if (AttackerActorId != 0)
			{
				attacker = world.GetActorById(AttackerActorId);
				if (attacker == null)
					return false;
			}

			Info = new AttackInfo
			{
				Attacker = attacker,
				Damage = new Damage(DamageValue, new BitSet<DamageType>(DamageTypes), ProjectileType.Length == 0 ? null : ProjectileType),
				DamageState = DamageState,
				PreviousDamageState = PreviousDamageState,
				ProjectileType = ProjectileType.Length == 0 ? null : ProjectileType,
			};
			return true;
		}

		protected override void SaveFields(List<MiniYamlNode> into)
		{
			Capture();
			into.Add(new("DamagedActorId", Fmt(DamagedActorId)));
			into.Add(new("AttackerActorId", Fmt(AttackerActorId)));
			into.Add(new("Damage", Fmt(DamageValue)));
			into.Add(new("DamageTypes", string.Join(",", DamageTypes)));
			into.Add(new("DamageState", Fmt((int)DamageState)));
			into.Add(new("PreviousDamageState", Fmt((int)PreviousDamageState)));
			into.Add(new("ProjectileType", ProjectileType));
		}

		protected override void LoadFields(MiniYaml data)
		{
			DamagedActorId = GetUInt(data, "DamagedActorId");
			AttackerActorId = GetUInt(data, "AttackerActorId");
			DamageValue = GetInt(data, "Damage");
			var types = Get(data, "DamageTypes");
			DamageTypes = types.Length == 0 ? [] : types.Split(',');
			DamageState = (DamageState)GetInt(data, "DamageState");
			PreviousDamageState = (DamageState)GetInt(data, "PreviousDamageState");
			ProjectileType = Get(data, "ProjectileType");
		}
	}

	// Engine INotifyAppliedDamage into CombatAnalysis and EngagementLog: the same fields plus the recipient module.
	public sealed class AppliedDamageEvent : AttackEvent
	{
		public const string AppliedKindName = "AppliedDamageEvent";

		public override string Kind => AppliedKindName;
	}

	public enum LeaseLostReason { Preempted, Released, Expired, UnitLost }

	public sealed class LeaseLostEvent : BotEvent
	{
		public const string KindName = "LeaseLostEvent";

		public uint UnitActorId;
		public ModuleKey PreviousHolder;
		public LeaseLostReason Reason;

		public override string Kind => KindName;
		public override IEnumerable<uint> ActorIds => [UnitActorId];

		protected override void SaveFields(List<MiniYamlNode> into)
		{
			into.Add(new("UnitActorId", Fmt(UnitActorId)));
			into.Add(new("PreviousHolder", PreviousHolder.ToString()));
			into.Add(new("Reason", Fmt((int)Reason)));
		}

		protected override void LoadFields(MiniYaml data)
		{
			UnitActorId = GetUInt(data, "UnitActorId");
			PreviousHolder = ModuleKey.Parse(Get(data, "PreviousHolder"));
			Reason = (LeaseLostReason)GetInt(data, "Reason");
		}
	}

	// 7.2: a queued disposition keeps the in-memory Order reference in a continuous game; after a load the
	// descriptor and serial are delivered and an unmatched event is a counted no-op (11.4).
	public sealed class OrderDispositionEvent : BotEvent
	{
		public const string KindName = "OrderDispositionEvent";

		public long Serial;
		public string OrderString = "";
		public uint SubjectActorId;
		public uint TargetActorId;
		public string TargetString = "";
		public uint ExtraData;
		public ModuleKey Issuer;
		public int IssueTick;
		public int Outcome;

		// Not saved.
		public Order Order;

		public override string Kind => KindName;
		public override IEnumerable<uint> ActorIds => SubjectActorId == 0 ? Array.Empty<uint>() : new[] { SubjectActorId };

		protected override void SaveFields(List<MiniYamlNode> into)
		{
			into.Add(new("Serial", Fmt(Serial)));
			into.Add(new("OrderString", OrderString));
			into.Add(new("SubjectActorId", Fmt(SubjectActorId)));
			into.Add(new("TargetActorId", Fmt(TargetActorId)));
			into.Add(new("TargetString", TargetString));
			into.Add(new("ExtraData", Fmt(ExtraData)));
			into.Add(new("Issuer", Issuer.ToString()));
			into.Add(new("IssueTick", Fmt(IssueTick)));
			into.Add(new("Outcome", Fmt(Outcome)));
		}

		protected override void LoadFields(MiniYaml data)
		{
			Serial = GetLong(data, "Serial");
			OrderString = Get(data, "OrderString");
			SubjectActorId = GetUInt(data, "SubjectActorId");
			TargetActorId = GetUInt(data, "TargetActorId");
			TargetString = Get(data, "TargetString");
			ExtraData = GetUInt(data, "ExtraData");
			Issuer = ModuleKey.Parse(Get(data, "Issuer"));
			IssueTick = GetInt(data, "IssueTick");
			Outcome = GetInt(data, "Outcome");
		}
	}

	public enum QueueTransition { Started, Done, Cancelled, Placed, Refunded }

	public sealed class QueueTransitionEvent : BotEvent
	{
		public const string KindName = "QueueTransitionEvent";

		public uint QueueActorId;
		public string QueueType = "";
		public string ItemName = "";
		public QueueTransition Transition;

		public override string Kind => KindName;
		public override IEnumerable<uint> ActorIds => [QueueActorId];

		protected override void SaveFields(List<MiniYamlNode> into)
		{
			into.Add(new("QueueActorId", Fmt(QueueActorId)));
			into.Add(new("QueueType", QueueType));
			into.Add(new("ItemName", ItemName));
			into.Add(new("Transition", Fmt((int)Transition)));
		}

		protected override void LoadFields(MiniYaml data)
		{
			QueueActorId = GetUInt(data, "QueueActorId");
			QueueType = Get(data, "QueueType");
			ItemName = Get(data, "ItemName");
			Transition = (QueueTransition)GetInt(data, "Transition");
		}
	}

	public sealed class BeaconEvent : BotEvent
	{
		public const string KindName = "BeaconEvent";

		public string SourceSeat = "";
		public int X;
		public int Y;
		public int Z;
		public string BeaconKind = "";

		public override string Kind => KindName;

		protected override void SaveFields(List<MiniYamlNode> into)
		{
			into.Add(new("SourceSeat", SourceSeat));
			into.Add(new("X", Fmt(X)));
			into.Add(new("Y", Fmt(Y)));
			into.Add(new("Z", Fmt(Z)));
			into.Add(new("BeaconKind", BeaconKind));
		}

		protected override void LoadFields(MiniYaml data)
		{
			SourceSeat = Get(data, "SourceSeat");
			X = GetInt(data, "X");
			Y = GetInt(data, "Y");
			Z = GetInt(data, "Z");
			BeaconKind = Get(data, "BeaconKind");
		}
	}
}

namespace OpenRA.Mods.Cameo.Traits
{
	// A module that receives queued events at its bot's visit. Recipient keys resolve to receivers at delivery
	// (7.4); a receiver never holds a reference into the saved form.
	public interface IBotEventReceiver
	{
		void ReceiveBotEvent(BotEvent ev);
	}
}
