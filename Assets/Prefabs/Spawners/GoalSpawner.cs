using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GoalSpawner : Prefab
{
    [Header("Spawning Params")]
    public BallGoal[] spawnObjects;
    public float initialSpawnSize;
    public float ripenedSpawnSize;
    public bool variableSize;
    public bool variableSpawnPosition;
    public float sphericalSpawnRadius;
    public Vector3 defaultSpawnPosition;
    public float timeToRipen; // in seconds
    public float timeBetweenSpawns; // also in seconds
    public float delaySeconds;
    public int spawnCount; // total number spawner can spawn; -1 if infinite
    [ColorUsage(true, true)]
    public Color colourOverride;
    private bool willSpawnInfinite() { return spawnCount == -1; }
    private bool canStillSpawn() { return spawnCount!=0; }

    private bool isSpawning = false;

    private float height;

    // random-object-spawning toggle and associated objects
    private bool spawnsRandomObjects;
    public int objSpawnSeed = 0; public int spawnSizeSeed = 0;
    /* IMPORTANT use ''System''.Random so can be locally instanced;
     * ..this allows us to fix a sequence via a particular seed. 
     * Four RNGs depending on which random variations are toggled:
     * (1) OBJECT: for spawn-object selection
     * (2) SIZE: for eventual size of spawned object when released
     * (3) H_ANGLE: proportion around the tree where spawning occurs
     * (4) V_ANGLE: extent up the tree where spawning occurs */
    private System.Random[] RNGs = new System.Random[4];
    private enum E {OBJECT=0, SIZE=1, H_ANGLE=2, V_ANGLE=3};

    void Awake()
    {
        // combats random size setting from ArenaBuilder
        sizeMin = sizeMax = Vector3Int.one;
        canRandomizeColor = false; ratioSize = Vector3Int.one;
        height = GetComponent<Renderer>().bounds.size.y;

        // sets to random if more than one spawn object to choose from
        // else just spawns the same object repeatedly
        // assumes uniform random sampling (for now?)
        spawnsRandomObjects = (spawnObjects.Length>1);
        if (spawnsRandomObjects) { RNGs[(int)E.OBJECT] = new System.Random(objSpawnSeed); }
        if (variableSize) { RNGs[(int)E.SIZE] = new System.Random(spawnSizeSeed); }
        if (variableSpawnPosition) { RNGs[(int)E.H_ANGLE] = new System.Random(0); RNGs[(int)E.V_ANGLE] = new System.Random(0); }

        StartCoroutine(startSpawning());

    }

    public override void SetSize(Vector3 size)
    {
        // bypasses random size assignment (used e.g. by ArenaBuilder) from parent Prefab class,
        // fixing to desired size otherwise just changes size as usual
        sizeMin = sizeMax = Vector3Int.one;//new Vector3(0.311f, 0.319f, 0.314f);
        base.SetSize(Vector3Int.one);
        _height = height;
    }
    protected override float AdjustY(float yIn)
    {
        return yIn;
        // trivial call - just in case of the GoalSpawner, we have origin at the bottom not in middle of bounding box
        // so no need to compensate for origin position via AdjustY
    }

    private void OnDrawGizmos()
    {
        Gizmos.DrawWireSphere(transform.position + defaultSpawnPosition, sphericalSpawnRadius);
        var bs = transform.GetComponent<Renderer>().bounds.size;
        Gizmos.DrawWireCube(transform.position + new Vector3(0,bs.y/2,0), bs);
    }

    private IEnumerator startSpawning() {
        yield return new WaitForSeconds(delaySeconds);

        isSpawning = true;
        while (canStillSpawn()) {
            // spawn first, wait second, repeat

            spawnNewGoal(0);
            // instantiate using linear interpolation growth
            // with growth time set by timeToRipen
            // then remove isKinematic/!useGravity constraint

            if (!willSpawnInfinite()) { spawnCount--; }

            yield return new WaitForSeconds(timeBetweenSpawns);
        }
    }

    BallGoal spawnNewGoal(int listID) {

        // calculate spawning location if necessary
        Vector3 spawnPos;
        if (variableSpawnPosition)
        {
            float phi /*azimuthal angle*/           = (float) (RNGs[(int)E.H_ANGLE].NextDouble() * 2 * Math.PI);
            float theta /*polar/inclination angle*/ = (float)((RNGs[(int)E.V_ANGLE].NextDouble() * 0.6f + 0.2f) * Math.PI);
            spawnPos = defaultSpawnPosition + sphericalToCartesian(sphericalSpawnRadius, theta, phi);
        }
        else { spawnPos = defaultSpawnPosition; }

        BallGoal newGoal = (BallGoal)Instantiate(spawnObjects[listID], transform.position + spawnPos, Quaternion.identity);
        newGoal.transform.parent = this.transform;
        newGoal.enabled = false;

        newGoal.SetSize(Vector3.one * initialSpawnSize);
        if (colourOverride != null) {
            Material _mat = newGoal.GetComponent<MeshRenderer>().material;
            _mat.SetColor("_EmissionColor", colourOverride);
        }

        newGoal.gameObject.GetComponent<Rigidbody>().useGravity = false;

        newGoal.enabled = true;
        return newGoal;
    }


    Vector3 sphericalToCartesian(float r, float theta, float phi) {
        float sin_theta = Mathf.Sin(theta);
        return new Vector3(r * Mathf.Cos(phi) * sin_theta, r * Mathf.Cos(theta), r * Mathf.Sin(phi) * sin_theta);
    }
}
