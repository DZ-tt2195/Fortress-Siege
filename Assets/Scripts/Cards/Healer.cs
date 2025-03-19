using UnityEngine;

public class Healer : TroopCard
{
    protected override void Awake()
    {
        base.Awake();
        this.bottomType = this.GetType();
        this.coinCost = 3;
        this.power = 2;
        this.health = 1;
        this.extraText = "When you play this: All your Troops gets +1 Health (including this).";
        this.artistText = "";
    }

    public override void DonePlaying(Player player, Entity createdEntity, int logged)
    {
        foreach (Row row in Manager.inst.allRows)
        {
            MovingTroop troop = Manager.inst.FindMyTroop(player, row.position);
            if (troop != null)
                troop.ChangeStatsRPC(0, 1, logged);
        }
        base.DonePlaying(player, createdEntity, logged);
    }
}
