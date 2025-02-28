using UnityEngine;

public class TrashCan : TroopCard
{
    protected override void Awake()
    {
        base.Awake();
        this.bottomType = this.GetType();
        this.coinCost = 5;
        this.power = 3;
        this.health = 3;
        this.extraText = "When you play this: It gains Shielded.";
    }

    public override void DonePlaying(Player player, Entity createdEntity, int logged)
    {
        if (createdEntity is MovingTroop troop)
            troop.ShieldStatusRPC(true, logged, this.name);
        base.DonePlaying(player, createdEntity, logged);
    }
}
