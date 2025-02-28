using UnityEngine;

public class DrainWeak : EnviroCard
{
    protected override void Awake()
    {
        base.Awake();
        this.bottomType = this.GetType();
        this.coinCost = 2;
        this.extraText = "Opposing Troops here with 2 Power or less get -2 Health.";
    }

    public override (int, int) PassiveStats(MovingTroop troop, Environment enviro = null)
    {
        if (enviro.currentRow == troop.currentRow && troop.player != enviro.player && troop.calcPower <= 2)
            return (0, -2);
        else
            return (0, 0);
    }
}
