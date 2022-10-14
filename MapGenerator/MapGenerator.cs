using System.Collections.Generic;
using UnityEngine;

public class MapGenerator : MonoBehaviour
{
    [SerializeField] private Transform boardContainer;
    [SerializeField] private List<PointOfInterest> pointsOfInterestPrefabs;
    [SerializeField] private GameObject pathPrefab;
    [SerializeField] private int numberOfStartingPoints = 4;
    [SerializeField] private int mapLength = 10;
    [SerializeField] private int maxWidth = 5;
    [SerializeField] private float xMaxSize;
    [SerializeField] private float yPadding;
    [SerializeField] private bool allowCrisscrossing;
    [Range(0.1f, 1f), SerializeField] private float chancePathMiddle;
    [Range(0f, 1f), SerializeField] private float chancePathSide;
    [SerializeField, Range(0.9f, 5f)] private float multiplicativeSpaceBetweenLines = 2.5f;

    private PointOfInterest[][] _pointOfInterestsPerFloor;
    private readonly List<PointOfInterest> pointsOfInterest = new();
    private int _numberOfConnections = 0;
    private float _lineLength;
    private float _lineHeight;

    private void Start()
    {
        RecreateBoard();
    }

    public void RecreateBoard()
    {
        _lineLength = pathPrefab.GetComponent<MeshFilter>().sharedMesh.bounds.size.z * pathPrefab.transform.localScale.z;
        _lineHeight = pathPrefab.GetComponent<MeshFilter>().sharedMesh.bounds.size.y * pathPrefab.transform.localScale.y;
        DestroyImmediateAllChildren(boardContainer);
        _numberOfConnections = 0;
        GenerateRandomSeed();
        pointsOfInterest.Clear();
        _pointOfInterestsPerFloor = new PointOfInterest[mapLength][];
        for (int i = 0; i < _pointOfInterestsPerFloor.Length; i++)
        {
            _pointOfInterestsPerFloor[i] = new PointOfInterest[maxWidth];
        }
        CreateMap();
    }

    private void GenerateRandomSeed()
    {
        int tempSeed = (int)System.DateTime.Now.Ticks;
        Random.InitState(tempSeed);
    }

    private PointOfInterest InstantiatePointOfInterest(int floorN, int xNum)
    {
        if (_pointOfInterestsPerFloor[floorN][xNum] != null)
        {
            return _pointOfInterestsPerFloor[floorN][xNum];
        }

        float xSize = xMaxSize / maxWidth;
        float xPos = (xSize * xNum) + (xSize / 2f);
        float yPos = yPadding * floorN;

        //Add a random padding
        xPos += Random.Range(-xSize / 4f, xSize / 4f);
        yPos += Random.Range(-yPadding / 4f, yPadding / 4f);

        Vector3 pos = new Vector3(xPos, 0, yPos);
        PointOfInterest randomPOI = pointsOfInterestPrefabs[Random.Range(0, pointsOfInterestPrefabs.Count)];
        PointOfInterest instance = Instantiate(randomPOI, boardContainer);
        pointsOfInterest.Add(instance);

        instance.transform.localPosition = pos;
        _pointOfInterestsPerFloor[floorN][xNum] = instance;
        int created = 0;

        void InstantiateNextPoint(int index_i, int index_j)
        {
            PointOfInterest nextPOI = InstantiatePointOfInterest(index_i, index_j);
            AddLineBetweenPoints(instance, nextPOI);
            instance.NextPointsOfInterestWithPath.Add(nextPOI);
            created++;
            _numberOfConnections++;
        }

        while (created == 0 && floorN < mapLength - 1)
        {
            if (xNum > 0 && Random.Range(0f, 1f) < chancePathSide)
            {
                if (allowCrisscrossing || _pointOfInterestsPerFloor[floorN + 1][xNum] == null)
                {
                    InstantiateNextPoint(floorN + 1, xNum - 1);
                }
            }

            if (xNum < maxWidth - 1 && Random.Range(0f, 1f) < chancePathSide)
            {
                if (allowCrisscrossing || _pointOfInterestsPerFloor[floorN + 1][xNum] == null)
                {
                    InstantiateNextPoint(floorN + 1, xNum + 1);
                }
            }

            if (Random.Range(0f, 1f) < chancePathMiddle)
            {
                InstantiateNextPoint(floorN + 1, xNum);
            }
        }

        return instance;
    }

    private void CreateMap()
    {
        List<int> positions = GetRandomIndexes(numberOfStartingPoints);
        foreach (int j in positions)
        {
            _ = InstantiatePointOfInterest(0, j);
        }


        if (_numberOfConnections <= mapLength)
        {
            Debug.Log($"Recreating board with {_numberOfConnections} connections");
            RecreateBoard();
            return;
        }

        Debug.Log($"Created board with {_numberOfConnections} connections");
        Debug.Log($"Created board with {pointsOfInterest.Count} points");
    }

    private void AddLineBetweenPoints(PointOfInterest thisPoint, PointOfInterest nextPoint)
    {
        float len = _lineLength;
        float height = _lineHeight;

        //Get direction from point A to B
        Vector3 dir = (nextPoint.transform.position - thisPoint.transform.position).normalized;

        //Get distance from point A to B
        float dist = Vector3.Distance(thisPoint.transform.position, nextPoint.transform.position);

        //Number of lines (with padding) that fits inside the space from point A to B
        int num = (int)(dist / (len * multiplicativeSpaceBetweenLines));

        //Find the real padding distance, since num is rounded to integer, the padding may increase
        float pad = (dist - (num * len)) / (num + 1);

        //Position of first line. the len/2f is the center of the line
        Vector3 pos_i = thisPoint.transform.position + (dir * (pad + (len / 2f)));

        //Position all the lines
        for (int i = 0; i < num; i++)
        {
            Vector3 pos = pos_i + ((len + pad) * i * dir);
            GameObject lineCreated = Instantiate(pathPrefab, pos, Quaternion.identity, boardContainer);
            lineCreated.transform.LookAt(nextPoint.transform);
            lineCreated.transform.position -= Vector3.up * (height / 2f);
        }
    }

    private List<int> GetRandomIndexes(int n)
    {
        List<int> indexes = new List<int>();
        if (n > maxWidth)
        {
            throw new System.Exception("Number of starting points greater than maxWidth!");
        }

        while (indexes.Count < n)
        {
            int randomNum = Random.Range(0, maxWidth);
            if (!indexes.Contains(randomNum))
            {
                indexes.Add(randomNum);
            }
        }
        return indexes;
    }


    private void DestroyImmediateAllChildren(Transform transform)
    {
        List<Transform> toKill = new();

        foreach (Transform child in transform)
        {
            toKill.Add(child);
        }

        for (int i = toKill.Count - 1; i >= 0; i--)
        {
            DestroyImmediate(toKill[i].gameObject);
        }
    }
}
