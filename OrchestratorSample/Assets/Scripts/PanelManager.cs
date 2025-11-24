using System.Collections.Generic;
using UnityEngine;

public class PanelManager : MonoBehaviour
{
    public List<CanvasGroup> panels;

    private void Start()
    {
        foreach (var panel in panels)
        {
            TogglePanel(panel, false);
            Debug.Log("Toggling: " + panel.name);
        }

        if (panels.Count > 0) TogglePanel(panels[0], true);
    }

    public void ActivatePanelByName(string panelName)
    {
        foreach (var panel in panels)
        {
            TogglePanel(panel, panel.gameObject.name == panelName);
        }
    }

    private void TogglePanel(CanvasGroup panel, bool active)
    {
        if (active)
        {
            panel.alpha = 1;
            panel.interactable = true;
            panel.blocksRaycasts = true;
        }
        else
        {
            panel.alpha = 0;
            panel.interactable = false;
            panel.blocksRaycasts = false;
        }
    }
}
