using UnityEngine;

public class Enlarge : AuraCard
{
    protected override void Awake()
    {
        base.Awake();
        this.bottomType = this.GetType();
        this.coinCost = 5;
        this.extraText = "Your Troops here get +3 Power and +3 Health.";
    }

    public override (int, int) PassiveStats(MovingTroop troop, MovingAura enviro = null)
    {
        if (enviro.currentRow == troop.currentRow)
        {
            if (troop.player == enviro.player)
                return (3, 3);
            else
                return (0, 0);
        }
        else
        {
            return (0, 0);
        }
    }
}
