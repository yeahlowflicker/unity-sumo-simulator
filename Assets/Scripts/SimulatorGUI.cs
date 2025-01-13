using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class SimulatorGUI : MonoBehaviour {

    public static SimulatorGUI instance;
    
    [Header("UI Reference")]
    public TextMeshProUGUI textTimestamp;
    public TextMeshProUGUI textConfigInfo;
    public GameObject configurationMenu;
    public Button beginSimulationButton;
    public TextMeshProUGUI textActiveUserPercentage;
    public TextMeshProUGUI textEventReportInterval;
    public TMP_InputField inputXMLInputFilePath;
    public TMP_InputField outputXMLInputFilePath;



    void Awake() {
        instance = this;
    }


    public void GUI_BrowseXmlInputFile() {
        SimFileBrowser.instance.BrowseFile();
    }

    public void GUI_BrowseXmlOutputFile() {
        SimFileBrowser.instance.ShowSaveDialog();
    }

    public void GUI_BeginSimulation() {
        configurationMenu.SetActive(false);
        Simulator.instance.RunSimulation();
    }

    public void GUI_SetActiveUserPercentage(Slider slider) {
        Simulator.instance.activeUserPercentage = (int)slider.value;
        textActiveUserPercentage.text = $"{Simulator.instance.activeUserPercentage}%";
    }

    public void GUI_SetEventReportInterval(Slider slider) {
        Simulator.instance.reportIntervalSeconds = (int)slider.value;
        textEventReportInterval.text = $"{Simulator.instance.reportIntervalSeconds} s";
    }

    public void GUI_ToggleSimulateGpsError(Toggle toggle) {
        Simulator.instance.simulateGpsError = toggle.isOn;
    }
}