using UnityEngine;

public class Training : AuraCard
{
    protected override void Awake()
    {
        base.Awake();
        this.bottomType = this.GetType();
        this.coinCost = 3;
        this.extraText = "Start of combat: If your Troop here has more Health than Power, it gets +3 Power.";
        this.artistText = "Claus Stephan\nDominion: Allies\n(Skirmisher)";
    }

    public override void StartOfCombat(Entity entity, int logged)
    {
        MovingTroop opposingTroop = Manager.inst.FindMyTroop(entity.player, entity.currentRow);
        if (opposingTroop != null && opposingTroop.calcHealth > opposingTroop.calcPower)
            opposingTroop.ChangeStatsRPC(3, 0, logged, this.name);
    }
}
