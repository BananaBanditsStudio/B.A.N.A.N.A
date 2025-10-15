using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Randomiserrrrr : MonoBehaviour
{
    public bool xRot = false, yRot = true, zRot = false;
    public float multiSize = 1;
    public float sizeVariation = 0.3f; // How much size can vary (0.3 = 30% variation)

    public void RandomiseAllChildren()
    {
        // Get all immediate children
        for (int i = 0; i < transform.childCount; i++)
        {
            Transform child = transform.GetChild(i);
            if (child != null)
            {
                SetupObject(child);
            }
        }
        
        Debug.Log("Randomized " + transform.childCount + " immediate children.");
    }

    public void SetupObject(Transform objTransform)
    {
        if (objTransform == null)
        {
            Debug.LogWarning("SetupObject: objTransform is null!");
            return;
        }

        float randomSize, randomX, randomY, randomZ;

        // Randomize size with more realistic variation
        // Base size is 1.0, variation is applied around it
        float baseSize = 1.0f * multiSize;
        float variation = Random.Range(-sizeVariation, sizeVariation);
        randomSize = baseSize + variation;
        
        // Ensure minimum size to avoid tiny trees
        randomSize = Mathf.Max(randomSize, 0.5f);

        // Randomize rotation
        randomX = 0;
        randomY = 0;
        randomZ = 0;

        if (yRot == true)
        {
            randomY = Random.Range(0, 360);
        }
        if (xRot == true)
        {
            randomX = Random.Range(0, 360);
        }
        if (zRot == true)
        {
            randomZ = Random.Range(0, 360);
        }

        // Apply scale and rotation (position stays the same)
        objTransform.localScale = new Vector3(randomSize, randomSize, randomSize);
        objTransform.localEulerAngles = new Vector3(randomX, randomY, randomZ);
    }
}