using UnityEngine;

public class HealthBuff : EnviroCard
{
    protected override void Awake()
    {
        base.Awake();
        this.bottomType = this.GetType();
        this.coinCost = 2;
        this.abilityValue = 3;
        this.extraText = "Your troops here have +3 Health.";
        Math();
    }

    public override (int, int) PassiveStats(MovingTroop troop, Environment enviro = null)
    {
        if (enviro.currentRow == troop.currentRow && enviro.player == troop.player)
            return (0, 3);
        else
            return (0, 0);
    }
}
