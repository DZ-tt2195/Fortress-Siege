using UnityEngine;

public class ChargeUp : TroopCard
{
    protected override void Awake()
    {
        base.Awake();
        this.bottomType = this.GetType();
        this.coinCost = 1;
        this.power = 0;
        this.health = 4;
        this.extraText = "Start of combat: This gets +2 Power per other Troop you have. End of turn: This loses all its Power.";
        this.artistText = "";
    }

    public override void StartOfCombat(Entity entity, int logged)
    {
        MovingTroop troop = (MovingTroop)entity;
        troop.ChangeStatsRPC(2 * (entity.player.FilterRows(true).Count - 1), 0, logged, this.name);
    }

    public override void EndOfTurn(Entity entity, int logged)
    {
        MovingTroop troop = (MovingTroop)entity;
        troop.ChangeStatsRPC(-1 * troop.calcPower, 0, logged, this.name);
    }
}
