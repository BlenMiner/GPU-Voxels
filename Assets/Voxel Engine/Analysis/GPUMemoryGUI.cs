using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GPUMemoryGUI : MonoBehaviour
{
    static readonly string[] SizeSuffixes = 
                  { "bytes", "KB", "MB", "GB", "TB", "PB", "EB", "ZB", "YB" };

    static string SizeSuffix(Int64 value, int decimalPlaces = 1)
    {
        if (value < 0) { return "-" + SizeSuffix(-value, decimalPlaces); } 

        int i = 0;
        decimal dValue = (decimal)value;
        while (Math.Round(dValue, decimalPlaces) >= 1000)
        {
            dValue /= 1024;
            i++;
        }

        return string.Format("{0:n" + decimalPlaces + "} {1}", dValue, SizeSuffixes[i]);
    }

    private void OnGUI()
    {
        GUI.color = Color.black;
        GUILayout.Label($"GPU Memory: {SizeSuffix(GPUMemoryTracker.Count)}");
    }
}
