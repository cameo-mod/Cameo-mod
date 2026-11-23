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
using OpenRA.Mods.CA.Traits;
using OpenRA.Primitives;
using OpenRA.Traits;

namespace OpenRA.Mods.Cameo.Traits
{
	// STAGGER_DESIGN_06 section 7.4: a recipient is a seat plus a module, resolved to an object at delivery.
	// Module = "<trait info type name>@<yaml instance name>"; an empty module means the bot itself.
	// HOTFIX-RR-01: default(ModuleKey) is the unset Recipient of every event that addresses the bot itself (AttackEvent,
	// LeaseLostEvent, OrderDispositionEvent are queued without one). Its fields are null, so both accessors normalize
	// null to "": a default key IS the empty "bot itself" key, everywhere it is read, compared, hashed or saved.
	public readonly struct ModuleKey : IEquatable<ModuleKey>
	{
		readonly string seat;
		readonly string module;

		public string Seat => seat ?? "";
		public string Module => module ?? "";

		public ModuleKey(string seat, string module)
		{
			this.seat = seat ?? "";
			this.module = module ?? "";
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
				DamageDealtEvent.KindName => new DamageDealtEvent(),
				EngagementHitEvent.KindName => new EngagementHitEvent(),
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

	// BotUnitLeaseRegistry.Preempt (the only source): the holder named PreviousHolder (a trait type name, the lease-owner
	// convention) lost UnitActorId to NewOwner. Recipient is empty: the bot re-runs the registry's holder match at delivery.
	public sealed class LeaseLostEvent : BotEvent
	{
		public const string KindName = "LeaseLostEvent";

		public uint UnitActorId;
		public string PreviousHolder = "";
		public string NewOwner = "";
		public int Purpose;

		// Not saved: the live reference of the continuous game; a load resolves the id.
		public Actor Unit;

		public override string Kind => KindName;
		public override IEnumerable<uint> ActorIds => [UnitActorId];

		public static LeaseLostEvent From(Actor unit, string previousHolder, string newOwner, int purpose) =>
			new() { Unit = unit, UnitActorId = unit.ActorID, PreviousHolder = previousHolder ?? "", NewOwner = newOwner ?? "", Purpose = purpose };

		protected override void SaveFields(List<MiniYamlNode> into)
		{
			into.Add(new("UnitActorId", Fmt(UnitActorId)));
			into.Add(new("PreviousHolder", PreviousHolder));
			into.Add(new("NewOwner", NewOwner));
			into.Add(new("Purpose", Fmt(Purpose)));
		}

		protected override void LoadFields(MiniYaml data)
		{
			UnitActorId = GetUInt(data, "UnitActorId");
			PreviousHolder = Get(data, "PreviousHolder");
			NewOwner = Get(data, "NewOwner");
			Purpose = GetInt(data, "Purpose");
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
		public bool Terminal;

		// Not saved.
		public Order Order;

		public override string Kind => KindName;
		public override IEnumerable<uint> ActorIds => SubjectActorId == 0 ? Array.Empty<uint>() : new[] { SubjectActorId };

		public static OrderDispositionEvent From(Order order, BotOrderOutcome outcome, bool terminal, long serial) => new()
		{
			Order = order,
			Serial = serial,
			OrderString = order.OrderString ?? "",
			SubjectActorId = order.Subject?.ActorID ?? 0,
			Outcome = (int)outcome,
			Terminal = terminal,
		};

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
			into.Add(new("Terminal", Terminal ? "true" : "false"));
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
			Terminal = Get(data, "Terminal") == "true";
		}
	}

	// INotifyAppliedDamage into CombatAnalysis (Opus condition): every fact the module reads is captured at event time, so a
	// deferred delivery scores exactly what the synchronous call would have. No live actor reference, so nothing to resolve
	// after a load and nothing can go stale.
	public sealed class DamageDealtEvent : BotEvent
	{
		public const string KindName = "DamageDealtEvent";

		public int EventTick;
		public uint DamagedActorId;
		public string VictimOwner = "";
		public string VictimType = "";
		public uint AttackerActorId;
		public string AttackerOwner = "";
		public string AttackerType = "";
		public int Damage;
		public int LocationX;
		public int LocationY;
		public bool Dead;

		public override string Kind => KindName;

		public static DamageDealtEvent From(ModuleKey recipient, int tick, Actor damaged, AttackInfo e)
		{
			var attacker = e.Attacker;
			return new DamageDealtEvent
			{
				Recipient = recipient,
				EventTick = tick,
				DamagedActorId = damaged.ActorID,
				VictimOwner = damaged.Owner?.InternalName ?? "",
				VictimType = damaged.Info.Name,
				AttackerActorId = attacker?.ActorID ?? 0,
				AttackerOwner = attacker?.Owner?.InternalName ?? "",
				AttackerType = attacker?.Info.Name ?? "",
				Damage = e.Damage?.Value ?? 0,
				LocationX = damaged.Location.X,
				LocationY = damaged.Location.Y,
				Dead = e.DamageState == DamageState.Dead,
			};
		}

		protected override void SaveFields(List<MiniYamlNode> into)
		{
			into.Add(new("EventTick", Fmt(EventTick)));
			into.Add(new("DamagedActorId", Fmt(DamagedActorId)));
			into.Add(new("VictimOwner", VictimOwner));
			into.Add(new("VictimType", VictimType));
			into.Add(new("AttackerActorId", Fmt(AttackerActorId)));
			into.Add(new("AttackerOwner", AttackerOwner));
			into.Add(new("AttackerType", AttackerType));
			into.Add(new("Damage", Fmt(Damage)));
			into.Add(new("LocationX", Fmt(LocationX)));
			into.Add(new("LocationY", Fmt(LocationY)));
			into.Add(new("Dead", Dead ? "true" : "false"));
		}

		protected override void LoadFields(MiniYaml data)
		{
			EventTick = GetInt(data, "EventTick");
			DamagedActorId = GetUInt(data, "DamagedActorId");
			VictimOwner = Get(data, "VictimOwner");
			VictimType = Get(data, "VictimType");
			AttackerActorId = GetUInt(data, "AttackerActorId");
			AttackerOwner = Get(data, "AttackerOwner");
			AttackerType = Get(data, "AttackerType");
			Damage = GetInt(data, "Damage");
			LocationX = GetInt(data, "LocationX");
			LocationY = GetInt(data, "LocationY");
			Dead = Get(data, "Dead") == "true";
		}
	}

	// EngagementLog hurt/dealt hits (its RunningTotalMilli feeds InMatchAdapt, so deferral must not change what it sees).
	// Everything the module's Apply reads is captured at event time; type names resolve against the rules at delivery.
	public sealed class EngagementHitEvent : BotEvent
	{
		public const string KindName = "EngagementHitEvent";

		public bool Hurt;
		public int EventTick;
		public uint SubjectId;
		public string SubjectType = "";
		public int X;
		public int Y;
		public bool Dead;
		public int Damage;
		public int Hp;
		public int MaxHp = 1;
		public uint AttackerId;
		public string AttackerType = "";
		public bool AttackerAlive;
		public bool AttackerInWorld;
		public int AttackerX;
		public int AttackerY;

		public override string Kind => KindName;

		static string B(bool v) => v ? "true" : "false";

		protected override void SaveFields(List<MiniYamlNode> into)
		{
			into.Add(new("Hurt", B(Hurt)));
			into.Add(new("EventTick", Fmt(EventTick)));
			into.Add(new("SubjectId", Fmt(SubjectId)));
			into.Add(new("SubjectType", SubjectType));
			into.Add(new("X", Fmt(X)));
			into.Add(new("Y", Fmt(Y)));
			into.Add(new("Dead", B(Dead)));
			into.Add(new("Damage", Fmt(Damage)));
			into.Add(new("Hp", Fmt(Hp)));
			into.Add(new("MaxHp", Fmt(MaxHp)));
			into.Add(new("AttackerId", Fmt(AttackerId)));
			into.Add(new("AttackerType", AttackerType));
			into.Add(new("AttackerAlive", B(AttackerAlive)));
			into.Add(new("AttackerInWorld", B(AttackerInWorld)));
			into.Add(new("AttackerX", Fmt(AttackerX)));
			into.Add(new("AttackerY", Fmt(AttackerY)));
		}

		protected override void LoadFields(MiniYaml data)
		{
			Hurt = Get(data, "Hurt") == "true";
			EventTick = GetInt(data, "EventTick");
			SubjectId = GetUInt(data, "SubjectId");
			SubjectType = Get(data, "SubjectType");
			X = GetInt(data, "X");
			Y = GetInt(data, "Y");
			Dead = Get(data, "Dead") == "true";
			Damage = GetInt(data, "Damage");
			Hp = GetInt(data, "Hp");
			MaxHp = GetInt(data, "MaxHp");
			AttackerId = GetUInt(data, "AttackerId");
			AttackerType = Get(data, "AttackerType");
			AttackerAlive = Get(data, "AttackerAlive") == "true";
			AttackerInWorld = Get(data, "AttackerInWorld") == "true";
			AttackerX = GetInt(data, "AttackerX");
			AttackerY = GetInt(data, "AttackerY");
		}
	}

	public enum QueueTransition {Started, Done, Cancelled, Placed, Refunded }

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

	// A module that also implements IBotRespondToAttack and reads live state there (liveness, health, location) takes its own
	// event-time snapshot instead of seeing the queued attack at the slot. For a gated bot ModularBot posts SnapshotAttack's
	// event to the module and skips the module in the queued attack delivery. Null = nothing to record.
	public interface IBotAttackSnapshotter
	{
		BotEvent SnapshotAttack(Actor self, AttackInfo e);
	}
}
