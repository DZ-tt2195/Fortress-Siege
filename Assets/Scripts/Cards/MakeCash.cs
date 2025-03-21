using System.Collections.Generic;
using UnityEngine;

public class MakeCash : TroopCard
{
    protected override void Awake()
    {
        base.Awake();
        this.bottomType = this.GetType();
        this.coinCost = 3;
        this.power = 3;
        this.health = 1;
        this.extraText = "When you play this: +2 Coin.";
        this.artistText = "";
    }

    public override void DonePlaying(Player player, Entity createdEntity, int logged)
    {
        player.CoinRPC(2, logged);
        base.DonePlaying(player, createdEntity, logged);
    }
}
