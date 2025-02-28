using UnityEngine;

public class Battlefield : EnviroCard
{
    protected override void Awake()
    {
        base.Awake();
        this.bottomType = this.GetType();
        this.coinCost = 4;
        this.extraText = "Your Troops here get +1 Power +1 Health. Opposing Troops here get -1 Power -1 Health.";
    }

    public override (int, int) PassiveStats(MovingTroop troop, Environment enviro = null)
    {
        if (enviro.currentRow == troop.currentRow)
        {
            if (troop.player == enviro.player)
                return (1, 1);
            else
                return (-1, -1);
        }
        else
        {
            return (0, 0);
        }
    }
}
