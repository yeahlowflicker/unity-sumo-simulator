using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class SimReportDevice
{
    // User ID
    public string UID;

    // Which vehicle is this report device currently attached to?
    public SimVehicle attachedVehicle;


    /*
    *   Simulate the change of vehicles.
    *   This randomly selects a new VID and assigns this report device to it.
    */
    public void ChangeVehicle() {
        if (Simulator.instance.vehicleData.Count == 0)
            return;

        string newRandomVID = $"v__{UnityEngine.Random.Range(1, Simulator.instance.vehicleData.Count)}";

        if (!Simulator.instance.vehicleData.ContainsKey(newRandomVID))
            return;

        attachedVehicle = Simulator.instance.vehicleData[newRandomVID];
    }
    


    /*
    *   Periodic report the GPS info and the vehicles seen by this report device.
    *   This is called by the Simulator scripts globally every X seconds.
    */
    public void Report() {

        // Don't report, if this report device is not attached to any active vehicles
        if (!attachedVehicle || !attachedVehicle.gameObject.activeInHierarchy)
            return;


        // Report the GPS info of the attached vehicle
        Simulator.instance.MakeUserLocationReport(
            UID, attachedVehicle.latitude, attachedVehicle.longitude,
            attachedVehicle.speed, attachedVehicle.angle, attachedVehicle.VID
        );


        // Then, skip if no vehicles have been seen
        if (attachedVehicle.currentlySeenVehicles.Count == 0)
            return;


        // If this report device sees any other vehicles,
        // estimate their GPS coordinates and then make a detection report
        foreach (SimVehicle seenVeh in attachedVehicle.currentlySeenVehicles) {

            // Calculate the angle offset
            Vector3 angleOffsetVector = seenVeh.transform.position - attachedVehicle.transform.position;
            float angleOffset = Vector3.SignedAngle(angleOffsetVector, attachedVehicle.transform.forward, Vector3.up);

            // Calculate the estimated GPS coordinate of the other vehicle
            (float newLat, float newLon) = SimUtils.CalculateNewPosition(attachedVehicle.latitude, attachedVehicle.longitude, attachedVehicle.angle, angleOffset);

            // Make a report
            Simulator.instance.MakePlateRecognitionReport(
                UID, newLat, newLon,
                attachedVehicle.speed, attachedVehicle.angle,
                seenVeh.plateNumber, attachedVehicle.VID
            );
        }


        // Clear the seen vehicles list of the attached vehicle
        attachedVehicle.currentlySeenVehicles.Clear();
    }
}
