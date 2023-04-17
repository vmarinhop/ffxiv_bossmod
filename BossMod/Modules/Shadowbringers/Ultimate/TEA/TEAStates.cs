﻿using System.Linq;

namespace BossMod.Shadowbringers.Ultimate.TEA
{
    class TEAStates : StateMachineBuilder
    {
        private TEA _module;

        public TEAStates(TEA module) : base(module)
        {
            _module = module;
            SimplePhase(0, Phase1LivingLiquid, "P1: Living Liquid")
                .ActivateOnEnter<P1HandOfPain>()
                .Raw.Update = () => Module.PrimaryActor.IsDestroyed || Module.PrimaryActor.IsDead; // phase 1 ends either with wipe (everything destroyed) or success (boss dies, then is destroyed few seconds into next phase)
            SimplePhase(1, Phase2BruteJusticeCruiseChaser, "P2: BJ+CC")
                .Raw.Update = () =>
                {
                    var bj = _module.BruteJustice();
                    var cc = _module.CruiseChaser();
                    if (bj == null || cc == null)
                        return true; // shouldn't happen - BJ & CC should exist from the start
                    if (Module.PrimaryActor.IsDestroyed && bj.IsDestroyed && cc.IsDestroyed)
                        return true; // wipe in p1/p2 => all actors are destroyed; BJ/CC are not destroyed otherwise
                    // BJ/CC start untargetable, then both become targetable after intermission and stay targetable until the end
                    // when either reaches 1hp, it becomes untargetable, and the remaining one starts casting enrage
                    // some time after becoming untargetable, BJ/CC get healed to full - so the pass condition is: both untargetable and at least one at non-full hp (this prevents triggering during intermission)
                    return !bj.IsTargetable && !cc.IsTargetable && (bj.HP.Cur < bj.HP.Max || cc.HP.Cur < cc.HP.Max);
                };
            SimplePhase(2, Phase3AlexanderPrime, "P3: Alex Prime")
                .Raw.Update = () => Module.PrimaryActor.IsDestroyed && (_module.BruteJustice()?.IsDestroyed ?? true) && (_module.CruiseChaser()?.IsDestroyed ?? true); // TODO: improve...
        }

        private void Phase1LivingLiquid(uint id)
        {
            P1FluidSwing(id, 10.2f);
            P1Cascade(id + 0x10000, 4.1f);
            P1HandsProteansDollsCleaves(id + 0x20000, 11.1f);
            P1ProteansBoss(id + 0x30000, 10.2f);
            P1SplashDrainageCascade(id + 0x40000, 6.1f);
            P1Throttle(id + 0x50000, 4.6f);
            P1ProteansBoth(id + 0x60000, 7.7f);
            P1PainSplashCleaves(id + 0x70000, 5.1f);
            ActorCast(id + 0x80000, _module.BossP1, AID.Enrage, 3.1f, 4, true, "Enrage");
        }

        private void P1FluidSwing(uint id, float delay)
        {
            ComponentCondition<P1FluidSwing>(id, delay, comp => comp.NumCasts > 0, "Cleave")
                .ActivateOnEnter<P1FluidSwing>()
                .DeactivateOnExit<P1FluidSwing>()
                .SetHint(StateMachine.StateHint.Tankbuster);
        }

        // keeps cascade component active
        private void P1Cascade(uint id, float delay)
        {
            ActorCast(id, _module.BossP1, AID.Cascade, delay, 4, true, "Raidwide")
                .ActivateOnEnter<P1Cascade>()
                .SetHint(StateMachine.StateHint.Raidwide);
        }

        private State P1HandOfPain(uint id, float delay, int seq)
        {
            return ComponentCondition<P1HandOfPain>(id, delay, comp => comp.NumCasts >= seq, $"HP check {seq}");
        }

        private void P1HandsProteansDollsCleaves(uint id, float delay)
        {
            Condition(id, delay, () => (_module.LiquidHand()?.ModelState.ModelState ?? 0) != 0, "Hand of parting/prayer bait");
            ComponentCondition<P1ProteanWaveTornadoVisCast>(id + 1, 3.1f, comp => comp.Casters.Count > 0)
                .ActivateOnEnter<P1HandOfPartingPrayer>()
                .ActivateOnEnter<P1ProteanWaveTornadoVisBait>()
                .ActivateOnEnter<P1ProteanWaveTornadoVisCast>()
                .DeactivateOnExit<P1ProteanWaveTornadoVisBait>();
            ComponentCondition<P1JagdDolls>(id + 2, 1, comp => comp.Active)
                .ActivateOnEnter<P1JagdDolls>();
            ComponentCondition<P1HandOfPartingPrayer>(id + 3, 1, comp => comp.Resolved, "Resolve")
                .DeactivateOnExit<P1HandOfPartingPrayer>();
            ComponentCondition<P1ProteanWaveTornadoVisCast>(id + 0x10, 1, comp => comp.Casters.Count == 0)
                .ActivateOnEnter<P1FluidStrike>()
                .ActivateOnEnter<P1FluidSwing>()
                .DeactivateOnExit<P1ProteanWaveTornadoVisCast>();
            ComponentCondition<P1FluidSwing>(id + 0x20, 1.1f, comp => comp.NumCasts > 0, "Cleaves")
                .ActivateOnEnter<P1ProteanWaveTornadoInvis>()
                .DeactivateOnExit<P1FluidStrike>()
                .DeactivateOnExit<P1FluidSwing>()
                .SetHint(StateMachine.StateHint.Tankbuster);
            ComponentCondition<P1ProteanWaveTornadoInvis>(id + 0x30, 1, comp => comp.NumCasts > 0)
                .DeactivateOnExit<P1ProteanWaveTornadoInvis>();
            ComponentCondition<P1JagdDolls>(id + 0x100, 1, comp => comp.NumExhausts > 0, "Exhaust 1");
            // +0.1s: hand of pain start
            // +2.0s: pressurize
            // +2.6s: embolus spawn
            P1HandOfPain(id + 0x200, 3.1f, 1);
            ComponentCondition<P1JagdDolls>(id + 0x300, 7.5f, comp => comp.NumExhausts > 1, "Exhaust 2", 0.1f);
            ComponentCondition<P1FluidSwing>(id + 0x400, 6.4f, comp => comp.NumCasts > 0, "Cleaves")
                .ActivateOnEnter<P1FluidStrike>()
                .ActivateOnEnter<P1FluidSwing>()
                .DeactivateOnExit<P1FluidStrike>()
                .DeactivateOnExit<P1FluidSwing>()
                .SetHint(StateMachine.StateHint.Tankbuster);
        }

        private void P1ProteansBoss(uint id, float delay)
        {
            ActorCast(id + 1, _module.BossP1, AID.ProteanWaveLiquidVisBoss, delay, 3, true, "Protean baited")
                .ActivateOnEnter<P1ProteanWaveLiquidVisBoss>()
                .ActivateOnEnter<P1ProteanWaveLiquidVisHelper>()
                .DeactivateOnExit<P1ProteanWaveLiquidVisBoss>()
                .DeactivateOnExit<P1ProteanWaveLiquidVisHelper>();
            P1HandOfPain(id + 0x10, 0.2f, 2)
                .ActivateOnEnter<P1ProteanWaveLiquidInvisFixed>()
                .ActivateOnEnter<P1ProteanWaveLiquidInvisBaited>()
                .ActivateOnEnter<P1Sluice>();
            ComponentCondition<P1ProteanWaveLiquidInvisFixed>(id + 0x20, 1.9f, comp => comp.NumCasts > 0, "Protean 1");
            ComponentCondition<P1ProteanWaveLiquidInvisFixed>(id + 0x30, 3.0f, comp => comp.NumCasts > 1, "Protean 2")
                .DeactivateOnExit<P1ProteanWaveLiquidInvisFixed>()
                .DeactivateOnExit<P1ProteanWaveLiquidInvisBaited>()
                .DeactivateOnExit<P1Sluice>();
        }

        private void P1SplashDrainageCascade(uint id, float delay)
        {
            ComponentCondition<P1Splash>(id, delay, comp => comp.NumCasts > 0, "Splash start")
                .ActivateOnEnter<P1Splash>()
                .ActivateOnEnter<P1Drainage>()
                .SetHint(StateMachine.StateHint.Raidwide);
            ComponentCondition<P1Splash>(id + 1, 1.1f, comp => comp.NumCasts > 1).SetHint(StateMachine.StateHint.Raidwide);
            ComponentCondition<P1Splash>(id + 2, 1.1f, comp => comp.NumCasts > 2).SetHint(StateMachine.StateHint.Raidwide);
            ComponentCondition<P1Splash>(id + 3, 1.1f, comp => comp.NumCasts > 3).SetHint(StateMachine.StateHint.Raidwide);
            ComponentCondition<P1Splash>(id + 4, 1.1f, comp => comp.NumCasts > 4).SetHint(StateMachine.StateHint.Raidwide);
            ComponentCondition<P1Splash>(id + 5, 1.1f, comp => comp.NumCasts > 5) // drainage resolves almost at the same time
                .DeactivateOnExit<P1Splash>()
                .DeactivateOnExit<P1Drainage>()
                .SetHint(StateMachine.StateHint.Raidwide);
            ActorCastStart(id + 0x100, _module.BossP1, AID.Cascade, 1.1f, true);
            P1HandOfPain(id + 0x101, 1.3f, 3);
            ActorCastEnd(id + 0x102, _module.BossP1, 2.7f, true, "Raidwide")
                .SetHint(StateMachine.StateHint.Raidwide);
        }

        private void P1Throttle(uint id, float delay)
        {
            ComponentCondition<P1Throttle>(id, delay, comp => comp.Applied, "Debuffs")
                .ActivateOnEnter<P1Throttle>()
                .DeactivateOnExit<P1Throttle>();
        }

        private void P1ProteansBoth(uint id, float delay)
        {
            ActorCast(id, _module.BossP1, AID.ProteanWaveLiquidVisBoss, delay, 3, true, "Protean baited")
                .ActivateOnEnter<P1ProteanWaveLiquidVisBoss>()
                .ActivateOnEnter<P1ProteanWaveLiquidVisHelper>()
                .DeactivateOnExit<P1ProteanWaveLiquidVisBoss>()
                .DeactivateOnExit<P1ProteanWaveLiquidVisHelper>();
            P1HandOfPain(id + 0x10, 2, 4)
                .ActivateOnEnter<P1ProteanWaveLiquidInvisFixed>()
                .ActivateOnEnter<P1ProteanWaveLiquidInvisBaited>()
                .ActivateOnEnter<P1Sluice>()
                .ActivateOnEnter<P1ProteanWaveTornadoVisBait>()
                .ActivateOnEnter<P1ProteanWaveTornadoVisCast>();
            ComponentCondition<P1ProteanWaveLiquidInvisFixed>(id + 0x11, 0.1f, comp => comp.NumCasts > 0, "Protean 1");
            ComponentCondition<P1ProteanWaveTornadoVisCast>(id + 0x12, 0.9f, comp => comp.Casters.Count > 0)
                .DeactivateOnExit<P1ProteanWaveTornadoVisBait>();
            ComponentCondition<P1ProteanWaveLiquidInvisFixed>(id + 0x20, 2.1f, comp => comp.NumCasts > 1, "Protean 2")
                .DeactivateOnExit<P1ProteanWaveLiquidInvisFixed>()
                .DeactivateOnExit<P1ProteanWaveLiquidInvisBaited>()
                .DeactivateOnExit<P1Sluice>();
            ComponentCondition<P1ProteanWaveTornadoVisCast>(id + 0x21, 0.8f, comp => comp.Casters.Count == 0)
                .DeactivateOnExit<P1ProteanWaveTornadoVisCast>();
            ComponentCondition<P1ProteanWaveTornadoInvis>(id + 0x30, 2.1f, comp => comp.NumCasts > 0)
                .ActivateOnEnter<P1ProteanWaveTornadoInvis>()
                .DeactivateOnExit<P1ProteanWaveTornadoInvis>();
            // +3.9s: pressurize
            // +4.5s: embolus spawn
            Condition(id + 0x40, 3.9f, () => (_module.LiquidHand()?.ModelState.ModelState ?? 0) != 0, "Hand of parting/prayer bait");
            ComponentCondition<P1HandOfPartingPrayer>(id + 0x41, 5.1f, comp => comp.Resolved, "Resolve")
                .ActivateOnEnter<P1HandOfPartingPrayer>()
                .DeactivateOnExit<P1HandOfPartingPrayer>();
        }

        private void P1PainSplashCleaves(uint id, float delay)
        {
            P1HandOfPain(id + 1, delay, 5)
                .DeactivateOnExit<P1HandOfPain>();
            ComponentCondition<P1Splash>(id + 0x10, 0.3f, comp => comp.NumCasts > 0, "Splash start")
                .ActivateOnEnter<P1Splash>()
                .ActivateOnEnter<P1FluidSwing>()
                .SetHint(StateMachine.StateHint.Raidwide);
            ComponentCondition<P1Splash>(id + 0x11, 1.1f, comp => comp.NumCasts > 1).SetHint(StateMachine.StateHint.Raidwide);
            ComponentCondition<P1Splash>(id + 0x12, 1.1f, comp => comp.NumCasts > 2).SetHint(StateMachine.StateHint.Raidwide);
            ComponentCondition<P1Splash>(id + 0x13, 1.1f, comp => comp.NumCasts > 3).SetHint(StateMachine.StateHint.Raidwide)
                .DeactivateOnExit<P1Splash>()
                .SetHint(StateMachine.StateHint.Raidwide);
            ComponentCondition<P1FluidSwing>(id + 0x20, 1.2f, comp => comp.NumCasts > 0, "Cleave")
                .DeactivateOnExit<P1FluidSwing>()
                .SetHint(StateMachine.StateHint.Tankbuster);
        }

        private void Phase2BruteJusticeCruiseChaser(uint id)
        {
            P2Intermission(id);
            P2WhirlwindDebuffs(id + 0x10000, 5.2f);
            P2ChakramOpticalSightPhoton(id + 0x20000, 6);
            P2SpinCrusherCompressedWaterLightning(id + 0x30000, 6.9f);
            P2MissileCommand(id + 0x40000, 4.3f);
            P2VerdictGavel(id + 0x50000, 1.4f);
            P2PhotonDoubleRocketPunch(id + 0x60000, 5.5f);
            P2SuperJumpApocalypticRay(id + 0x70000, 3.2f);
            P2Whirlwind(id + 0x80000, 1.4f);
            // TODO: second whirlwind > enrage
            SimpleState(id + 0xFF0000, 100, "???");
        }

        private void P2Intermission(uint id)
        {
            Timeout(id, 0)
                .SetHint(StateMachine.StateHint.DowntimeStart);
            ComponentCondition<P2IntermissionHawkBlaster>(id + 0x10, 5.2f, comp => comp.NumCasts > 0, "First aoe")
                .ActivateOnEnter<P2IntermissionOrder>()
                .ActivateOnEnter<P2IntermissionHawkBlaster>();
            ComponentCondition<P2IntermissionKnockbacks>(id + 0x20, 7.4f, comp => comp.NumCasts >= 1, "Hit 1")
                .ActivateOnEnter<P2IntermissionKnockbacks>();
            ComponentCondition<P2IntermissionKnockbacks>(id + 0x30, 4.6f, comp => comp.NumCasts >= 3, "Hit 3");
            ComponentCondition<P2IntermissionKnockbacks>(id + 0x40, 4.6f, comp => comp.NumCasts >= 5, "Hit 5");
            ComponentCondition<P2IntermissionKnockbacks>(id + 0x50, 4.6f, comp => comp.NumCasts >= 7, "Hit 7");
            ComponentCondition<P2JKick>(id + 0x100, 4.6f, comp => comp.NumCasts > 0)
                .ActivateOnEnter<P2JKick>()
                .DeactivateOnExit<P2IntermissionOrder>()
                .DeactivateOnExit<P2IntermissionHawkBlaster>()
                .DeactivateOnExit<P2IntermissionKnockbacks>()
                .DeactivateOnExit<P2JKick>()
                .SetHint(StateMachine.StateHint.Raidwide);
            ActorTargetable(id + 0x101, _module.BruteJustice, true, 3, "Intermission end")
                .SetHint(StateMachine.StateHint.DowntimeEnd);
        }

        // keeps nisi component active
        private void P2WhirlwindDebuffs(uint id, float delay)
        {
            ActorCastStart(id, _module.CruiseChaser, AID.Whirlwind, delay);
            ActorCastStart(id + 1, _module.BruteJustice, AID.JudgmentNisi, 3);
            ActorCastEnd(id + 2, _module.CruiseChaser, 1, false, "Raidwide")
                .SetHint(StateMachine.StateHint.Raidwide);
            ActorCastEnd(id + 3, _module.BruteJustice, 3, false, "Nisi")
                .ActivateOnEnter<P2Nisi>(); // debuffs are applied ~0.8s after cast end
            ActorCast(id + 0x10, _module.BruteJustice, AID.LinkUp, 3.2f, 3, false, "Debuffs")
                .ActivateOnEnter<P2CompressedWaterLightning>(); // debuffs & icons are applied ~0.8s after cast end
        }

        private void P2ChakramOpticalSightPhoton(uint id, float delay)
        {
            ActorCastStart(id, _module.CruiseChaser, AID.OpticalSight, delay)
                .ActivateOnEnter<P2EyeOfTheChakram>();
            ActorCastEnd(id + 1, _module.CruiseChaser, 2);
            ComponentCondition<P2EyeOfTheChakram>(id + 2, 0.9f, comp => comp.NumCasts > 0, "Chakrams")
                .ActivateOnEnter<P2HawkBlasterOpticalSight>()
                .DeactivateOnExit<P2EyeOfTheChakram>();
            ActorCastStart(id + 0x10, _module.CruiseChaser, AID.Photon, 3.3f);
            ComponentCondition<P2HawkBlasterOpticalSight>(id + 0x11, 1.1f, comp => comp.NumCasts > 0, "Puddles")
                .DeactivateOnExit<P2HawkBlasterOpticalSight>();
            ActorCastEnd(id + 0x12, _module.CruiseChaser, 1.9f)
                .OnEnter(() => Module.FindComponent<P2Nisi>()!.ShowPassHint = 1) // first nisi pass should happen around photon cast end
                .OnExit(() => Module.FindComponent<P2CompressedWaterLightning>()!.ResolveImminent = true); // should start moving to debuff stacks after nisi pass
            ComponentCondition<P2Photon>(id + 0x13, 0.3f, comp => comp.NumCasts > 0, "Photon")
                .ActivateOnEnter<P2Photon>()
                .DeactivateOnExit<P2Photon>()
                .SetHint(StateMachine.StateHint.Raidwide);
        }

        // keeps tornado component active
        private void P2SpinCrusherCompressedWaterLightning(uint id, float delay)
        {
            ActorCast(id, _module.CruiseChaser, AID.SpinCrusher, delay, 3, false, "Baited cleave")
                .ActivateOnEnter<P2SpinCrusher>()
                .DeactivateOnExit<P2SpinCrusher>();
            ComponentCondition<P2CompressedWaterLightning>(id + 0x10, 4.6f, comp => !comp.ResolveImminent, "Water/lightning 1")
                .ActivateOnEnter<P2Drainage>(); // tornado spawns ~1s after resolve
            // +0.6s: vulns applied to previous stack targets
            // +0.7s: icons/debuffs applied to next stack targets
            // +0.8s: first sets of nisis expire
        }

        private void P2MissileCommand(uint id, float delay)
        {
            ActorCast(id, _module.BruteJustice, AID.MissileCommand, delay, 3);
            ComponentCondition<P2EarthMissileBaited>(id + 0x10, 1.2f, comp => comp.HaveCasters, "Bait missiles")
                .ActivateOnEnter<P2EarthMissileBaited>();
            ComponentCondition<P2Enumeration>(id + 0x20, 1.9f, comp => comp.Active)
                .ActivateOnEnter<P2Enumeration>();
            ComponentCondition<P2HiddenMinefield>(id + 0x30, 0.1f, comp => comp.Casters.Count > 0)
                .ActivateOnEnter<P2HiddenMinefield>();
            ComponentCondition<P2EarthMissileBaited>(id + 0x40, 1.0f, comp => !comp.HaveCasters); // voidzones appear at cast positions with a slight delay
            ComponentCondition<P2HiddenMinefield>(id + 0x50, 2.0f, comp => comp.Casters.Count == 0);
            ComponentCondition<P2Enumeration>(id + 0x60, 2.1f, comp => !comp.Active, "Enumerations + Ice")
                .ActivateOnEnter<P2EarthMissileIce>()
                .DeactivateOnExit<P2Enumeration>();
            ComponentCondition<P2EarthMissileIce>(id + 0x70, 0.8f, comp => comp.Sources(Module).Any());
            // +4.0s: ice voidzone grows
            // +5.6s: gelid gaol spawns where tornado is (assuming it is in ice voidzone)
            // +6.3s: tornado is destroyed
            // +6.3s: if any mine is not soaked, they explode now
            // +6.8s: smaller ice voidzone disappears (eventstate 7)
            // +7.7s: fire voidzones disappear (eventstate 7)
            ComponentCondition<P2EarthMissileIce>(id + 0x80, 9.8f, comp => !comp.Sources(Module).Any(), "Voidzones disappear")
                .OnEnter(() => Module.FindComponent<P2Nisi>()!.ShowPassHint = 2) // second nisi pass should happen after enumerations are resolved
                .OnEnter(() => Module.FindComponent<P2CompressedWaterLightning>()!.ResolveImminent = true) // should start moving to debuff stacks after nisi pass
                .DeactivateOnExit<P2Drainage>()
                .DeactivateOnExit<P2EarthMissileBaited>()
                .DeactivateOnExit<P2HiddenMinefield>()
                .DeactivateOnExit<P2EarthMissileIce>();
        }

        private void P2VerdictGavel(uint id, float delay)
        {
            ActorCastStart(id, _module.BruteJustice, AID.Verdict, delay);
            ComponentCondition<P2CompressedWaterLightning>(id + 0x10, 2.2f, comp => !comp.ResolveImminent, "Water/lightning 2")
                .ActivateOnEnter<P2Drainage>();
            ActorCastEnd(id + 0x20, _module.BruteJustice, 1.8f); // judgment debuffs appear ~0.8s after cast end

            ActorCast(id + 0x100, _module.CruiseChaser, AID.LimitCutP2, 3.2f, 2, false, "CC invuln") // note: BJ starts flarethrower cast together with CC; invuln is applied ~0.6s after cast end
                .ActivateOnEnter<P2Flarethrower>();
            ActorCastEnd(id + 0x102, _module.BruteJustice, 1.9f)
                .ActivateOnEnter<P2PlasmaShield>();
            ComponentCondition<P2Flarethrower>(id + 0x103, 0.3f, comp => comp.NumCasts > 0, "Baited flamethrower")
                .DeactivateOnExit<P2Flarethrower>() // note: tornado is normally destroyed by a flarethrower, failing to do that will cause tornado to wipe the raid later
                .OnExit(() => Module.FindComponent<P2Nisi>()!.ShowPassHint = 3) // third nisi pass should happen after flarethrower bait
                .OnExit(() => Module.FindComponent<P2CompressedWaterLightning>()!.ResolveImminent = true); // resolve stacks after nisi pass
            ActorCast(id + 0x110, _module.CruiseChaser, AID.Whirlwind, 8.0f, 4, false, "Raidwide")
                .DeactivateOnExit<P2PlasmaShield>() // it's a wipe if shield is not dealth with in time
                .SetHint(StateMachine.StateHint.Raidwide);

            ComponentCondition<P2CompressedWaterLightning>(id + 0x200, 8.7f, comp => !comp.ResolveImminent, "Water/lightning 3")
                .DeactivateOnExit<P2CompressedWaterLightning>()
                .OnExit(() => Module.FindComponent<P2Nisi>()!.ShowPassHint = 4); // fourth nisi pass should happen after last stacks, while resolving propeller wind

            ActorCastStart(id + 0x300, _module.CruiseChaser, AID.PropellerWind, 12.5f);
            ActorCastStart(id + 0x301, _module.BruteJustice, AID.Gavel, 3)
                .ActivateOnEnter<P2PropellerWind>();
            ActorCastEnd(id + 0x302, _module.CruiseChaser, 3, false, "LOS")
                .DeactivateOnExit<P2PropellerWind>();
            ActorCastEnd(id + 0x303, _module.BruteJustice, 2, false, "Gavel");
            ComponentCondition<P2Nisi>(id + 0x304, 2, comp => comp.NumActiveNisi == 0, "Nisi resolve")
                .DeactivateOnExit<P2Nisi>()
                .DeactivateOnExit<P2Drainage>();
        }

        private void P2PhotonDoubleRocketPunch(uint id, float delay)
        {
            ActorCast(id, _module.CruiseChaser, AID.Photon, delay, 3);
            ComponentCondition<P2Photon>(id + 2, 0.3f, comp => comp.NumCasts > 0, "Photon")
                .ActivateOnEnter<P2Photon>()
                .DeactivateOnExit<P2Photon>()
                .SetHint(StateMachine.StateHint.Tankbuster);
            ActorCast(id + 0x100, _module.BruteJustice, AID.DoubleRocketPunch, 3.4f, 4, false, "Shared tankbuster")
                .ActivateOnEnter<P2DoubleRocketPunch>()
                .DeactivateOnExit<P2DoubleRocketPunch>()
                .SetHint(StateMachine.StateHint.Tankbuster);
        }

        private void P2SuperJumpApocalypticRay(uint id, float delay)
        {
            ActorCast(id, _module.BruteJustice, AID.SuperJump, delay, 3.9f)
                .ActivateOnEnter<P2SuperJump>();
            ComponentCondition<P2SuperJump>(id + 2, 0.4f, comp => comp.NumCasts > 0, "Jump")
                .DeactivateOnExit<P2SuperJump>();

            ComponentCondition<P2ApocalypticRay>(id + 0x10, 2.3f, comp => comp.Source != null)
                .ActivateOnEnter<P2ApocalypticRay>();
            ComponentCondition<P2ApocalypticRay>(id + 0x15, 4.9f, comp => comp.NumCasts >= 5, "AOE end")
                .DeactivateOnExit<P2ApocalypticRay>();
        }

        private void P2Whirlwind(uint id, float delay)
        {
            ActorCast(id, _module.CruiseChaser, AID.Whirlwind, delay, 4, false, "Raidwide")
                .SetHint(StateMachine.StateHint.Raidwide);
        }

        private void Phase3AlexanderPrime(uint id)
        {
            P3TemporalStasis(id, 0);
            P3ChasteningHeat(id + 0x10000, 5.1f, false);
            P3InceptionFormation(id + 0x20000, 5.3f);
            P3ChasteningHeat(id + 0x30000, 5.1f, true);
            P3WormholeFormation(id + 0x40000, 5.2f);
            P3MegaHoly(id + 0x50000, 4.7f);
            P3SummonAlexander(id + 0x60000, 6.4f);
            SimpleState(id + 0xFF0000, 200, "???");
        }

        private void P3TemporalStasis(uint id, float delay)
        {
            ActorTargetable(id, _module.AlexPrime, false, delay)
                .SetHint(StateMachine.StateHint.DowntimeStart);
            ActorCast(id + 0x10, _module.AlexPrime, AID.TemporalStasis, 7.5f, 8, true)
                .ActivateOnEnter<P3TemporalStasis>();
            ComponentCondition<P3TemporalStasis>(id + 0x20, 1.2f, comp => comp.Frozen, "Temporal stasis");
            ComponentCondition<P3TemporalStasis>(id + 0x30, 6.4f, comp => comp.NumCasts >= 2)
                .DeactivateOnExit<P3TemporalStasis>();
            ActorTargetable(id + 0x40, _module.AlexPrime, true, 3.7f, "Boss appears")
                .SetHint(StateMachine.StateHint.DowntimeEnd);
        }

        private void P3ChasteningHeat(uint id, float delay, bool longDivineSpearDelay)
        {
            ActorCast(id, _module.AlexPrime, AID.ChasteningHeat, delay, 5, true, "Tankbuster (vuln)")
                .ActivateOnEnter<P3ChasteningHeat>()
                .DeactivateOnExit<P3ChasteningHeat>()
                .SetHint(StateMachine.StateHint.Tankbuster);
            ComponentCondition<P3DivineSpear>(id + 0x10, longDivineSpearDelay ? 5.2f : 3.2f, comp => comp.NumCasts >= 1)
                .ActivateOnEnter<P3DivineSpear>()
                .SetHint(StateMachine.StateHint.Tankbuster);
            ComponentCondition<P3DivineSpear>(id + 0x11, 2.1f, comp => comp.NumCasts >= 2)
                .SetHint(StateMachine.StateHint.Tankbuster);
            ComponentCondition<P3DivineSpear>(id + 0x12, 2.1f, comp => comp.NumCasts >= 3, "Tankbuster (cones)")
                .DeactivateOnExit<P3DivineSpear>()
                .SetHint(StateMachine.StateHint.Tankbuster);
        }

        private void P3InceptionFormation(uint id, float delay)
        {
            ActorCast(id, _module.AlexPrime, AID.InceptionFormation, delay, 4, true);
            ActorTargetable(id + 0x10, _module.AlexPrime, false, 3.1f, "Inception formation")
                .SetHint(StateMachine.StateHint.DowntimeStart);
            ComponentCondition<P3Inception1>(id + 0x20, 4.2f, comp => comp.AllSpheresSpawned)
                .ActivateOnEnter<P3Inception1>()
                .ActivateOnEnter<P3Inception2>(); // note: activated early, since spheres potentially could be intercepted early, and we track their casts to start showing hints
            ActorCast(id + 0x30, _module.AlexPrime, AID.JudgmentCrystal, 4.2f, 3, true);
            // +0.7s: remaining 4 players get icon 96
            ComponentCondition<P3Inception1>(id + 0x40, 5.8f, comp => comp.CrystalsDone, "Crystals");
            ActorTargetable(id + 0x50, _module.TrueHeart, true, 0.7f);

            ComponentCondition<P3Inception2>(id + 0x100, 10.6f, comp => comp.NumCasts >= 1, "Bait 1")
                .DeactivateOnExit<P3Inception1>();
            ComponentCondition<P3Inception2>(id + 0x101, 2.2f, comp => comp.NumCasts >= 2, "Bait 2");
            ComponentCondition<P3Inception2>(id + 0x102, 2.2f, comp => comp.NumCasts >= 3, "Bait 3")
                .DeactivateOnExit<P3Inception2>();

            // debuffs (restraining x2, aggravated x2, shared) appear right before cast start
            ActorCast(id + 0x200, _module.AlexPrime, AID.Inception, 2.2f, 5, true);
            Condition(id + 0x208, 4.0f, () => _module.TrueHeart()?.IsDead ?? true, "Heart disappears");
            ComponentCondition<P3Inception3Sacrament>(id + 0x210, 4.3f, comp => comp.NumCasts > 0, "Shared sentence")
                .ActivateOnEnter<P3Inception3Sacrament>()
                .ActivateOnEnter<P3Inception3Debuffs>()
                .DeactivateOnExit<P3Inception3Debuffs>() // note: debuffs resolve ~0.3s before sacrament
                .DeactivateOnExit<P3Inception3Sacrament>();

            ActorCastStart(id + 0x300, _module.BruteJustice, AID.SuperJump, 5.1f)
                .ActivateOnEnter<P2SuperJump>()
                .ActivateOnEnter<P3Inception4Cleaves>();
            ComponentCondition<P3Inception4Cleaves>(id + 0x301, 0.9f, comp => comp.NumCasts >= 1);
            ComponentCondition<P3Inception4Cleaves>(id + 0x302, 1.1f, comp => comp.NumCasts >= 2);
            ComponentCondition<P3Inception4Cleaves>(id + 0x303, 1.1f, comp => comp.NumCasts >= 3)
                .DeactivateOnExit<P3Inception4Cleaves>();
            ActorCastEnd(id + 0x304, _module.BruteJustice, 0.8f);
            ComponentCondition<P2SuperJump>(id + 0x305, 0.3f, comp => comp.NumCasts > 0, "Jump")
                .DeactivateOnExit<P2SuperJump>();

            ActorTargetable(id + 0x400, _module.AlexPrime, true, 2.1f, "Inception resolve")
                .SetHint(StateMachine.StateHint.DowntimeEnd);
        }

        // TODO: components...
        private void P3WormholeFormation(uint id, float delay)
        {
            ActorCast(id, _module.AlexPrime, AID.WormholeFormation, delay, 4);
            ActorTargetable(id + 0x10, _module.AlexPrime, false, 3.1f, "Wormhole formation")
                .SetHint(StateMachine.StateHint.DowntimeStart);
            // +2.2s: PATE 1E43 on alex & bj & cc in their spots
            // +3.4s: CC starts casting limit cut (to 5.4)
            // +5.4s: BJ starts casting link-up (to 8.4)
            ActorCast(id + 0x100, _module.AlexPrime, AID.VoidOfRepentance, 8.4f, 3, true)
                .ActivateOnEnter<P2IntermissionOrder>() // icons appear ~0.7s after cast start
                .ActivateOnEnter<P2EyeOfTheChakram>(); // chakrams start casts around cast end
            // +1.0s: wormholes appear
            // +1.8s: eobjanim 00010002 on wormholes
            // +3.2s: BJ starts super jump
            ComponentCondition<P2EyeOfTheChakram>(id + 0x200, 6, comp => comp.NumCasts > 0, "Chakrams")
                .ActivateOnEnter<P2IntermissionKnockbacks>() // TODO: replace with proper component...
                .DeactivateOnExit<P2EyeOfTheChakram>();
            // +1.1s: BJ ends super jump
            // +1.3s: alpha sword 1
            // +1.4s: super jump aoe
            ActorCast(id + 0x300, _module.AlexPrime, AID.SacramentWormhole, 2.2f, 6, true);
            // (from cast start)
            // +0.6s: super blasty 2
            // +1.6s: BJ apoc ray
            // ...
            // (from cast end)
            // +5.7s: BJ starts missile command
            ActorCast(id + 0x400, _module.AlexPrime, AID.IncineratingHeat, 7.2f, 5, true, "Stack")
                .SetHint(StateMachine.StateHint.Raidwide);
            // ?? enumerations?

            ActorTargetable(id + 0x500, _module.AlexPrime, true, 4.3f, "Wormhole resolve")
                .DeactivateOnExit<P2IntermissionOrder>()
                .SetHint(StateMachine.StateHint.DowntimeEnd);
        }

        private void P3MegaHoly(uint id, float delay)
        {
            ActorCast(id, _module.AlexPrime, AID.MegaHoly, delay, 4, true, "Raidwide")
                .SetHint(StateMachine.StateHint.Raidwide);
            ActorCast(id + 0x10, _module.AlexPrime, AID.MegaHoly, 3.2f, 4, true, "Raidwide")
                .SetHint(StateMachine.StateHint.Raidwide);
        }

        private void P3SummonAlexander(uint id, float delay)
        {
            ActorCast(id, _module.AlexPrime, AID.SummonAlexander, delay, 3, true);
            ActorTargetable(id + 0x10, _module.AlexPrime, false, 6.2f, "Alex disappers");
            // +0.2s: j storm
            // +1.2s: cc & alex targetable
            // +1.3s: cc & alex start casting their enrages
            // +2.2s: bj targetable
            // +5.4s: j wave 1, then every 3s
        }
    }
}
