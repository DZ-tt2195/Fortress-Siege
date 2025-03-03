using UnityEngine;

public class RaZombie : TroopCard
{
    protected override void Awake()
    {
        base.Awake();
        this.bottomType = this.GetType();
        this.coinCost = 4;
        this.power = 4;
        this.health = 2;
        this.extraText = "Start of turn: The other player loses 1 Coin.";
    }

    public override int CoinEffect(Player player, Entity entity, int logged)
    {
        if (player != entity.player)
        {
            Log.inst.PreserveTextRPC($"{player.name} loses 1 Coin to {entity.name}.", logged);
            return -1;
        }
        else
        {
            return 0;
        }
    }
}
