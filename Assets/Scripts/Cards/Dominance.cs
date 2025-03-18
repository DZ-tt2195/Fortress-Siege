using UnityEngine;

public class Dominance : AuraCard
{
    protected override void Awake()
    {
        base.Awake();
        this.bottomType = this.GetType();
        this.coinCost = 4;
        this.extraText = "Your Troops here get +1 Power and +1 Health. Opposing Troops here get -1 Power and -1 Health.";
        this.artistText = "Claus Stephan\nDominion: Rising Sun\n(Practice)";
    }

    public override (int, int) PassiveStats(MovingTroop troop, MovingAura enviro = null)
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
