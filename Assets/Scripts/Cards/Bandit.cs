using UnityEngine;

public class Bandit : TroopCard
{
    protected override void Awake()
    {
        base.Awake();
        this.bottomType = this.GetType();
        this.coinCost = 5;
        this.power = 4;
        this.health = 4;
        this.extraText = "Start of turn: The other player loses 1 Coin.";
        this.artistText = "Hans Krill\nDominion: Plunder\n(Cutthroat)";
    }

    public override int CoinEffect(Player player, Entity entity, int logged)
    {
        if (player != entity.player)
        {
            Log.inst.AddTextRPC($"{player.name} loses 1 Coin ({entity.name}.)", LogAdd.Remember, logged);
            return -1;
        }
        else
        {
            return 0;
        }
    }
}
