using System.Collections;
using System.Collections.Generic;
using UnityEngine;


/*
*   This class handles the detection of vehicles using PhysX's collision system.
*   The script is attached to every single vehicle and operates every frame.
*/
public class SimVehicleDetector : MonoBehaviour
{
    public SimVehicle attachedVehicle;

    /*
    *   This is called whenever there is a vehicle within the detection box.
    */
    void OnTriggerStay(Collider col) {

        // Don't process, if the other object is not a car
        if (!col.transform.CompareTag("car"))
            return;

        // Don't process, if the vehicle is not parallel to the direction of travel
        if (Vector3.Dot(attachedVehicle.transform.forward, col.transform.forward) < 0.5f)
            return;

        // Get the SimVehicle script reference from the seen vehicle
        SimVehicle seenVeh = col.transform.parent.GetComponent<SimVehicle>();

        // Store the seen vehicle in the currentlySeenVehicles list of this vehicle
        if (!attachedVehicle.currentlySeenVehicles.Contains(seenVeh))
            attachedVehicle.currentlySeenVehicles.Add(seenVeh);
    }


    /*
    *   This is called whenever a vehicle leaves the detection box.
    *   Removes the leaving vehicle from the currentlySeenVehicles list of this vehicle.
    */
    void OnTriggerExit(Collider col) {
        SimVehicle exitVeh = col.transform.parent.GetComponent<SimVehicle>();

        attachedVehicle.currentlySeenVehicles.Remove(exitVeh);
    }
}
