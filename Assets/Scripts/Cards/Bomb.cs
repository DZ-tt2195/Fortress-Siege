using UnityEngine;

public class Bomb : TroopCard
{
    protected override void Awake()
    {
        base.Awake();
        this.bottomType = this.GetType();
        this.coinCost = 3;
        this.power = 6;
        this.health = 1;
        this.extraText = "End of turn: This loses 1 Health.";
    }

    public override void EndOfTurn(Entity entity, int logged)
    {
        MovingTroop troop = (MovingTroop)entity;
        troop.ChangeStatsRPC(0, -1, logged, this.name);
    }
}
