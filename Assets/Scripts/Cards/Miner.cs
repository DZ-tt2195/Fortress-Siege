using UnityEngine;

public class Miner : TroopCard
{
    protected override void Awake()
    {
        base.Awake();
        this.bottomType = this.GetType();
        this.coinCost = 1;
        this.power = 2;
        this.health = 1;
        this.extraText = "Start of turn: +1 Coin.";
        this.artistText = "Martin Hoffmann\nDominion: Rising Sun\n(Gold Mine)";
    }

    public override int CoinEffect(Player player, Entity entity, int logged)
    {
        if (player == entity.player)
        {
            Log.inst.PreserveTextRPC($"{player.name}'s {entity.name} gives +1 Coin.", logged);
            return 1;
        }
        else
        {
            return 0;
        }
    }
}
