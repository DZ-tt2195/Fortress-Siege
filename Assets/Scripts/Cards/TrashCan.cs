using UnityEngine;

public class TrashCan : TroopCard
{
    protected override void Awake()
    {
        base.Awake();
        this.bottomType = this.GetType();
        this.coinCost = 3;
        this.power = 2;
        this.health = 3;
        this.abilityValue = 3;
        this.extraText = "When you play this: It gains Shielded.";
        Math();
    }

    public override void DonePlaying(Player player, Entity createdEntity, int logged)
    {
        if (createdEntity is MovingTroop troop)
            troop.ShieldStatusRPC(true, logged);
        base.DonePlaying(player, createdEntity, logged);
    }
}
