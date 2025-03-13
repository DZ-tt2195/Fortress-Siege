using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

public class Entity : PhotonCompatible, IPointerClickHandler
{
    protected Image image;
    protected Image border;

    public Player player { get; protected set; }
    public int currentRow { get; protected set; }
    public Card myCard { get; protected set; }

    protected override void Awake()
    {
        base.Awake();
        this.bottomType = this.GetType();
        currentRow = -1;
        image = this.transform.Find("Art Box").GetComponent<Image>();
        border = this.transform.Find("border").GetComponent<Image>();
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        if (eventData.button == PointerEventData.InputButton.Right && myCard != null)
            CarryVariables.inst.RightClickDisplay(this.myCard, 1);
    }

    public virtual void MoveEntityRPC(int newPosition, int logged)
    {
    }

    public void DestroyEntityRPC(int logged)
    {
        MoveEntityRPC(-1, logged);
        myCard.WhenDestroy(this, logged + 1);
    }
}
