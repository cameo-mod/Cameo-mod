# audit_upgrade_coverage — roster-wide upgrade gaps (B4)

Coverage-tagged upgrades checked: **15** — uncovered unit slots: **55**


## Coverage by upgrade

| upgrade | faction | declared coverage | covered | uncovered actors |
|---|---|---|---|---|
| cabaldarkarmament | cabal | infantry | 7/10 | tsdefender, tsengineecabal, tshacker |
| cabalfirewallprotocol | cabal | roster_wide | 21/28 | tsdissolver, tsengineecabal, tsgtcnstcabal, tsharvcabal, tsmcvcabal, tsprobe, tsrepaircabal |
| cabalnetworkprotocols | cabal | roster_wide | 21/28 | tsdissolver, tsengineecabal, tsgtcnstcabal, tsharvcabal, tsmcvcabal, tsprobe, tsrepaircabal |
| up_advancedtiberiumrefinement | tsnod | vehicles | 10/11 | tsmcvnod |
| up_chemicalfuel | forgotten | vehicles | 18/19 | tsmcvmutant |
| up_genomemapping | forgotten | infantry | 13/13 | — |
| up_junkarmor | forgotten | vehicles | 18/19 | tsmcvmutant |
| up_mechanicalreliability | tsgdi | vehicles | 16/16 | — |
| up_modernfirecontrolsystems | tsgdi | roster_wide | 15/31 | tse1.gdi, tse2, tsenforcer, tshammerhead, tsjumpjet2, tskodk, tslpst, tsmedic, tsorca, tsorcab, tsprobe, tsrailcom, tsriott, tstrnsport, tszoneorca, tszonetrooper |
| up_mypet | forgotten | roster_wide | 31/39 | tsapachemutant, tscropplane, tsflocust, tsheli, tshind, tsmcvmutant, tsprobe, tstrnsportmutant |
| up_seretraining | tsgdi | infantry | 8/8 | — |
| up_tiberiumadaptability | forgotten | roster_wide | 37/39 | tsprobe, tstrnsportmutant |
| up_unity | forgotten | roster_wide | 37/39 | tsmcvmutant, tsprobe |
| up_willofkane | tsnod | infantry | 7/7 | — |
| upcabalfullassimilation | cabal | roster_wide | 21/28 | tsdissolver, tsengineecabal, tsgtcnstcabal, tsharvcabal, tsmcvcabal, tsprobe, tsrepaircabal |


_Note: 'covered' means the actor carries a GrantConditionOnPrerequisite hook for the upgrade. Upgrades applied globally through a shared decoration/rank template count as covered because the hook is inherited into the resolved actor._

