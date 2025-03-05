using UnityEngine;

public class Targeted : AuraCard
{
    protected override void Awake()
    {
        base.Awake();
        this.bottomType = this.GetType();
        this.coinCost = 1;
        this.extraText = "Start of combat: Deal 1 damage to the opposing Troop here.";
    }

    public override void StartOfCombat(Entity entity, int logged)
    {
        MovingTroop opposingTroop = Manager.inst.FindOpposingTroop(entity.player, entity.currentRow);
        if (opposingTroop != null)
            opposingTroop.ChangeStatsRPC(0, -1, logged, this.name);
    }
}
