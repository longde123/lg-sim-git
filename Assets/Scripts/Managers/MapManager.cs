using System.Diagnostics;
/**
 * Copyright (c) 2019 LG Electronics, Inc.
 *
 * This software contains code licensed as described in LICENSE.
 *
 */

using System.Collections.Generic;
using UnityEngine;
using Simulator.Utilities;
using Simulator.Map;
using Simulator.Api;

public class MapManager : MonoBehaviour
{
    [System.NonSerialized]
    public List<MapLane> trafficLanes = new List<MapLane>();
    [System.NonSerialized]
    public List<MapIntersection> intersections = new List<MapIntersection>();
    public float totalLaneDist { get; private set; } = 0f;

    public List<MapLine> trafficLinesLeft = new List<MapLine>();
    public List<MapLine> trafficLinesRight = new List<MapLine>();

    private MapManagerData mapData;
    public List<MapLane> trafficLaneSegments = new List<MapLane>();

    private void Awake()
    {
        SetMapData();
    }

    private void Start()
    {
        intersections.ForEach(intersection => intersection.StartTrafficLightLoop());
    }

    private void SetMapData()
    {
        mapData = new MapManagerData();
        if (mapData.MapHolder == null)
            return;

        trafficLanes = mapData.GetTrafficLanes();
        intersections = mapData.GetIntersections();
        totalLaneDist = MapManagerData.GetTotalLaneDistance(trafficLanes);
        trafficLanes.ForEach(trafficLane => trafficLane.SetTrigger());
        intersections.ForEach(intersection => intersection.SetTriggerAndState());
        trafficLinesLeft = mapData.GetTrafficLeftBoundaryLines();
        trafficLinesRight = mapData.GetTrafficRightBoundaryLines();
    }

    // npc and api
    public MapLane GetClosestLane(Vector3 position)
    {
        MapLane result = null;
        float minDist = float.PositiveInfinity;

        // TODO: this should be optimized
        foreach (var lane in trafficLanes)
        {
            if (lane.mapWorldPositions.Count >= 2)
            {
                for (int i = 0; i < lane.mapWorldPositions.Count - 1; i++)
                {
                    var p0 = lane.mapWorldPositions[i];
                    var p1 = lane.mapWorldPositions[i + 1];

                    float d = Utility.SqrDistanceToSegment(p0, p1, position);
                    if (d < minDist)
                    {
                        minDist = d;
                        result = lane;
                    }
                }
            }
        }
        return result;
    }


    public MapLine GetClosestLine(Vector3 position)
    {
        MapLine result = null;
        float minDistLeft = float.PositiveInfinity;
        float minDistRight = float.PositiveInfinity;
        MapLine Leftline = new MapLine();
        MapLine RightLine = new MapLine();

        // TODO: this should be optimized
        foreach (var LL in trafficLinesLeft)
        {
            if (LL.mapWorldPositions.Count >= 2)
            {
                for (int i = 0; i < LL.mapWorldPositions.Count - 1; i++)
                {
                    var p0 = LL.mapWorldPositions[i];
                    var p1 = LL.mapWorldPositions[i + 1];

                    float d = Utility.SqrDistanceToSegment(p0, p1, position);
                    if (d < minDistLeft)
                    {
                        Leftline = LL;
                        minDistLeft = d;
                        result = Leftline;
                    }
                }
            }
        }
        foreach (var RL in trafficLinesRight)
        {
            if (RL.mapWorldPositions.Count >= 2)
            {
                for (int i = 0; i < RL.mapWorldPositions.Count - 1; i++)
                {
                    var p0 = RL.mapWorldPositions[i];
                    var p1 = RL.mapWorldPositions[i + 1];

                    float d = Utility.SqrDistanceToSegment(p0, p1, position);
                    if (d < minDistRight)
                    {
                        RightLine = RL;
                        minDistRight = d;
                        result = RightLine;
                    }
                }
            }
        }
        if(minDistLeft <= minDistRight)
        {
            result = Leftline;
        }
        else
        {
            result = RightLine;
        }
        return result;
    }

    public int GetLaneNextIndex(Vector3 position, MapLane lane)
    {
        float minDist = float.PositiveInfinity;
        int index = -1;
        
        for (int i = 0; i < lane.mapWorldPositions.Count - 1; i++)
        {
            var p0 = lane.mapWorldPositions[i];
            var p1 = lane.mapWorldPositions[i + 1];

            var p = Utility.ClosetPointOnSegment(p0, p1, position);

            float d = Vector3.SqrMagnitude(position - p);
            if (d < minDist)
            {
                minDist = d;
                index = i + 1;
            }
        }

        return index;
    }

    // api
    public void GetPointOnLane(Vector3 point, out Vector3 position, out Quaternion rotation)
    {
        var lane = GetClosestLane(point);

        int index = -1;
        float minDist = float.PositiveInfinity;
        Vector3 closest = Vector3.zero;

        for (int i = 0; i < lane.mapWorldPositions.Count - 1; i++)
        {
            var p0 = lane.mapWorldPositions[i];
            var p1 = lane.mapWorldPositions[i + 1];

            var p = Utility.ClosetPointOnSegment(p0, p1, point);

            float d = Vector3.SqrMagnitude(point - p);
            if (d < minDist)
            {
                minDist = d;
                index = i;
                closest = p;
            }
        }

        position = closest;
        rotation = Quaternion.LookRotation(lane.mapWorldPositions[index + 1] - lane.mapWorldPositions[index], Vector3.up);
    }

    public void GetPointOnLine(Vector3 point, out Vector3 position, out Quaternion rotation)
    {
        var line = GetClosestLine(point);

        int index = -1;
        float minDist = float.PositiveInfinity;
        Vector3 closest = Vector3.zero;

        for (int i = 0; i < line.mapWorldPositions.Count - 1; i++)
        {
            var p0 = line.mapWorldPositions[i];
            var p1 = line.mapWorldPositions[i + 1];

            var p = Utility.ClosetPointOnSegment(p0, p1, point);

            float d = Vector3.SqrMagnitude(point - p);
            if (d < minDist)
            {
                minDist = d;
                index = i;
                closest = p;
            }
        }

        position = closest;
        rotation = Quaternion.LookRotation(line.mapWorldPositions[index + 1] - line.mapWorldPositions[index], Vector3.up);
    }

    public MapLane GetLane(int index)
    {
        return trafficLanes == null || trafficLanes.Count == 0 ? null : trafficLanes[index];
    }

    public void Reset()
    {
        var api = ApiManager.Instance;
        var controllables = SimulatorManager.Instance.Controllables;
        controllables.Clear();

        foreach (var intersection in intersections)
        {
            intersection.npcsInIntersection.Clear();
            intersection.stopQueue.Clear();

            if (!intersection.isStopSignIntersection)
            {
                foreach (var signal in intersection.GetSignals())
                {
                    var uid = System.Guid.NewGuid().ToString();
                    api.Controllables.Add(uid, signal);
                    api.ControllablesUID.Add(signal, uid);
                    controllables.Add(signal);
                }
            }

            intersection.SetTriggerAndState();
            intersection.StartTrafficLightLoop();
        }
    }

    public void RemoveNPCFromIntersections(NPCController npc)
    {
        foreach (var intersection in intersections)
        {
            intersection.ExitIntersectionList(npc);
            if (intersection.isStopSignIntersection)
            {
                intersection.ExitStopSignQueue(npc);
            }
        }
    }
    public void CreateLaneSegment(string[] lane_uid_list)
    {
        //Find the gameobjects of MapLane type which will be used for searching uids
        var map_lanes_go = FindObjectsOfType<MapLane>();
        if(map_lanes_go.Length != 0)
        {
            for(int i = 0; i < lane_uid_list.Length; i++)
            {
                int counter = 0;
                foreach(var lane_go in map_lanes_go)
                {
                    counter++;
                    if(lane_go.name.Contains(lane_uid_list[i]))
                    {
                        var lane = lane_go.GetComponent<MapLane>();
                        trafficLaneSegments.Add(lane);
                        break;
                    }
                    else
                    {
                        if(counter == map_lanes_go.Length)
                        {
                            UnityEngine.Debug.Log($"uid not found");
                        }
                    }
                }
            }
        }
        else
        {
            UnityEngine.Debug.Log($"MapLane objects not found.Map may be disabled.Check Assetbundle");
        }
    }
    public MapLane GetClosestLane(Vector3 position, string[] lane_uid_list)
    {
        MapLane result = null;
        float minDist = float.PositiveInfinity;
        CreateLaneSegment(lane_uid_list);

        // TODO: this should be optimized
        foreach (var lane in trafficLaneSegments)
        {
            if (lane.mapWorldPositions.Count >= 2)
            {
                for (int i = 0; i < lane.mapWorldPositions.Count - 1; i++)
                {
                    var p0 = lane.mapWorldPositions[i];
                    var p1 = lane.mapWorldPositions[i + 1];

                    float d = Utility.SqrDistanceToSegment(p0, p1, position);
                    if (d < minDist)
                    {
                        minDist = d;
                        result = lane;
                    }
                }
            }
        }
        return result;
    }
}
