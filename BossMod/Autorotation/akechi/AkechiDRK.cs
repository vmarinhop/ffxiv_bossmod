﻿using FFXIVClientStructs.FFXIV.Client.Game.Gauge;
using BossMod.DRK;

namespace BossMod.Autorotation.akechi;
//Contribution by Akechi
//Discord: @akechdz or 'Akechi' on Puni.sh for maintenance

public sealed class AkechiDRK(RotationModuleManager manager, Actor player) : AkechiTools<AID, TraitID>(manager, player)
{
    #region Enums: Abilities / Strategies
    public enum Track
    {
        Blood                //Blood abilities tracking
        = SharedTrack.Count, //Shared tracking
        MPactions,                  //MP actions tracking
        Carve,               //Carve and Spit & Abyssal Drain tracking
        DeliriumCombo,        //Scarlet Combo ability tracking
        Potion,              //Potion item tracking
        Unmend,              //Ranged ability tracking
        Delirium,            //Delirium ability tracking
        SaltedEarth,         //Salted Earth ability tracking
        SaltAndDarkness,     //Salt and Darkness ability tracking
        LivingShadow,        //Living Shadow ability tracking
        Shadowbringer,       //Shadowbringer ability tracking
        Disesteem            //Disesteem ability tracking
    }
    public enum BloodStrategy
    {
        Automatic,           //Automatically decide when to use Burst Strike & Fated Circle
        OnlyBloodspiller,    //Use Bloodspiller optimally as Blood spender only, regardless of targets
        OnlyQuietus,         //Use Quietus optimally as Blood spender only, regardless of targets
        ForceBloodspiller,   //Force use of Bloodspiller
        ForceQuietus,        //Force use of Quietus
        Conserve             //Conserves all Blood-related abilities as much as possible
    }
    public enum MPStrategy
    {
        Auto3k,              //Automatically decide best MP action to use; Uses when at 3000+ MP
        Auto6k,              //Automatically decide best MP action to use; Uses when at 6000+ MP
        Auto9k,              //Automatically decide best MP action to use; Uses when at 9000+ MP
        AutoRefresh,         //Automatically decide best MP action to use
        Edge3k,              //Use Edge of Shadow as Darkside refresher & MP spender; Uses when at 3000+ MP
        Edge6k,              //Use Edge of Shadow as Darkside refresher & MP spender; Uses when at 6000+ MP
        Edge9k,              //Use Edge of Shadow as Darkside refresher & MP spender; Uses when at 9000+ MP
        EdgeRefresh,         //Use Edge of Shadow as Darkside refresher only
        Flood3k,             //Use Flood of Shadow as Darkside refresher & MP spender; Uses when at 3000+ MP
        Flood6k,             //Use Flood of Shadow as Darkside refresher & MP spender; Uses when at 6000+ MP
        Flood9k,             //Use Flood of Shadow as Darkside refresher & MP spender; Uses when at 9000+ MP
        FloodRefresh,        //Use Flood of Shadow as Darkside refresher only
        Delay                //Delay the use of MP actions for strategic reasons
    }
    public enum CarveStrategy
    {
        Automatic,           //Automatically decide when to use either Carve and Spit or Abyssal Drain
        OnlyCarve,
        OnlyDrain,
        ForceCarve,
        ForceDrain,
        Delay                //Delay the use of Carve and Spit and Abyssal Drain for strategic reasons
    }
    public enum DeliriumComboStrategy
    {
        Automatic,           //Automatically decide when to use Scarlet Combo
        ScarletDelirum,      //Force use of Scarlet Delirium
        Comeuppance,         //Force use of Comeuppance
        Torcleaver,          //Force use of Torcleaver
        Impalement,          //Force use of Impalement
        Delay                //Delay the use of Scarlet Combo for strategic reasons
    }
    public enum PotionStrategy
    {
        Manual,              //Manual potion usage
        AlignWithRaidBuffs,  //Align potion usage with raid buffs
        Immediate            //Use potions immediately when available
    }
    public enum UnmendStrategy
    {
        OpenerFar,           //Only use Unmend in pre-pull & out of melee range
        OpenerForce,         //Force use Unmend in pre-pull in any range
        Force,               //Force the use of Unmend in any range
        Allow,               //Allow the use of Unmend when out of melee range
        Forbid               //Prohibit the use of Unmend
    }
    #endregion

    #region Module Definitions & Strategies
    public static RotationModuleDefinition Definition()
    {
        var res = new RotationModuleDefinition("Akechi DRK", //Title
            "Standard Rotation Module", //Description
            "Standard rotation (Akechi)", //Category
            "Akechi", //Contributor
            RotationModuleQuality.Basic, //Quality
            BitMask.Build((int)Class.DRK), //Job
            100); //Level supported

        #region Strategies

        res.DefineShared();

        res.Define(Track.Blood).As<BloodStrategy>("Blood", "Carts", uiPriority: 180)
            .AddOption(BloodStrategy.Automatic, "Automatic", "Automatically decide when to use Blood optimally")
            .AddOption(BloodStrategy.OnlyBloodspiller, "Only Bloodspiller", "Uses Bloodspiller optimally as Blood spender only, regardless of targets", 0, 0, ActionTargets.Hostile, 62)
            .AddOption(BloodStrategy.OnlyQuietus, "Only Quietus", "Uses Quietus optimally as Blood spender only, regardless of targets", 0, 0, ActionTargets.Hostile, 64)
            .AddOption(BloodStrategy.ForceBloodspiller, "Force Bloodspiller", "Force use of Bloodspiller", 0, 0, ActionTargets.Hostile, 62)
            .AddOption(BloodStrategy.ForceQuietus, "Force Quietus", "Force use of Quietus", 0, 0, ActionTargets.Hostile, 64)
            .AddOption(BloodStrategy.Conserve, "Conserve", "Conserves all Blood-related abilities as much as possible");

        res.Define(Track.MPactions).As<MPStrategy>("MP", "MP", uiPriority: 170)
            .AddOption(MPStrategy.Auto3k, "Auto 3k", "Automatically decide best MP action to use; Uses when at 3000+ MP", 0, 0, ActionTargets.Self, 30)
            .AddOption(MPStrategy.Auto6k, "Auto 6k", "Automatically decide best MP action to use; Uses when at 6000+ MP", 0, 0, ActionTargets.Self, 30)
            .AddOption(MPStrategy.Auto9k, "Auto 9k", "Automatically decide best MP action to use; Uses when at 9000+ MP", 0, 0, ActionTargets.Self, 30)
            .AddOption(MPStrategy.AutoRefresh, "Auto Refresh", "Automatically decide best MP action to use as Darkside refresher only", 0, 0, ActionTargets.Self, 30)
            .AddOption(MPStrategy.Edge3k, "Edge 3k", "Use Edge of Shadow as Darkside refresher & MP spender; Uses when at 3000+ MP", 0, 0, ActionTargets.Self, 40)
            .AddOption(MPStrategy.Edge6k, "Edge 6k", "Use Edge of Shadow as Darkside refresher & MP spender; Uses when at 6000+ MP", 0, 0, ActionTargets.Self, 40)
            .AddOption(MPStrategy.Edge9k, "Edge 9k", "Use Edge of Shadow as Darkside refresher & MP spender; Uses when at 9000+ MP", 0, 0, ActionTargets.Self, 40)
            .AddOption(MPStrategy.EdgeRefresh, "Edge Refresh", "Use Edge of Shadow as Darkside refresher only", 0, 0, ActionTargets.Self, 40)
            .AddOption(MPStrategy.Flood3k, "Flood 3k", "Use Flood of Shadow as Darkside refresher & MP spender; Uses when at 3000+ MP", 0, 0, ActionTargets.Self, 30)
            .AddOption(MPStrategy.Flood6k, "Flood 6k", "Use Flood of Shadow as Darkside refresher & MP spender; Uses when at 6000+ MP", 0, 0, ActionTargets.Self, 30)
            .AddOption(MPStrategy.Flood9k, "Flood 9k", "Use Flood of Shadow as Darkside refresher & MP spender; Uses when at 9000+ MP", 0, 0, ActionTargets.Self, 30)
            .AddOption(MPStrategy.FloodRefresh, "Flood Refresh", "Use Flood of Shadow as Darkside refresher only", 0, 0, ActionTargets.Self, 30)
            .AddOption(MPStrategy.Delay, "Delay", "Delay the use of MP actions for strategic reasons", 0, 0, ActionTargets.None, 30)
            .AddAssociatedActions(AID.EdgeOfDarkness, AID.EdgeOfShadow, AID.FloodOfDarkness, AID.FloodOfShadow);

        res.Define(Track.Carve).As<CarveStrategy>("Carve", "Carve", uiPriority: 160)
            .AddOption(CarveStrategy.Automatic, "Auto", "Automatically decide when to use either Carve and Spit or Abyssal Drain")
            .AddOption(CarveStrategy.OnlyCarve, "Only Carve and Spit", "Automatically use Carve and Spit as optimal spender", 0, 0, ActionTargets.Hostile, 60)
            .AddOption(CarveStrategy.OnlyDrain, "Only Abysssal Drain", "Automatically use Abyssal Drain as optimal spender", 0, 0, ActionTargets.Hostile, 56)
            .AddOption(CarveStrategy.ForceCarve, "Force Carve and Spit", "Force the use of Carve and Spit", 60, 0, ActionTargets.Hostile, 60)
            .AddOption(CarveStrategy.ForceDrain, "Force Abyssal Drain", "Force the use of Abyssal Drain", 60, 0, ActionTargets.Hostile, 56)
            .AddOption(CarveStrategy.Delay, "Delay", "Delay the use of Carve and Spit for strategic reasons", 0, 0, ActionTargets.None, 56)
            .AddAssociatedActions(AID.CarveAndSpit);

        res.Define(Track.DeliriumCombo).As<DeliriumComboStrategy>("Scarlet Combo", "DeliriumCombo", uiPriority: 150)
            .AddOption(DeliriumComboStrategy.Automatic, "Auto", "Automatically decide when to use Scarlet Combo", 0, 0, ActionTargets.Hostile, 96)
            .AddOption(DeliriumComboStrategy.ScarletDelirum, "Scarlet Delirium", "Force use of Scarlet Delirium", 0, 0, ActionTargets.Hostile, 96)
            .AddOption(DeliriumComboStrategy.Comeuppance, "Comeuppance", "Force use of Comeuppance", 0, 0, ActionTargets.Hostile, 96)
            .AddOption(DeliriumComboStrategy.Torcleaver, "Torcleaver", "Force use of Torcleaver", 0, 0, ActionTargets.Hostile, 96)
            .AddOption(DeliriumComboStrategy.Impalement, "Impalement", "Force use of Impalement", 0, 0, ActionTargets.Hostile, 96)
            .AddOption(DeliriumComboStrategy.Delay, "Delay", "Delay use of Scarlet combo for strategic reasons", 0, 0, ActionTargets.Hostile, 96)
            .AddAssociatedActions(AID.ScarletDelirium, AID.Comeuppance, AID.Torcleaver, AID.Impalement);

        res.Define(Track.Potion).As<PotionStrategy>("Potion", uiPriority: 20)
            .AddOption(PotionStrategy.Manual, "Manual", "Do not use automatically")
            .AddOption(PotionStrategy.AlignWithRaidBuffs, "AlignWithRaidBuffs", "Align with No Mercy & Bloodfest together (to ensure use on 2-minute windows)", 270, 30, ActionTargets.Self)
            .AddOption(PotionStrategy.Immediate, "Immediate", "Use ASAP, regardless of any buffs", 270, 30, ActionTargets.Self)
            .AddAssociatedAction(ActionDefinitions.IDPotionStr);

        res.Define(Track.Unmend).As<UnmendStrategy>("Unmend", "Unmend", uiPriority: 30)
            .AddOption(UnmendStrategy.OpenerFar, "Far (Opener)", "Use Unmend in pre-pull & out of melee range", supportedTargets: ActionTargets.Hostile)
            .AddOption(UnmendStrategy.OpenerForce, "Force (Opener)", "Force use Unmend in pre-pull in any range", supportedTargets: ActionTargets.Hostile)
            .AddOption(UnmendStrategy.Force, "Force", "Force use Unmend in any range", supportedTargets: ActionTargets.Hostile)
            .AddOption(UnmendStrategy.Allow, "Allow", "Allow use of Unmend when out of melee range", supportedTargets: ActionTargets.Hostile)
            .AddOption(UnmendStrategy.Forbid, "Forbid", "Prohibit use of Unmend")
            .AddAssociatedActions(AID.Unmend);

        res.DefineOGCD(Track.Delirium, "Delirium", "Delirium", 60, 15, ActionTargets.Self, 35).AddAssociatedActions(AID.Delirium);
        res.DefineOGCD(Track.SaltedEarth, "Salted Earth", "S.Earth", 90, 15, ActionTargets.Self, 52).AddAssociatedActions(AID.SaltedEarth);
        res.DefineOGCD(Track.SaltAndDarkness, "Salt & Darkness", "S&D", 20, 0, ActionTargets.Self, 86).AddAssociatedActions(AID.SaltAndDarkness);
        res.DefineOGCD(Track.LivingShadow, "Living Shadow", "L.Shadow", 120, 20, ActionTargets.Self, 80).AddAssociatedActions(AID.LivingShadow);
        res.DefineOGCD(Track.Shadowbringer, "Shadowbringer", "S.bringer", 60, 0, ActionTargets.Hostile, 90).AddAssociatedActions(AID.Shadowbringer);
        res.DefineGCD(Track.Disesteem, "Disesteem", "Disesteem", supportedTargets: ActionTargets.Hostile, minLevel: 100).AddAssociatedActions(AID.Disesteem);
        #endregion

        return res;
    }
    #endregion

    #region Priorities
    public enum GCDPriority
    {
        None = 0,
        Standard = 300,
        Blood = 400,
        Disesteem = 500,
        DeliriumCombo = 600,
        NeedBlood = 700,
        Opener = 800,
        ForcedGCD = 900,
    }
    public enum OGCDPriority
    {
        None = 0,
        Standard = 300,
        MP = 350,
        SaltedEarth = 400,
        CarveOrDrain = 450,
        Shadowbringer = 500,
        Delirium = 550,
        LivingShadow = 600,
        NeedRefresh = 700,
        ForcedOGCD = 900,
    }
    #endregion

    #region Upgrade Paths
    private AID BestEdge => Unlocked(AID.EdgeOfShadow) ? AID.EdgeOfShadow : Unlocked(AID.EdgeOfDarkness) ? AID.EdgeOfDarkness : AID.FloodOfDarkness;
    private AID BestFlood => Unlocked(AID.FloodOfShadow) ? AID.FloodOfShadow : AID.FloodOfDarkness;
    private AID BestMPSpender => ShouldUseAOECircle(5).OnThreeOrMore ? BestAOEMPSpender : BestEdge;
    private AID BestAOEMPSpender => Unlocked(AID.FloodOfShadow) && ShouldUseAOECircle(5).OnThreeOrMore ? AID.FloodOfDarkness : Unlocked(AID.FloodOfDarkness) && ShouldUseAOECircle(5).OnFourOrMore ? AID.FloodOfDarkness : BestEdge;
    private AID BestQuietus => Unlocked(AID.Quietus) ? AID.Quietus : AID.Bloodspiller;
    private AID BestBloodSpender => ShouldUseAOE ? BestQuietus : AID.Bloodspiller;
    private AID BestDelirium => Unlocked(AID.Delirium) ? AID.Delirium : AID.BloodWeapon;
    private AID CarveOrDrain => ShouldUseAOE ? AID.AbyssalDrain : AID.CarveAndSpit;
    private AID BestSalt => Unlocked(AID.SaltAndDarkness) && PlayerHasEffect(SID.SaltedEarth, 15) ? AID.SaltAndDarkness : AID.SaltedEarth;
    private SID BestBloodWeapon => Unlocked(AID.ScarletDelirium) ? SID.EnhancedDelirium : Unlocked(AID.Delirium) ? SID.Delirium : SID.BloodWeapon;
    private AID DeliriumCombo => Delirium.Step is 2 ? AID.Torcleaver : Delirium.Step is 1 ? AID.Comeuppance : ShouldUseAOE ? AID.Impalement : AID.ScarletDelirium;
    #endregion

    #region Module Variables
    public byte Blood;
    public (byte State, bool IsActive) DarkArts;
    public (float Timer, bool IsActive, bool NeedsRefresh) Darkside;
    public bool RiskingBlood;
    public bool RiskingMP;
    public (float Left, float CD, bool IsActive, bool IsReady) SaltedEarth;
    public (float CD, bool IsReady) AbyssalDrain;
    public (float CD, bool IsReady) CarveAndSpit;
    public (ushort Step, float Left, int Stacks, float CD, bool IsActive, bool IsReady) Delirium;
    public (float Timer, float CD, bool IsActive, bool IsReady) LivingShadow;
    public (float TotalCD, float ChargeCD, bool HasCharges, bool IsReady) Shadowbringer;
    public (float Left, bool IsActive, bool IsReady) Disesteem;
    private bool ShouldUseAOE; //Checks if AOE rotation or abilities should be used
    public int NumAOERectTargets;
    public Actor? BestAOERectTarget;
    #endregion

    public override void Execute(StrategyValues strategy, Actor? primaryTarget, float estimatedAnimLockDelay, bool isMoving) //Executes our actions
    {
        #region Variables
        #region Gauge
        var gauge = World.Client.GetGauge<DarkKnightGauge>(); //Retrieve DRK gauge
        Blood = gauge.Blood;
        DarkArts.State = gauge.DarkArtsState; //Retrieve current Dark Arts state
        DarkArts.IsActive = DarkArts.State > 0; //Checks if Dark Arts is active
        Darkside.Timer = gauge.DarksideTimer / 1000f; //Retrieve current Darkside timer
        Darkside.IsActive = Darkside.Timer > 0.1f; //Checks if Darkside is active
        Darkside.NeedsRefresh = Darkside.Timer <= 3; //Checks if Darkside needs to be refreshed
        RiskingBlood =
            (ComboLastMove is AID.SyphonStrike or AID.Unleash && Blood >= 80) || (Delirium.CD <= 3 && Blood >= 70); //Checks if we are risking Blood
        RiskingMP = MP >= 9800 || Darkside.NeedsRefresh;
        #endregion
        #region Cooldowns
        SaltedEarth.Left = StatusRemaining(Player, SID.SaltedEarth, 15); //Retrieve current Salted Earth time left
        SaltedEarth.CD = TotalCD(AID.SaltedEarth); //Retrieve current Salted Earth cooldown
        SaltedEarth.IsActive = SaltedEarth.Left > 0.1f; //Checks if Salted Earth is active
        SaltedEarth.IsReady = Unlocked(AID.SaltedEarth) && SaltedEarth.CD < 0.6f; //Salted Earth ability
        AbyssalDrain.CD = TotalCD(AID.AbyssalDrain); //Retrieve current Abyssal Drain cooldown
        AbyssalDrain.IsReady = Unlocked(AID.AbyssalDrain) && AbyssalDrain.CD < 0.6f; //Abyssal Drain ability
        CarveAndSpit.CD = TotalCD(AID.CarveAndSpit); //Retrieve current Carve and Spit cooldown
        CarveAndSpit.IsReady = Unlocked(AID.CarveAndSpit) && CarveAndSpit.CD < 0.6f; //Carve and Spit ability
        Disesteem.Left = StatusRemaining(Player, SID.Scorn, 30); //Retrieve current Disesteem time left
        Disesteem.IsActive = Disesteem.Left > 0.1f; //Checks if Disesteem is active
        Disesteem.IsReady = Unlocked(AID.Disesteem) && Disesteem.Left > 0.1f; //Disesteem ability
        Delirium.Step = gauge.DeliriumStep; //Retrieve current Delirium combo step
        Delirium.Left = StatusRemaining(Player, BestBloodWeapon, 15); //Retrieve current Delirium time left
        Delirium.Stacks = StacksRemaining(Player, BestBloodWeapon, 15); //Retrieve current Delirium stacks
        Delirium.CD = TotalCD(BestDelirium); //Retrieve current Delirium cooldown
        Delirium.IsActive = Delirium.Left > 0.1f; //Checks if Delirium is active
        Delirium.IsReady = Unlocked(BestDelirium) && Delirium.CD < 0.6f; //Delirium ability
        LivingShadow.Timer = gauge.ShadowTimer / 1000f; //Retrieve current Living Shadow timer
        LivingShadow.CD = TotalCD(AID.LivingShadow); //Retrieve current Living Shadow cooldown
        LivingShadow.IsActive = LivingShadow.Timer > 0; //Checks if Living Shadow is active
        LivingShadow.IsReady = Unlocked(AID.LivingShadow) && LivingShadow.CD < 0.6f; //Living Shadow ability
        Shadowbringer.TotalCD = TotalCD(AID.Shadowbringer); //Retrieve current Shadowbringer cooldown
        Shadowbringer.ChargeCD = ChargeCD(AID.Shadowbringer); //Retrieve current Shadowbringer charge cooldown
        Shadowbringer.HasCharges = TotalCD(AID.Shadowbringer) <= 60; //Checks if Shadowbringer has charges
        Shadowbringer.IsReady = Unlocked(AID.Shadowbringer) && Shadowbringer.HasCharges; //Shadowbringer ability
        #endregion
        ShouldUseAOE = ShouldUseAOECircle(5).OnThreeOrMore;
        (BestAOERectTarget, NumAOERectTargets) = SelectBestTarget(strategy, primaryTarget, 10, (primary, other) => Hints.TargetInAOERect(other, Player.Position, Player.DirectionTo(primary), 10, 4));

        #region Strategy Definitions
        var mp = strategy.Option(Track.MPactions);
        var mpStrat = mp.As<MPStrategy>(); //Retrieve MP strategy
        var blood = strategy.Option(Track.Blood);
        var bloodStrat = blood.As<BloodStrategy>(); //Retrieve Blood strategy
        var se = strategy.Option(Track.SaltedEarth);
        var seStrat = se.As<OGCDStrategy>(); //Retrieve Salted Earth strategy
        var cd = strategy.Option(Track.Carve);
        var cdStrat = cd.As<CarveStrategy>(); //Retrieve Carve and Drain strategy
        var deli = strategy.Option(Track.Delirium);
        var deliStrat = deli.As<OGCDStrategy>(); //Retrieve Delirium strategy
        var ls = strategy.Option(Track.LivingShadow);
        var lsStrat = ls.As<OGCDStrategy>(); //Retrieve Living Shadow strategy
        var sb = strategy.Option(Track.Shadowbringer);
        var sbStrat = sb.As<OGCDStrategy>(); //Retrieve Shadowbringer strategy
        var dcombo = strategy.Option(Track.DeliriumCombo);
        var dcomboStrat = dcombo.As<DeliriumComboStrategy>(); //Retrieve Delirium combo strategy
        var de = strategy.Option(Track.Disesteem);
        var deStrat = de.As<GCDStrategy>(); //Retrieve Disesteem strategy
        var unmend = strategy.Option(Track.Unmend);
        var unmendStrat = unmend.As<UnmendStrategy>(); //Retrieve Unmend strategy
        #endregion
        #endregion

        #region Full Rotation Execution

        #region Standard Rotations
        if (ModuleExtensions.AutoAOE(strategy))
        {
            QueueGCD(NextBestRotation(), //queue the next single-target combo action only if combo is finished
                ResolveTargetOverride(strategy.Option(SharedTrack.AOE).Value) //Get target choice
                ?? primaryTarget, //if none, choose primary target
                GCDPriority.Standard); //with priority for 123/12 combo actions
        }
        if (ModuleExtensions.ForceST(strategy)) //if Force Single Target option is selected
        {
            QueueGCD(ST(),
                ResolveTargetOverride(strategy.Option(SharedTrack.AOE).Value) //Get target choice
                ?? primaryTarget, //if none, choose primary target
                GCDPriority.Standard); //with priority for 123/12 combo actions
        }
        if (ModuleExtensions.ForceAOE(strategy)) //if Force AOE option is selected
        {
            QueueGCD(AOE(),
                Player,
                GCDPriority.Standard); //with priority for 123/12 combo actions
        }
        #endregion

        if (!ModuleExtensions.HoldAll(strategy)) //if not holding cooldowns
        {
            if (!ModuleExtensions.HoldCooldowns(strategy)) //if holding cooldowns
            {
                if (ShouldUseSaltedEarth(seStrat, primaryTarget))
                    QueueOGCD(BestSalt,
                        Player,
                        seStrat is OGCDStrategy.Force
                        or OGCDStrategy.AnyWeave
                        or OGCDStrategy.EarlyWeave
                        or OGCDStrategy.LateWeave
                        ? OGCDPriority.ForcedOGCD
                        : OGCDPriority.SaltedEarth);

                if (ShouldUseCarveOrDrain(cdStrat, primaryTarget))
                {
                    if (cdStrat is CarveStrategy.Automatic)
                        QueueOGCD(CarveOrDrain,
                            TargetChoice(cd) ?? primaryTarget,
                            cdStrat is CarveStrategy.ForceCarve
                            or CarveStrategy.ForceDrain
                            ? OGCDPriority.ForcedOGCD
                            : OGCDPriority.CarveOrDrain);
                    if (cdStrat is CarveStrategy.OnlyCarve)
                        QueueOGCD(AID.CarveAndSpit,
                            TargetChoice(cd) ?? primaryTarget,
                            cdStrat is CarveStrategy.ForceCarve
                            ? OGCDPriority.ForcedOGCD
                            : OGCDPriority.CarveOrDrain);
                    if (cdStrat is CarveStrategy.OnlyDrain)
                        QueueOGCD(AID.AbyssalDrain,
                            TargetChoice(cd) ?? primaryTarget,
                            cdStrat is CarveStrategy.ForceDrain
                            ? OGCDPriority.ForcedOGCD
                            : OGCDPriority.CarveOrDrain);
                }

                if (ShouldUseDelirium(deliStrat, primaryTarget))
                    QueueOGCD(BestDelirium,
                        Player,
                        deliStrat is OGCDStrategy.Force
                        or OGCDStrategy.AnyWeave
                        or OGCDStrategy.EarlyWeave
                        or OGCDStrategy.LateWeave
                        ? OGCDPriority.ForcedOGCD
                        : OGCDPriority.Delirium);

                if (ShouldUseLivingShadow(lsStrat, primaryTarget))
                    QueueOGCD(AID.LivingShadow,
                        Player,
                        lsStrat is OGCDStrategy.Force
                        or OGCDStrategy.AnyWeave
                        or OGCDStrategy.EarlyWeave
                        or OGCDStrategy.LateWeave
                        ? OGCDPriority.ForcedOGCD
                        : OGCDPriority.LivingShadow);

                if (ShouldUseShadowbringer(sbStrat, primaryTarget))
                    QueueOGCD(AID.Shadowbringer,
                        TargetChoice(sb) ?? primaryTarget ?? BestAOERectTarget,
                        sbStrat is OGCDStrategy.Force
                        or OGCDStrategy.AnyWeave
                        or OGCDStrategy.EarlyWeave
                        or OGCDStrategy.LateWeave
                        ? OGCDPriority.ForcedOGCD
                        : OGCDPriority.Shadowbringer);

                if (ShouldUseDisesteem(deStrat, primaryTarget))
                    QueueGCD(AID.Disesteem,
                        TargetChoice(de) ?? primaryTarget ?? BestAOERectTarget,
                        deStrat is GCDStrategy.Force
                        ? GCDPriority.ForcedGCD
                        : CombatTimer < 30
                        ? GCDPriority.Opener
                        : GCDPriority.Disesteem);
            }
            if (!ModuleExtensions.HoldGauge(strategy)) //if holding gauge
            {
                if (ShouldUseBlood(bloodStrat, primaryTarget))
                {
                    if (bloodStrat is BloodStrategy.Automatic)
                        QueueGCD(BestBloodSpender, TargetChoice(blood) ?? primaryTarget, bloodStrat is BloodStrategy.ForceBloodspiller or BloodStrategy.ForceQuietus ? GCDPriority.ForcedGCD : RiskingBlood ? GCDPriority.NeedBlood : GCDPriority.Blood);
                    if (bloodStrat is BloodStrategy.OnlyBloodspiller)
                        QueueGCD(AID.Bloodspiller, TargetChoice(blood) ?? primaryTarget, bloodStrat is BloodStrategy.ForceBloodspiller ? GCDPriority.ForcedGCD : RiskingBlood ? GCDPriority.NeedBlood : GCDPriority.Blood);
                    if (bloodStrat is BloodStrategy.OnlyQuietus)
                        QueueGCD(AID.Quietus, Unlocked(AID.Quietus) ? Player : TargetChoice(blood) ?? primaryTarget, bloodStrat is BloodStrategy.ForceQuietus ? GCDPriority.ForcedGCD : RiskingBlood ? GCDPriority.NeedBlood : GCDPriority.Blood);
                }
            }
            if (ShouldUseMP(mpStrat, primaryTarget))
            {
                if (mpStrat is MPStrategy.Auto9k
                    or MPStrategy.Auto6k
                    or MPStrategy.Auto3k
                    or MPStrategy.AutoRefresh)
                    QueueOGCD(BestMPSpender, TargetChoice(mp) ?? primaryTarget ?? BestAOERectTarget, RiskingMP ? OGCDPriority.ForcedOGCD : OGCDPriority.MP);
                if (mpStrat is MPStrategy.Edge9k
                    or MPStrategy.Edge6k
                    or MPStrategy.Edge3k
                    or MPStrategy.EdgeRefresh)
                    QueueOGCD(BestEdge, TargetChoice(mp) ?? primaryTarget, RiskingMP ? OGCDPriority.ForcedOGCD : OGCDPriority.MP);
                if (mpStrat is MPStrategy.Flood9k
                    or MPStrategy.Flood6k
                    or MPStrategy.Flood3k
                    or MPStrategy.FloodRefresh)
                    QueueOGCD(BestFlood, TargetChoice(mp) ?? primaryTarget ?? BestAOERectTarget, RiskingMP ? OGCDPriority.ForcedOGCD : OGCDPriority.MP);
            }
        }

        if (ShouldUseSaltAndDarkness(strategy.Option(Track.SaltAndDarkness).As<OGCDStrategy>(), primaryTarget))
            QueueOGCD(AID.SaltAndDarkness, Player, OGCDPriority.SaltedEarth);

        if (ShouldUseDeliriumCombo(dcomboStrat, primaryTarget))
        {
            if (dcomboStrat is DeliriumComboStrategy.Automatic)
                QueueGCD(DeliriumCombo, TargetChoice(dcombo) ?? primaryTarget, GCDPriority.DeliriumCombo);
            if (dcomboStrat is DeliriumComboStrategy.ScarletDelirum)
                QueueGCD(AID.ScarletDelirium, TargetChoice(dcombo) ?? primaryTarget, GCDPriority.ForcedGCD);
            if (dcomboStrat is DeliriumComboStrategy.Comeuppance)
                QueueGCD(AID.Comeuppance, TargetChoice(dcombo) ?? primaryTarget, GCDPriority.ForcedGCD);
            if (dcomboStrat is DeliriumComboStrategy.Torcleaver)
                QueueGCD(AID.Torcleaver, TargetChoice(dcombo) ?? primaryTarget, GCDPriority.ForcedGCD);
            if (dcomboStrat is DeliriumComboStrategy.Impalement)
                QueueGCD(AID.Impalement, Player, GCDPriority.ForcedGCD);
        }

        if (ShouldUseUnmend(unmendStrat, primaryTarget))
            QueueGCD(AID.Unmend, TargetChoice(unmend) ?? primaryTarget, GCDPriority.Standard);
        if (ShouldUsePotion(strategy.Option(Track.Potion).As<PotionStrategy>()))
            Hints.ActionsToExecute.Push(ActionDefinitions.IDPotionStr, Player, ActionQueue.Priority.VeryHigh + (int)OGCDPriority.ForcedOGCD, 0, GCD - 0.9f);
        #endregion
    }

    #region Rotation Helpers
    private AID NextBestRotation() => ComboLastMove switch
    {
        //ST
        AID.Souleater => ShouldUseAOE ? AOE() : ST(),
        AID.SyphonStrike => ST(),
        AID.HardSlash => ST(),
        //AOE
        AID.StalwartSoul => ShouldUseAOE ? AOE() : ST(),
        AID.Unleash => AOE(),
        _ => ShouldUseAOE ? AOE() : ST(),
    };
    private AID ST() => ComboLastMove switch
    {
        AID.SyphonStrike => AID.Souleater,
        AID.HardSlash => AID.SyphonStrike,
        _ => AID.HardSlash,
    };
    private AID AOE() => ComboLastMove switch
    {
        AID.Unleash => AID.StalwartSoul,
        _ => AID.Unleash,
    };
    #endregion

    #region Cooldown Helpers
    private bool ShouldUseMP(MPStrategy strategy, Actor? target) => strategy switch
    {
        MPStrategy.Auto3k => CanWeaveIn && In10y(target) && MP >= 3000,
        MPStrategy.Auto6k => CanWeaveIn && In10y(target) && MP >= 6000,
        MPStrategy.Auto9k => CanWeaveIn && In10y(target) && MP >= 9000,
        MPStrategy.AutoRefresh => CanWeaveIn && In10y(target) && RiskingMP,
        MPStrategy.Edge3k => CanWeaveIn && In3y(target) && MP >= 3000,
        MPStrategy.Edge6k => CanWeaveIn && In3y(target) && MP >= 6000,
        MPStrategy.Edge9k => CanWeaveIn && In3y(target) && MP >= 9000,
        MPStrategy.EdgeRefresh => CanWeaveIn && In3y(target) && RiskingMP,
        MPStrategy.Flood3k => CanWeaveIn && In10y(target) && MP >= 3000,
        MPStrategy.Flood6k => CanWeaveIn && In10y(target) && MP >= 6000,
        MPStrategy.Flood9k => CanWeaveIn && In10y(target) && MP >= 9000,
        MPStrategy.FloodRefresh => CanWeaveIn && In10y(target) && RiskingMP,
        MPStrategy.Delay => false,
        _ => false
    };
    private bool ShouldUseBlood(BloodStrategy strategy, Actor? target) => strategy switch
    {
        BloodStrategy.Automatic => ShouldSpendBlood(BloodStrategy.Automatic, target),
        BloodStrategy.OnlyBloodspiller => ShouldSpendBlood(BloodStrategy.Automatic, target),
        BloodStrategy.OnlyQuietus => ShouldSpendBlood(BloodStrategy.Automatic, target),
        BloodStrategy.ForceBloodspiller => Unlocked(AID.Bloodspiller) && (Blood >= 50 || Delirium.IsActive),
        BloodStrategy.ForceQuietus => Unlocked(AID.Quietus) && (Blood >= 50 || Delirium.IsActive),
        BloodStrategy.Conserve => false,
        _ => false
    };
    private bool ShouldSpendBlood(BloodStrategy strategy, Actor? target) => strategy switch
    {
        BloodStrategy.Automatic =>
            Player.InCombat &&
            target != null &&
            Darkside.IsActive &&
            Unlocked(AID.Bloodspiller) &&
            Blood >= 50 &&
            (RiskingBlood || Delirium.IsActive),
        _ => false
    };
    private bool ShouldUseSaltedEarth(OGCDStrategy strategy, Actor? target) => strategy switch
    {
        OGCDStrategy.Automatic =>
            Player.InCombat &&
            target != null &&
            CanWeaveIn &&
            In3y(target) &&
            Darkside.IsActive &&
            SaltedEarth.IsReady,
        OGCDStrategy.Force => SaltedEarth.IsReady,
        OGCDStrategy.AnyWeave => SaltedEarth.IsReady && CanWeaveIn,
        OGCDStrategy.EarlyWeave => SaltedEarth.IsReady && CanEarlyWeaveIn,
        OGCDStrategy.LateWeave => SaltedEarth.IsReady && CanLateWeaveIn,
        OGCDStrategy.Delay => false,
        _ => false
    };
    private bool ShouldUseSaltAndDarkness(OGCDStrategy strategy, Actor? target) => strategy switch
    {
        OGCDStrategy.Automatic =>
            Player.InCombat &&
            target != null &&
            CanWeaveIn &&
            TotalCD(AID.SaltAndDarkness) < 0.6f &&
            SaltedEarth.IsActive,
        OGCDStrategy.Force => SaltedEarth.IsActive,
        OGCDStrategy.AnyWeave => SaltedEarth.IsActive && CanWeaveIn,
        OGCDStrategy.EarlyWeave => SaltedEarth.IsActive && CanEarlyWeaveIn,
        OGCDStrategy.LateWeave => SaltedEarth.IsActive && CanLateWeaveIn,
        OGCDStrategy.Delay => false,
        _ => false
    };
    private bool ShouldUseCarveOrDrain(CarveStrategy strategy, Actor? target) => strategy switch
    {
        CarveStrategy.Automatic => ShouldSpendCarveOrDrain(CarveStrategy.Automatic, target),
        CarveStrategy.OnlyCarve => ShouldSpendCarveOrDrain(CarveStrategy.Automatic, target),
        CarveStrategy.OnlyDrain => ShouldSpendCarveOrDrain(CarveStrategy.Automatic, target),
        CarveStrategy.ForceCarve => CarveAndSpit.IsReady,
        CarveStrategy.ForceDrain => AbyssalDrain.IsReady,
        CarveStrategy.Delay => false,
        _ => false
    };
    private bool ShouldSpendCarveOrDrain(CarveStrategy strategy, Actor? target) => strategy switch
    {
        CarveStrategy.Automatic =>
            Player.InCombat &&
            target != null &&
            CanWeaveIn &&
            In3y(target) &&
            Darkside.IsActive &&
            AbyssalDrain.IsReady,
        _ => false
    };
    private bool ShouldUseDelirium(OGCDStrategy strategy, Actor? target) => strategy switch
    {
        OGCDStrategy.Automatic =>
            Player.InCombat &&
            target != null &&
            CanWeaveIn &&
            Darkside.IsActive &&
            Blood <= 70 &&
            Delirium.IsReady,
        OGCDStrategy.Force => Delirium.IsReady,
        OGCDStrategy.AnyWeave => Delirium.IsReady && CanWeaveIn,
        OGCDStrategy.EarlyWeave => Delirium.IsReady && CanEarlyWeaveIn,
        OGCDStrategy.LateWeave => Delirium.IsReady && CanLateWeaveIn,
        OGCDStrategy.Delay => false,
        _ => false
    };
    private bool ShouldUseLivingShadow(OGCDStrategy strategy, Actor? target) => strategy switch
    {
        OGCDStrategy.Automatic =>
            Player.InCombat &&
            target != null &&
            CanWeaveIn &&
            Darkside.IsActive &&
            LivingShadow.IsReady,
        OGCDStrategy.Force => LivingShadow.IsReady,
        OGCDStrategy.AnyWeave => LivingShadow.IsReady && CanWeaveIn,
        OGCDStrategy.EarlyWeave => LivingShadow.IsReady && CanEarlyWeaveIn,
        OGCDStrategy.LateWeave => LivingShadow.IsReady && CanLateWeaveIn,
        OGCDStrategy.Delay => false,
        _ => false
    };
    private bool ShouldUseShadowbringer(OGCDStrategy strategy, Actor? target) => strategy switch
    {
        OGCDStrategy.Automatic =>
            Player.InCombat &&
            In10y(target) &&
            CanWeaveIn &&
            Darkside.IsActive &&
            Shadowbringer.IsReady &&
            LivingShadow.IsActive &&
            Delirium.IsActive,
        OGCDStrategy.Force => Shadowbringer.IsReady,
        OGCDStrategy.AnyWeave => Shadowbringer.IsReady && CanWeaveIn,
        OGCDStrategy.EarlyWeave => Shadowbringer.IsReady && CanEarlyWeaveIn,
        OGCDStrategy.LateWeave => Shadowbringer.IsReady && CanLateWeaveIn,
        OGCDStrategy.Delay => false,
        _ => false
    };
    private bool ShouldUseDeliriumCombo(DeliriumComboStrategy strategy, Actor? target) => strategy switch
    {
        DeliriumComboStrategy.Automatic
            => Player.InCombat &&
            target != null &&
            In3y(target) &&
            Unlocked(AID.ScarletDelirium) &&
            Delirium.Step is (0 or 1 or 2) &&
            Delirium.IsActive,
        DeliriumComboStrategy.ScarletDelirum => Unlocked(AID.ScarletDelirium) && Delirium.Step is 0 && Delirium.IsActive,
        DeliriumComboStrategy.Comeuppance => Unlocked(AID.Comeuppance) && Delirium.Step is 1 && Delirium.IsActive,
        DeliriumComboStrategy.Torcleaver => Unlocked(AID.Torcleaver) && Delirium.Step is 2 && Delirium.IsActive,
        DeliriumComboStrategy.Impalement => Unlocked(AID.Impalement) && Delirium.Step is 0 && Delirium.IsActive,
        DeliriumComboStrategy.Delay => false,
        _ => false
    };
    private bool ShouldUseDisesteem(GCDStrategy strategy, Actor? target) => strategy switch
    {
        GCDStrategy.Automatic =>
            Player.InCombat &&
            target != null &&
            In10y(target) &&
            Darkside.IsActive &&
            Disesteem.IsReady,
        GCDStrategy.Force => Disesteem.IsReady,
        GCDStrategy.Delay => false,
        _ => false
    };
    private bool ShouldUseUnmend(UnmendStrategy strategy, Actor? target) => strategy switch
    {
        UnmendStrategy.OpenerFar =>
            (Player.InCombat || World.Client.CountdownRemaining < 0.8f) && //Prepull or already in combat
            IsFirstGCD() && !In3y(target), //First GCD of fight and target is not in melee range
        UnmendStrategy.OpenerForce => (Player.InCombat || World.Client.CountdownRemaining < 0.8f) && IsFirstGCD(), //Prepull or already in combat and first GCD of fight
        UnmendStrategy.Force => true, //Force Unmend, regardless of any cooldowns or GCDs
        UnmendStrategy.Allow => !In3y(target), //Use Unmend if target is not in melee range
        UnmendStrategy.Forbid => false, //Do not use Unmend
        _ => false
    };
    private bool ShouldUsePotion(PotionStrategy strategy) => strategy switch
    {
        PotionStrategy.AlignWithRaidBuffs => LivingShadow.CD < 5,
        PotionStrategy.Immediate => true,
        _ => false
    };
    #endregion
}
