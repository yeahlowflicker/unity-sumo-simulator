using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using System.Xml;
using System.Xml.Linq;

public class Simulator : MonoBehaviour {

    public static Simulator instance;

    [Header("Object Reference")]
    public GameObject carObjectPrefab;

    [Header("Sim Configuration")]
    public bool saveXMLEverySecond = false;
    public bool simulateGpsError = true;
    [Range(0, 100)] public int simulationSpeed = 100;
    [Range(0, 100)] public int activeUserPercentage = 100;
    public int frameToSkipAtStart = 0;
    public int reportIntervalSeconds = 3;
    public int phoneCount = 100;
    public int endTime = 400;
    public int maxVehicleCount = 500;



    [Header("Runtime Variables")]
    public long currentTimestamp;
    int reportCooldownCounter = 0;
    public float minLat = 999f, minLon = 999f;
    public float maxLat = -999f, maxLon = -999f;
    List<string> generatedPlateNumbers = new List<string>(0);
    [SerializeField] public Dictionary<string, SimVehicle> vehicleData = new Dictionary<string, SimVehicle>();
    public List<SimReportDevice> reportDevices = new List<SimReportDevice>(0);


    [Header("XML")]
    XmlTextReader xmlReader;

    public string xmlInputFilePath = "assets/sumoTrace.xml";
    string xmlOutputFilePath = "assets/Scripts/SumoExporter/output/output.xml";
    XDocument xmlOutputDoc;
    XElement xmlOutputRootElement;
    XElement xmlSimConfigElement = new XElement("sim-configuration");
    XElement xmlFactsListElement = new XElement("facts");
    XElement xmlSimulationElement;
    XElement xmlTimestepElement;
    XElement xmlVehicleTrueLocationListElement;
    XElement xmlUserLocationReportListElement;
    XElement xmlPlateRecognitionReportListElement;


    void Awake() {
        instance = this;
    }

    // Start is called before the first frame update
    void Start() {
        SimulatorGUI.instance.beginSimulationButton.interactable = false;
    }


    /**
    *   Update the SUMO XML input path.
    *   This is called by SimFileBrowser.
    */
    public void OnXMLInputFilePathSelected(string path) {
        SimulatorGUI.instance.beginSimulationButton.interactable = path.Length > 0 && path.EndsWith(".xml");

        xmlInputFilePath = path;
        SimulatorGUI.instance.inputXMLInputFilePath.text = path;
    }

    /**
    *   Update the simulation output path.
    *   This is called by SimFileBrowser.
    */
    public void OnXMLOutputPathSelected(string path) {
        xmlOutputFilePath = path;
        SimulatorGUI.instance.outputXMLInputFilePath.text = path;
    }


    /**
    *   The entry point of a new simulation.
    */
    public void RunSimulation() {
        SimFileBrowser.CreateXMLFileIfNotExists(xmlOutputFilePath);

        xmlReader = new XmlTextReader(xmlInputFilePath);

        xmlOutputDoc = XDocument.Load(xmlOutputFilePath);
        xmlOutputRootElement = xmlOutputDoc.Root;
        xmlOutputRootElement.RemoveAll();

        xmlSimConfigElement = new XElement("sim-configuration");
        currentTimestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        xmlSimConfigElement.Add(new XAttribute("startTime", currentTimestamp));
        xmlSimConfigElement.Add(new XAttribute("reportIntervalInSeconds", reportIntervalSeconds));
        xmlSimConfigElement.Add(new XAttribute("simulateGPSError", simulateGpsError));

        xmlFactsListElement = new XElement("pv-binding-facts");
        xmlSimulationElement = new XElement("simulation");
        xmlOutputRootElement.Add(xmlSimConfigElement);
        xmlOutputRootElement.Add(xmlFactsListElement);
        xmlOutputRootElement.Add(xmlSimulationElement);

        SimulatorGUI.instance.textTimestamp.text = $"Time: {(int)(currentTimestamp)}";

        SimulatorGUI.instance.textConfigInfo.text = $"Active Users: {activeUserPercentage}%\n";
        SimulatorGUI.instance.textConfigInfo.text += $"Event Report Interval: {reportIntervalSeconds} s\n";
        SimulatorGUI.instance.textConfigInfo.text += $"Simulate GPS Error: {simulateGpsError}\n";

        PreprocessXML(xmlInputFilePath);
        StartCoroutine(ReadXml(xmlInputFilePath));
    }


    /*
    *   Traverse the entire SUMO XML file before the actual simulation
    *   to extract the time information and calculate world boundaries.
    *
    *   @param [string] xmlInputFilePath
    */
    void PreprocessXML(string xmlInputFilePath) {
        xmlReader = new XmlTextReader(xmlInputFilePath);

        while (xmlReader.Read()) {
            if (xmlReader.NodeType != XmlNodeType.Element)
                continue;

            // Terminate, if time exceeds the desired end time
            if (xmlReader.Name == "timestep") {
                if (float.Parse(xmlReader.GetAttribute("time")) >= endTime)
                    break;
            }

            // Compare and upate world boundary values
            if (xmlReader.Name == "vehicle") {
                float latitude = float.Parse(xmlReader.GetAttribute("y"));
                float longitude = float.Parse(xmlReader.GetAttribute("x"));

                if (latitude < minLat)
                    minLat = latitude;

                if (latitude > maxLat)
                    maxLat = latitude;

                if (longitude < minLon)
                    minLon = longitude;

                if (longitude > maxLon)
                    maxLon = longitude;
            }
        }

        Debug.Log($"World boundary: ({minLat}, {minLon})  to  ({maxLat}, {maxLon})");
    }



    /*
    *   The main simulation coroutine.
    *   This generates and updates vehicles in real time, according to
    *
    *   @param [string] xmlInputFilePath
    */
    IEnumerator ReadXml(string xmlInputFilePath) {

        xmlReader = new XmlTextReader(xmlInputFilePath);
    
        // Begin reading XML data
        while (xmlReader.Read()) {

            if (xmlReader.NodeType != XmlNodeType.Element)
                continue;


            // Update current simulation time
            if (xmlReader.Name == "timestep") {

                float t = float.Parse(xmlReader.GetAttribute("time"));

                // Allow skip frames at the beginning
                if (t < frameToSkipAtStart)
                    continue;

                // Early-stopping
                if (t >= endTime)
                    break;

                // Add time delay between each sim moment (based on simulation speed)
                yield return new WaitForSeconds(simulationSpeed == 0 ? 1f : 1f / simulationSpeed);

                // Initialize output XML elements for this particular moment
                xmlTimestepElement = new XElement("timestep");
                xmlTimestepElement.Add(new XAttribute("timeSeconds", currentTimestamp));

                xmlVehicleTrueLocationListElement = new XElement("vehicle-true-locations");
                xmlUserLocationReportListElement = new XElement("user-location-reports");
                xmlPlateRecognitionReportListElement = new XElement("plate-recognition-reports");
                xmlTimestepElement.Add(xmlVehicleTrueLocationListElement);
                xmlTimestepElement.Add(xmlUserLocationReportListElement);
                xmlTimestepElement.Add(xmlPlateRecognitionReportListElement);

                xmlSimulationElement.Add(xmlTimestepElement);

                // Write to file if saveXMLEverySecond is enabled
                if(saveXMLEverySecond)
                    xmlOutputDoc.Save(xmlOutputFilePath);

                currentTimestamp += 1;
                SimulatorGUI.instance.textTimestamp.text = $"Time: {(int)(currentTimestamp)}";

                reportCooldownCounter++;

                // if (reportCooldownCounter % changeVehicleIntervalSeconds == 0) {
                //     foreach(SimReportDevice reportDevice in reportDevices)
                //         reportDevice.ChangeVehicle();
                // }


                // Check if reportDevices should report
                // If yes, make them report
                if (reportCooldownCounter % reportIntervalSeconds == 0) {
                    foreach(SimReportDevice reportDevice in reportDevices)
                        reportDevice.Report();
                }

                // Disable all vehicle objects
                // This is done because SUMO cars can disappear at any time
                // so disabling them prevents leftovers 
                foreach (KeyValuePair<string, SimVehicle> entry in vehicleData)
                    (entry.Value as SimVehicle).gameObject.active = false;
            }

            // Process vehicles within the same time moment
            else if (xmlReader.Name == "vehicle") {

                // Extract raw vehicle ID from SUMO
                string rawId = xmlReader.GetAttribute("id");
                string VID = rawId.Replace("veh", "v__");

                // Extract coordinates, heading and speed from SUMO
                float latitude = float.Parse(xmlReader.GetAttribute("y"));
                float longitude = float.Parse(xmlReader.GetAttribute("x"));
                float angle = float.Parse(xmlReader.GetAttribute("angle"));
                float speed = float.Parse(xmlReader.GetAttribute("speed"));

                // Initialize a sim vehicle instance
                SimVehicle veh = null;
                
                // If the vehicle is already created in the sim
                // then simply refernce it and update its data
                if (vehicleData.ContainsKey(VID)) {
                    veh = (vehicleData[VID] as SimVehicle);
                    veh.gameObject.active = true;
                    veh.UpdateData(latitude, longitude, angle, speed);
                }

                // Otherwise, create a new vehicle in the sim
                // and set its data
                else if (vehicleData.Count <= maxVehicleCount) {

                    // Instantiate the vehicle prefab
                    GameObject vehObj = Instantiate(carObjectPrefab, Vector3.zero, Quaternion.Euler(Vector3.zero));
                    vehObj.transform.SetParent(transform);


                    // Generate a random license plate
                    string plateNumber = SimUtils.GenerateLicensePlate();

                    // Regenerate the plate number if it is repeated
                    while (generatedPlateNumbers.Contains(plateNumber))
                        plateNumber = SimUtils.GenerateLicensePlate();

                    // Update name in hierarchy tab
                    vehObj.name = $"{VID} [{plateNumber}]";

                    // Reference the SimVehicle component in the newly-generated vehicle
                    veh = vehObj.GetComponent<SimVehicle>();

                    // Apply data to the vehicle object
                    veh.VID = VID;
                    veh.plateNumber = plateNumber;
                    // veh.isActiveUser = IsUserActiveRandom();
                    veh.UpdateData(latitude, longitude, angle, speed, true);

                    // Store the vehicle reference in a list
                    // so it won't get recreated
                    vehicleData.Add(VID, veh);


                    // Record vehicle metadata to output XML
                    XElement xelem = new XElement("veh");
                    xelem.Add(new XAttribute("VID", VID));
                    xelem.Add(new XAttribute("plate", plateNumber));
                    xmlFactsListElement.Add(xelem);


                    if (vehicleData.Count % 2 == 0) {
                        SimReportDevice reportDevice = new SimReportDevice();
                        reportDevice.UID = VID.Replace("v__", "u__");
                        reportDevice.attachedVehicle = veh;
                        reportDevices.Add(reportDevice);
                    }
                }

                if (veh)
                    RecordVehicleTrueLocation(veh);
            }
        }

        xmlReader.Close();

        Debug.Log("Simulation complete.");

        // Add additional sim config info
        xmlSimConfigElement.Add(new XAttribute("activeUserPercentage", reportDevices.Count / vehicleData.Count));
        xmlSimConfigElement.Add(new XAttribute("activeUserCount", reportDevices.Count));
        xmlSimConfigElement.Add(new XAttribute("vehicleCount", vehicleData.Count));

        // Open file save dialog
        xmlOutputDoc.Save(xmlOutputFilePath);


        // Show the main menu
        SimulatorGUI.instance.configurationMenu.SetActive(true);

        // Clear leftovers
        CleanUpSimulation();

        yield return null;
    }



    /*
    *   Save the true vehicle data (facts) to the output XML.
    *   This is called by the ReadXML() main sim sequence.
    *
    *   @param [SimVehicle] veh - The target vehicle
    */
    public void RecordVehicleTrueLocation(SimVehicle veh) {
        XElement xelem = new XElement("report");
        xelem.Add(new XAttribute("VID", veh.VID));
        xelem.Add(new XAttribute("lat", veh.latitude));
        xelem.Add(new XAttribute("lon", veh.longitude));
        xelem.Add(new XAttribute("speed_ms", veh.speed));
        xelem.Add(new XAttribute("heading", veh.angle));

        xmlVehicleTrueLocationListElement.Add(xelem);
    }



    /*
    *   Save a GPS info report in the output XML.
    *   This is called by SimReportDevice.
    *
    *   @param [string] UID         - User ID of the report device
    *   @param [float]  latitude    - Latitude of the report device's attached vehicle
    *   @param [float]  longitude   - Longitude of the report device's attached vehicle
    *   @param [float]  speed_ms    - Speed of the report device's attached vehicle
    *   @param [float]  heading     - Heading of the report device's attached vehicle
    *   @param [string] attachedVID - Vehicle ID of the report device's attached vehicle
    */
    public void MakeUserLocationReport(
        string UID,
        float latitude, float longitude,
        float speed_ms, float heading,
        string attachedVID
    ) {

        // Add random values to lat-lon to simulate GPS precision error
        if (simulateGpsError) {
            latitude += UnityEngine.Random.Range(-0.00009f, 0.00009f) * .5f;
            longitude += UnityEngine.Random.Range(-0.000082219f, 0.000082219f) * .5f;
        }

        XElement xelem = new XElement("report");
        xelem.Add(new XAttribute("UID", UID));
        xelem.Add(new XAttribute("lat", latitude));
        xelem.Add(new XAttribute("lon", longitude));
        xelem.Add(new XAttribute("speed", speed_ms));
        xelem.Add(new XAttribute("heading", heading));
        xelem.Add(new XAttribute("attachedVID", attachedVID));

        xmlUserLocationReportListElement.Add(xelem);
    }



    /*
    *   Save a plate recognition report to the output XML.
    *   This is called by SimReportDevice.
    *
    *   @param [string] UID         - User ID of the report device
    *   @param [float]  latitude    - Estimated latitude of the seen vehicle
    *   @param [float]  longitude   - Estimated latitude of the seen vehicle
    *   @param [float]  speed_ms    - Speed of the report device's attached vehicle
    *   @param [float]  heading     - Heading of the report device's attached vehicle
    *   @param [string] plateNumber - Plate number of the seen vehicle
    *   @param [string] attachedVID - Vehicle ID of the report device's attached vehicle
    */
    public void MakePlateRecognitionReport(
        string UID,
        float latitude, float longitude,
        float speed_ms, float heading,
        string plateNumber, string attachedVID
    ) {

        // Add random values to lat-lon to simulate GPS precision error
        if (simulateGpsError) {
            latitude += UnityEngine.Random.Range(-0.00009f, 0.00009f) * .5f;
            longitude += UnityEngine.Random.Range(-0.000082219f, 0.000082219f) * .5f;
        }

        XElement xelem = new XElement("report");
        xelem.Add(new XAttribute("reporterUID", UID));
        xelem.Add(new XAttribute("lat", latitude));
        xelem.Add(new XAttribute("lon", longitude));
        xelem.Add(new XAttribute("speed", speed_ms));
        xelem.Add(new XAttribute("heading", heading));
        xelem.Add(new XAttribute("plateNumberSeen", plateNumber));
        xelem.Add(new XAttribute("attachedVID", attachedVID));

        xmlPlateRecognitionReportListElement.Add(xelem);
    }



    /*
    *   Removes leftover cars and data after each simulation.
    */
    void CleanUpSimulation() {
        foreach (KeyValuePair<string, SimVehicle> entry in vehicleData)
            Destroy((entry.Value as SimVehicle).gameObject);

        vehicleData.Clear();
        reportDevices.Clear();
    }
}
