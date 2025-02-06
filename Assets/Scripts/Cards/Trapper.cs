using UnityEngine;

public class Trapper : EnviroCard
{
    protected override void Awake()
    {
        base.Awake();
        this.bottomType = this.GetType();
        this.coinCost = 1;
        this.abilityValue = 1;
        this.extraText = "Start of combat: Deal 1 damage to the opposing troop here.";
        Math();
    }

    public override void StartOfCombat(Entity entity, int logged)
    {
        MovingTroop opposingTroop = Manager.inst.FindOpposingTroop(entity.player, entity.currentRow);
        if (opposingTroop != null)
            Log.inst.RememberStep(opposingTroop, StepType.Revert, () => opposingTroop.ChangeHealthRPC(-1, logged));
    }
}
