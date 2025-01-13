using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SimUtils {

    private static System.Random random = new System.Random();
    private const float EarthRadius = 6371000f; // Earth's radius in meters

    
    public static string GenerateLicensePlate() {
        const string letters = "ABCDEFGHIJKLMNOPQRSTUVWXYZ";
        const string numbers = "0123456789";

        // while (true) {
        char firstLetter = random.Next(0, 2) == 0 ? 'A' : 'B'; // First letter can only be 'A' or 'B'
        char secondLetter = letters[random.Next(0, letters.Length)];
        char thirdLetter = letters[random.Next(0, letters.Length)];

        string firstPart = $"{firstLetter}{secondLetter}{thirdLetter}";

        string secondPart = string.Empty;
        for (int i = 0; i < 4; i++)
            secondPart += numbers[random.Next(0, numbers.Length)];

        string licensePlate = $"{firstPart}-{secondPart}";

        // if (generatedPlateNumbers.Contains(licensePlate))
        //     return GenerateLicensePlate();
            
        return licensePlate;
        // }
    }



    public static Vector3 ConvertLatLonToCartesian(float lat, float lon) {
        float lat1 = Simulator.instance.minLat;
        float lat2 = Simulator.instance.maxLat;
        float lon1 = Simulator.instance.minLon;
        float lon2 = Simulator.instance.maxLon;

        float dlat = Mathf.Abs(lat2-lat1);      // 0.0113
        float dlon = Mathf.Abs(lon2-lon1);      // 0.0094

        float ratio = dlat / dlon;

        float percentLat = (lat - lat1) / dlat;
        float percentLon = (lon - lon1) / dlon;

        float zSize = dlat * 111320f;
        float xSize = dlon * 40075000f * Mathf.Cos((lat1+lat2)*.5f*Mathf.Deg2Rad) / 360f;

        float z = (-zSize / 2f) + zSize * percentLat;
        float x = (-xSize / 2f) + xSize * percentLon;

        Vector3 cartesian = new Vector3(x, 1f, z);
        return cartesian;
    }





    // Single function to calculate Car B's new latitude and longitude
    public static (float, float) CalculateNewPosition(
        float carALatitude, float carALongitude,
        float carAHeading, float theta
    ) {
        // Fixed distances
        float carAOffsetDistance = 2.25f; // Offset 2.25 meters ahead of Car A
        float vectorLength = 4f;          // Length of the vector (4 meters)

        // Step 1: Convert Car A's lat/lon and heading to radians
        float carALatRad = carALatitude * Mathf.Deg2Rad;
        float carALonRad = carALongitude * Mathf.Deg2Rad;
        float headingRad = carAHeading * Mathf.Deg2Rad;

        // Step 2: Calculate the offset point 2.25 meters in the direction of Car A's heading
        float offsetNorth = carAOffsetDistance * Mathf.Cos(headingRad);  // North offset in meters
        float offsetEast = carAOffsetDistance * Mathf.Sin(headingRad);   // East offset in meters

        // Calculate latitude and longitude change for the offset point
        float deltaLatOffset = offsetNorth / EarthRadius * Mathf.Rad2Deg;
        float deltaLonOffset = (offsetEast / (EarthRadius * Mathf.Cos(carALatRad))) * Mathf.Rad2Deg;

        // New origin point latitude and longitude after the 2.25 meter offset
        float originLat = carALatitude + deltaLatOffset;
        float originLon = carALongitude + deltaLonOffset;

        // Step 3: Calculate the new point for Car B (moving 4 meters at angle theta from Car A's heading)
        float totalAngleRad = (carAHeading - theta) * Mathf.Deg2Rad; // Combined angle

        // Calculate displacement in meters based on the combined angle
        float deltaNorth = vectorLength * Mathf.Cos(totalAngleRad);  // North component (4 meters forward)
        float deltaEast = vectorLength * Mathf.Sin(totalAngleRad);   // East component (4 meters forward)

        // Latitude change in degrees (northward displacement)
        float deltaLat = deltaNorth / EarthRadius * Mathf.Rad2Deg;

        // Longitude change in degrees (eastward displacement, adjusted by latitude's cosine)
        float deltaLon = (deltaEast / (EarthRadius * Mathf.Cos(originLat * Mathf.Deg2Rad))) * Mathf.Rad2Deg;

        // Final latitude and longitude for Car B
        float carBLatitude = originLat + deltaLat;
        float carBLongitude = originLon + deltaLon;

        // Return the new latitude and longitude of Car B
        return (carBLatitude, carBLongitude);
    }
}