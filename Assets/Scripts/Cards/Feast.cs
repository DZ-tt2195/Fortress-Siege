using UnityEngine;

public class Feast : AuraCard
{
    protected override void Awake()
    {
        base.Awake();
        this.bottomType = this.GetType();
        this.coinCost = 2;
        this.extraText = "Start of combat: If your Troop here has more Health than Power, it gets +2 Power.";
        this.artistText = "";
    }

    public override void StartOfCombat(Entity entity, int logged)
    {
        MovingTroop opposingTroop = Manager.inst.FindMyTroop(entity.player, entity.currentRow);
        if (opposingTroop != null && opposingTroop.calcHealth > opposingTroop.calcPower)
            opposingTroop.ChangeStatsRPC(2, 0, logged, this.name);
    }
}
