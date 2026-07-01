using System.Collections.Generic;
using System.Linq;
using UnityEngine;

internal static class CanvasGroupExtensions
{
    public static void ActivatePanel(this CanvasGroup panel, bool activate)
    {
        panel.alpha = (activate) ? 1 : 0;
        panel.interactable = activate;
        panel.blocksRaycasts = activate;
    }
}

public class PanelManager : MonoBehaviour
{
    [SerializeField]
    private bool activateFirstPanel = true;

    private List<CanvasGroup> _panels;

    private void Start()
    {
        _panels = GetComponentsInChildren<CanvasGroup>().ToList();
        _panels.ForEach(panel => panel.ActivatePanel(false));

        if (activateFirstPanel && _panels.Count > 0)
            _panels[0].ActivatePanel(true);
    }

    public void ActivatePanelByName(string panelName)
    {
        _panels.ForEach(panel => panel.ActivatePanel(panel.gameObject.name == panelName));
    }
}
