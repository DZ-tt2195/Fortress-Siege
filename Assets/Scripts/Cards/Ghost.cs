using UnityEngine;

public class Ghost : TroopCard
{
    protected override void Awake()
    {
        base.Awake();
        this.bottomType = this.GetType();
        this.coinCost = 1;
        this.power = 4;
        this.health = 3;
        this.extraText = "When you play this: The other player gets +1 Card.";
    }

    public override void DonePlaying(Player player, Entity createdEntity, int logged)
    {
        Manager.inst.OpposingPlayer(player).DrawCardRPC(1, logged, this.name);
        base.DonePlaying(player, createdEntity, logged);
    }
}
