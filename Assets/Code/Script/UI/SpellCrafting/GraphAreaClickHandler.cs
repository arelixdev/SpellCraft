using UnityEngine.EventSystems;

/// Placé sur le fond de GraphArea.
/// Un clic dans le vide annule le câble en cours.
public class GraphAreaClickHandler : UnityEngine.MonoBehaviour, IPointerDownHandler
{
    public void OnPointerDown(PointerEventData e)
    {
        PendingConnectionController.Instance?.Cancel();
    }
}
