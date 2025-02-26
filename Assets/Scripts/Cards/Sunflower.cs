using UnityEngine;

public class Sunflower : TroopCard
{
    protected override void Awake()
    {
        base.Awake();
        this.bottomType = this.GetType();
        this.coinCost = 1;
        this.power = 1;
        this.health = 2;
        this.abilityValue = 1;
        this.extraText = "Start of turn: You get +1 Coin.";
        Math();
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
