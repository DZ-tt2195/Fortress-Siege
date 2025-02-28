using UnityEngine;

public class Bank : EnviroCard
{
    protected override void Awake()
    {
        base.Awake();
        this.bottomType = this.GetType();
        this.coinCost = 2;
        this.extraText = "When you play a Troop here: +2 Coin.";
    }

    public override void OtherCardPlayed(Player player, Entity thisEntity, Entity playedEntity, int logged)
    {
        if (thisEntity.player == player && playedEntity is MovingTroop && thisEntity.currentRow == playedEntity.currentRow)
            player.CoinRPC(2, logged, this.name);
    }
}
