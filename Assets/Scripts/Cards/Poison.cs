using UnityEngine;

public class Poison : AuraCard
{
    protected override void Awake()
    {
        base.Awake();
        this.bottomType = this.GetType();
        this.coinCost = 1;
        this.extraText = "Start of combat: The opposing Troop here loses 1 Health.";
        this.artistText = "Julia Metzger\nMTG: Murders at Karlov Manor\n(Pick Your Poison)";
    }

    public override void StartOfCombat(Entity entity, int logged)
    {
        MovingTroop opposingTroop = Manager.inst.FindOpposingTroop(entity.player, entity.currentRow);
        if (opposingTroop != null)
            opposingTroop.ChangeStatsRPC(0, -1, logged, this.name);
    }
}
