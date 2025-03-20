using UnityEngine;

public class FlagBearer : TroopCard
{
    protected override void Awake()
    {
        base.Awake();
        this.bottomType = this.GetType();
        this.coinCost = 3;
        this.power = 1;
        this.health = 4;
        this.extraText = "Start of combat: All Troops get +1 Power.";
    }

    public override void StartOfCombat(Entity entity, int logged)
    {
        foreach (Row row in Manager.inst.allRows)
        {
            foreach (MovingTroop troop in row.playerTroops)
            {
                if (troop != null)
                    troop.ChangeStatsRPC(1, 0, logged, this.name);
            }
        }
    }
}
