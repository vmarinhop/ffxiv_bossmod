namespace BossMod.Autorotation;

public sealed class StandardMCH(RotationModuleManager manager, Actor player) : RotationModule(manager, player)
{
    public static RotationModuleDefinition Definition()
    {
        var res = new RotationModuleDefinition(
            "Standard MCH",
            "Description",
            "Author",
            RotationModuleQuality.WIP,
            BitMask.Build((int)Class.MCH),
            100);

        return res;
    }

    public enum GCDPriority
    {
        None = 0,

        DelayCombo = 350,
        FlexibleCombo = 400,
        ForcedCombo = 880
    };

    private MCH.AID PrevCombo => (MCH.AID)World.Client.ComboState.Action;

    public override void Execute(
        StrategyValues strategy,
        Actor? primaryTarget,
        float estimatedAnimLockDelay,
        float forceMovementIn,
        bool isMoving)
    {
        QueueGCD(NextComboSingleTarget(), primaryTarget, GCDPriority.FlexibleCombo);
    }

    private MCH.AID NextComboSingleTarget() => PrevCombo switch
    {
        MCH.AID.SlugShot => MCH.AID.CleanShot,
        MCH.AID.SplitShot => MCH.AID.SlugShot,
        _ => MCH.AID.SplitShot
    };

    private void QueueGCD(MCH.AID aid, Actor? target, GCDPriority prio)
    {
        if (prio != GCDPriority.None)
        {
            Hints.ActionsToExecute.Push(ActionID.MakeSpell(aid), target, ActionQueue.Priority.High + (int)prio);
        }
    }
}
