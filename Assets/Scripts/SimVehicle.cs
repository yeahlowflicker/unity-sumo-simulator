using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SimVehicle : MonoBehaviour {

    // Vehicle ID
    public string VID;

    public string plateNumber;
    public bool isActiveUser;

    // Spatial information read from SUMO
    public float latitude;
    public float longitude;
    public float angle;
    public float speed;

    // Temporary spatial data used for smoother visualization
    Vector3 targetNewPosition;
    Quaternion targetNewRotation;

    // The vehicles that are within the detection box of this vehicle
    // (this will be updated in real time)
    public List<SimVehicle> currentlySeenVehicles;


    void Start() {
        currentlySeenVehicles = new List<SimVehicle>(0);
    }

    /*
    *   Update() is called every frame.
    *   It animates the movement and rotation of this vehicle.
    */
    void Update() {
        float delta = speed * (Simulator.instance.simulationSpeed) * Time.deltaTime;
        transform.position = Vector3.MoveTowards(transform.position, targetNewPosition, delta);
        transform.rotation = Quaternion.RotateTowards(transform.rotation, targetNewRotation, delta * Quaternion.Angle(transform.rotation, targetNewRotation));
    }

 
    /*
    *   Called by the Simulator script to update the SUMO data to this vehicle.
    *
    *   @param [float] latitude     - GPS latitude
    *   @param [float] longitude    - GPS longitude
    *   @param [float] angle        - Heading angle (0~360 degrees)
    *   @param [float] speed        - Speed in m/s
    *   @param [bool]  noLerp       - Should smoothing be disabled?
    */
    public void UpdateData(
        float latitude, float longitude,
        float angle, float speed,
        bool noLerp = false
    ) {

        this.latitude = latitude;
        this.longitude = longitude;
        this.angle = angle;
        this.speed = speed;

        UpdateTransformLocation(true);
    }


    /*
    *   Update the cartesian coordinates of this vehicle object
    *   based on the current latitude and longitude values
    *
    *   @param [bool] noLerp - smoothing is applied if set to false
    */
    public void UpdateTransformLocation(bool noLerp) {

        Vector3 cartesian = SimUtils.ConvertLatLonToCartesian(latitude, longitude);
        
        // Directly "teleport" to the target position, if noLerp is true
        if (noLerp) {
            transform.position = cartesian;
            transform.eulerAngles = Vector3.up * angle;
        }

        // Otherwise, update the target position and rotation
        // and let the Update() logic to animate the movement
        targetNewPosition = cartesian;
        targetNewRotation = Quaternion.Euler(Vector3.up * angle);
    }    
}
