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

using NUnit.Framework;
using OpenRA.Mods.CA.Traits.BotModules.Squads;

namespace OpenRA.Mods.Cameo.Test
{
	[TestFixture]
	public class ProtectionEpisodeGuardTest
	{
		// AR-S residual 2 (BL_protection_episode_guard, 2026-10-04): the dominant residual
		// flap (~95% of tagged alternations) is the protection tick alternating its engage
		// AttackMove with the lure Move as the combat-predictor verdict flickers per eval.
		// Evals run once per AttackForceInterval, so each confirm tick is ~2-4s of game time.

		const int Enter = 2;
		const int Abort = 2;

		static ProtectionEpisode Run(ProtectionEpisode ep, params bool[] wantsLure)
		{
			foreach (var w in wantsLure)
				ep = ep.Eval(w, Enter, Abort);

			return ep;
		}

		[Test]
		public void FlickerNeverEnters()
		{
			// THE flap case: verdict alternating every eval. Classic re-issued
			// Move(rally) <-> AttackMove(target) each pass; the guard must stay out
			// of the lure episode entirely.
			var ep = Run(default(ProtectionEpisode), true, false, true, false, true, false, true, false);
			Assert.That(ep.Luring, Is.False, "T/F alternation must never start a lure episode");
		}

		[Test]
		public void SustainedLossEntersOnConfirm()
		{
			// A real losing fight: first losing eval keeps engaging, second starts the lure.
			var ep = default(ProtectionEpisode).Eval(true, Enter, Abort);
			Assert.That(ep.Luring, Is.False, "single losing eval must not start the fallback");
			ep = ep.Eval(true, Enter, Abort);
			Assert.That(ep.Luring, Is.True, "second consecutive losing eval starts the lure");
		}

		[Test]
		public void InFlightLureIgnoresSingleFlicker()
		{
			// Luring, then verdict flickers win/loss — one clear eval must not abort.
			var ep = Run(default(ProtectionEpisode), true, true); // luring
			ep = ep.Eval(false, Enter, Abort);
			Assert.That(ep.Luring, Is.True, "one clear eval is a flicker, not a resolved verdict");
			ep = ep.Eval(true, Enter, Abort);
			ep = ep.Eval(true, Enter, Abort);
			Assert.That(ep.Luring, Is.True, "a flicker inside a loss run keeps the lure");
		}

		[Test]
		public void ResolvedVerdictAbortsWithinOneExtraEval()
		{
			// The enemy is genuinely beatable now (two consecutive clear evals): the lure
			// aborts on the second — at most one extra eval of fallback vs classic.
			var ep = Run(default(ProtectionEpisode), true, true); // luring
			ep = ep.Eval(false, Enter, Abort);
			ep = ep.Eval(false, Enter, Abort);
			Assert.That(ep.Luring, Is.False, "sustained win verdict must re-engage");
		}

		[Test]
		public void EnemyAppearsEngagesImmediately()
		{
			// THE no-delay case: not in a lure, an eval that does not want to lure (a real,
			// winnable enemy) engages this eval — the guard confirms only the fallback.
			var ep = default(ProtectionEpisode);
			for (var i = 0; i < 6; i++)
				ep = ep.Eval(false, Enter, Abort);
			Assert.That(ep.Luring, Is.False);
		}

		[Test]
		public void ReenterAfterAbortStillConfirms()
		{
			// Aborted the lure; a fresh losing run must confirm again — an aborted
			// episode is not sticky.
			var ep = Run(default(ProtectionEpisode), true, true, false, false); // aborted
			ep = ep.Eval(true, Enter, Abort);
			Assert.That(ep.Luring, Is.False, "first loss of a new run must not re-enter instantly");
			ep = ep.Eval(true, Enter, Abort);
			Assert.That(ep.Luring, Is.True);
		}

		[Test]
		public void ConfirmOneMatchesPerEvalClassic()
		{
			// Degenerate config sanity: enter=1, abort=1 reproduces the unarmed per-eval
			// decision for every input sequence.
			var seq = new[] { true, true, false, true, false, false, true, true, false };
			var ep = default(ProtectionEpisode);
			foreach (var w in seq)
			{
				ep = ep.Eval(w, 1, 1);
				Assert.That(ep.Luring, Is.EqualTo(w));
			}
		}
	}
}
