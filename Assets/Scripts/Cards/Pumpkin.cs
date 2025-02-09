using UnityEngine;

public class Pumpkin : TroopCard
{
    protected override void Awake()
    {
        base.Awake();
        this.bottomType = this.GetType();
        this.coinCost = 1;
        this.power = 3;
        this.health = 3;
        this.abilityValue = -2;
        this.extraText = "When you play this: The other player gets +1 Card.";
        Math();
    }

    public override void DonePlaying(Player player, Entity createdEntity, int logged)
    {
        Manager.inst.OpposingPlayer(player).DrawCardRPC(1, logged);
        base.DonePlaying(player, createdEntity, logged);
    }
}
