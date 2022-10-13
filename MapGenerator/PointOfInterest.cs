using System.Collections.Generic;
using UnityEngine;

public class PointOfInterest : MonoBehaviour
{
    public List<PointOfInterest> NextPointsOfInterest { get; set; } = new();
}