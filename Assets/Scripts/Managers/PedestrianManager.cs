/**
 * Copyright (c) 2019 LG Electronics, Inc.
 *
 * This software contains code licensed as described in LICENSE.
 *
 */

using System.Collections.Generic;
using UnityEngine;
using Simulator.Map;
using Simulator.Utilities;
using UnityEngine.AI;
using UnityEngine.SceneManagement;

public class PedestrianManager : MonoBehaviour
{
    public GameObject pedPrefab;
    public List<GameObject> pedModels = new List<GameObject>();
    private bool _pedestriansActive = false;
    public bool PedestriansActive
    {
        get => _pedestriansActive;
        set
        {
            _pedestriansActive = value;
            TogglePedestrians();
        }
    }
    public enum PedestrianVolume { LOW = 50, MED = 25, HIGH = 10 };
    public PedestrianVolume pedVolume = PedestrianVolume.LOW;

    [HideInInspector]
    public List<MapPedestrian> pedPaths = new List<MapPedestrian>();
    private List<GameObject> pedPool = new List<GameObject>();
    private List<GameObject> pedActive = new List<GameObject>();
    private System.Random RandomGenerator;
    private System.Random PEDSeedGenerator;  // Only use this for initializing a new pedestrian
    private int Seed = new System.Random().Next();

    public void InitRandomGenerator(int seed)
    {
        Seed = seed;
        RandomGenerator = new System.Random(Seed);
        PEDSeedGenerator = new System.Random(Seed);
    }

    private void Start()
    {
        SpawnInfo[] spawnInfos = FindObjectsOfType<SpawnInfo>();
        var pt = Vector3.zero;
        if (spawnInfos.Length > 0)
        {
            pt = spawnInfos[0].transform.position;
        }
        NavMeshHit hit;
        if (NavMesh.SamplePosition(pt, out hit, 1f, NavMesh.AllAreas))
        {
            InitPedestrians();
            TogglePedestrians();
        }
        else
        {
            var sceneName = SceneManager.GetActiveScene().name;
            Debug.LogError($"{sceneName} is missing Pedestrian NavMesh");
            gameObject.SetActive(false);
        }
    }

    public void PhysicsUpdate()
    {
        for (int i = 0; i < pedActive.Count; i++)
        {
            var ped = pedActive[i];
            if (ped.activeInHierarchy)
            {
                var pedController = ped.GetComponent<PedestrianController>();
                pedController.PhysicsUpdate();
            }
        }
    }

    private void InitPedestrians()
    {
        pedPaths.Clear();
        pedPool.Clear();
        pedPaths = new List<MapPedestrian>(FindObjectsOfType<MapPedestrian>());
        for (int i = 0; i < pedPaths.Count; i++)
        {
            foreach (var localPos in pedPaths[i].mapLocalPositions)
                pedPaths[i].mapWorldPositions.Add(pedPaths[i].transform.TransformPoint(localPos)); //Convert ped segment local to world position

            pedPaths[i].PedVolume = Mathf.CeilToInt(Vector3.Distance(pedPaths[i].mapWorldPositions[0], pedPaths[i].mapWorldPositions[pedPaths[i].mapWorldPositions.Count - 1]) / (int)pedVolume);

            Debug.Assert(pedPrefab != null && pedModels != null && pedModels.Count != 0);
            pedPrefab.GetComponent<NavMeshAgent>().enabled = false; // disable to prevent warning issues parenting nav agent
            for (int j = 0; j < pedPaths[i].PedVolume; j++)
            {
                GameObject ped = Instantiate(pedPrefab, Vector3.zero, Quaternion.identity, transform);
                ped.GetComponent<PedestrianController>().SetGroundTruthBox();
                pedPool.Add(ped);
                Instantiate(pedModels[RandomGenerator.Next(pedModels.Count)], ped.transform);
                ped.SetActive(false);
                SimulatorManager.Instance.UpdateSemanticTags(ped);
            }
        }
    }

    private void TogglePedestrians()
    {
        if (pedPaths == null || pedPaths.Count == 0) return;

        if (PedestriansActive)
        {
            for (int i = 0; i < pedPaths.Count; i++)
            {
                for (int j = 0; j < pedPaths[i].PedVolume; j++)
                    SpawnPedestrian(pedPaths[i]);
            }
        }
        else
        {
            List<PedestrianController> peds = new List<PedestrianController>(FindObjectsOfType<PedestrianController>()); // search to prevent missed peds
            for (int i = 0; i < peds.Count; i++)
                ReturnPedestrianToPool(peds[i].gameObject);

            pedActive.Clear();
        }
    }

    private void SpawnPedestrian(MapPedestrian path)
    {
        if (pedPool.Count == 0) return;

        GameObject ped = pedPool[0];
        pedPool.RemoveAt(0);
        pedActive.Add(ped);
        ped.SetActive(true);
        PedestrianController pedC = ped.GetComponent<PedestrianController>();
        pedC.InitPed(path.mapWorldPositions, PEDSeedGenerator.Next());
        pedC.GTID = ++SimulatorManager.Instance.GTIDs;
    }

    private void ReturnPedestrianToPool(GameObject go)
    {
        go.SetActive(false);
        pedActive.Remove(go);
        pedPool.Add(go);
    }

    #region api
    public GameObject SpawnPedestrianApi(string name, Vector3 position, Quaternion rotation)
    {
        var prefab = pedModels.Find(obj => obj.name == name);
        if (prefab == null)
        {
            return null;
        }

        GameObject ped = Instantiate(pedPrefab, Vector3.zero, Quaternion.identity, transform);
        Instantiate(prefab, ped.transform);
        PedestrianController pedC = ped.GetComponent<PedestrianController>();
        pedC.InitManual(position, rotation, PEDSeedGenerator.Next());
        pedC.GTID = ++SimulatorManager.Instance.GTIDs;
        pedC.SetGroundTruthBox();
        pedActive.Add(ped);
        SimulatorManager.Instance.UpdateSemanticTags(ped);
        return ped;
    }

    public void DespawnPedestrianApi(PedestrianController ped)
    {
        ped.StopPEDCoroutines();
        pedActive.Remove(ped.gameObject);
        Destroy(ped.gameObject);
    }

    public void Reset()
    {
        RandomGenerator = new System.Random(Seed);
        PEDSeedGenerator = new System.Random(Seed);

        List<GameObject> peds = new List<GameObject>(pedActive);
        foreach (var ped in peds)
        {
            PedestrianController pedC = ped.GetComponent<PedestrianController>();
            if (pedC)
            {
                DespawnPedestrianApi(pedC);
            }
        }

        pedActive.Clear();
    }
    #endregion
}
