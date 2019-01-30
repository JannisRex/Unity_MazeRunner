using System.Collections;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.UI;
using System.Diagnostics;
using System.Timers;
using System;
using System.Collections.Generic;

public class PathingAndAnimation : MonoBehaviour
{
    //Pathfinding   
    public Maze MazeScript;                                 // Ref for scripts and variables
    public Transform navmeshDestination;                    // Destination ref
    public NavMeshAgent navmeshAgent;                       // Agent ref
    public float fAgentSpeed;                               // Speed should be < 6f and accel should be something like 100f
    private float _distanceStearingTarget;                  // empty float to store old dístance in, for comparing with new (for timescale unstuck)
    private Vector3[] navmeshAgentPathCorners;              // Stores all Waypoints of the navmeshPath (for backtracking)
    private NavMeshPath calculatedPathFromCorner;           // will be the recalculated NavMeshPath if agent gets stuck
    private bool bNavMeshAgentIsUnstucking = false;         // for executing real calculation
    private bool bUnstuckIsInitialized = false;             // Initial delay for UnstuckChecker
    private bool bFoundValidPath = false;                   // Disable repathing and new calculations if correct path got found
    [HideInInspector] public bool bNavMeshFinished = false; // Checking if finsihed for various things


    //Clock
    public Text Clock;                                      // Clock ref
    private float _time = 0f;                               // Timer for Clock
    private float fRoundTimeMinutes;                        // Minutes for Clock
    private float fRoundTimeSeconds;                        // Seconds for Clock


    //Animation
    public Animator xbotAnimatior;                          // Animator ref

    // debug & unstuck
    bool bDebugTimeGotCalled = false;                                           // prevent spamming 13k debugs xd
    //Stopwatch stuckTimerWatch = null;                                           // for unstucking
    //float fStuckTimer = 0f;                                                     // For 1st unstuck (with timedelta)
    //float timerStopWatchDebug = 0f;                                             // For Stopwatch Debug
    //bool bStopWatchStarted = false;                                             // prevent overriding clock
    Timer stuckTimerTimer = new Timer();                          // .5 sec   |   1.5s    |   3s


    void Awake()
    {
        calculatedPathFromCorner = new NavMeshPath();

        StartCoroutine(WaitBeforeStarting());
    }

    IEnumerator WaitBeforeStarting()                        // Wait till Map is generated and Locations are set to prevent instant finish and/or not starting at all
    {
        yield return new WaitForSeconds(.5f);                                           // wait half a second
        UnityEngine.Debug.Log("Waited for .5 seconds, NavMesh is starting");
        _time = 0f;                                                                     // reset clock
        navmeshAgent.SetDestination(navmeshDestination.position);                       // goto point
        CalculateAndTestPath();
    }

    void FixedUpdate()
    {
        //Debug.Log("Mag: " + navmeshAgent.velocity.magnitude); 
        //Debug.Log("sqrMag: " + navmeshAgent.velocity.sqrMagnitude);


        #region Debug Stopwatch
        //timerStopWatchDebug += Time.deltaTime;

        //if (timerStopWatchDebug >= 1f)
        //{
        //    if (stuckTimerWatch != null)
        //    {
        //        UnityEngine.Debug.Log("default: " + stuckTimerWatch.ElapsedMilliseconds);                     // 500
        //        UnityEngine.Debug.Log("/1000: " + (double)stuckTimerWatch.ElapsedMilliseconds / 1000);        // .05     
        //        UnityEngine.Debug.Log("Timespan : " + stuckTimerWatch.Elapsed);                               // 00:00:00.0500000
        //        UnityEngine.Debug.Log("Timespan SEC: " + stuckTimerWatch.Elapsed.Seconds);                    // .5

        //        timerStopWatchDebug = 0f;
        //    }
        //}
        #endregion

        RoundTimer();

        if (bNavMeshFinished)
            WhenFinished();

        if (Time.timeScale > 1.0f)
            StopGettingStuckInTheFuckingCorner();

        if (!bNavMeshFinished)
            StartCoroutine(InitializationForUnstuck());


        UnityEngine.Debug.DrawLine(navmeshAgent.transform.position, navmeshAgent.steeringTarget, Color.yellow, 2f, false);          // Draws a  yellow line from Agent to his current steeringTarget (basically overwrites the red one and fades after a few sec)
    }

    void OnTriggerEnter(Collider other)                     // just for the bool to check wether or not the maze is finished
    {
        if (other.gameObject.CompareTag("NavMeshGoal"))                                 // if hitting the goal trigger
            bNavMeshFinished = true;                                                    // set bool to do stuff
    }

    void RoundTimer()                                       // Display Clock counting up
    {
        if (!bNavMeshFinished)
            _time += Time.deltaTime;                        // Count Time

        fRoundTimeMinutes = Mathf.FloorToInt(_time / 60f);  // Format Minutes
        fRoundTimeSeconds = Mathf.FloorToInt(_time % 60f);  // Format Seconds

        Clock.text = string.Format("Time: {0:0}:{1:00}", fRoundTimeMinutes, fRoundTimeSeconds);            // Apply  gameclock         
    }

    IEnumerator InitializationForUnstuck()                  // Just a Timer waiting 10s to start check if stuck (prevents triggering the 'stuck logic' before even starting movement)
    {

        if (!bUnstuckIsInitialized)
        {
            yield return new WaitForSeconds(5f);                       // to prevent triggering at start
            //Debug.Log("Waited 10s");
            bUnstuckIsInitialized = true;
        }
        else
        {
            if (navmeshAgent.velocity.sqrMagnitude <= .09f || bNavMeshAgentIsUnstucking)            // if agent is standing (average mag is ~2.5 ->sqr something about ~6.2)
            {
                bFoundValidPath = false;
                //StartCoroutine(UnstuckAndRetarget());
                yield return StartCoroutine(UnstuckAndRetarget());
            }
        }

    }

    IEnumerator UnstuckAndRetarget()                        // If stuck somwhere go back 1 corner and recalculate
    {
        #region old unstuck (deltatime) -> Actually kinda works
        //bNavMeshAgentIsUnstucking = true;        // prevent overriding of timer
        //UnityEngine.Debug.Log("StuckTimer = 0s");
        //fStuckTimer += Time.deltaTime;       // count up

        //if (fStuckTimer >= .49 && fStuckTimer < .52)
        //    UnityEngine.Debug.Log("stuckTimer is .5f");

        //if (fStuckTimer >= 1.49 && fStuckTimer < 1.52)
        //    UnityEngine.Debug.Log("stuckTimer is 1.5f");

        //if (fStuckTimer >= 3f)                                                       // if standing still for 3 sec so essentially basically being stuck
        //{
        //    yield return UnstuckLogic();
        //}
        #endregion

        #region new unstuck - also broken (stopwatch)

        //if (!bStopWatchStarted)
        //{
        //    stuckTimerWatch = Stopwatch.StartNew();         // setting up the clock
        //    bStopWatchStarted = true;
        //}

        //if (!bNavMeshAgentIsUnstucking && !bFoundValidPath)
        //{
        //    bNavMeshAgentIsUnstucking = true;           // prevent overriding of timer

        //    UnityEngine.Debug.Log("StuckTimer initiated");
        //    // stuckTimerWatch = Stopwatch.StartNew();         // setting up the clock



        //    if (stuckTimerWatch.Elapsed.TotalSeconds >= .4 && stuckTimerWatch.Elapsed.Seconds < .6) 
        //        UnityEngine.Debug.Log("DF stuckTimer is .5f");

        //    if (stuckTimerWatch.Elapsed.CompareTo(new System.TimeSpan(0, 0, 0, 0, 500) ) > .045 )          
        //        UnityEngine.Debug.Log("TS stuckTimer is .5f");

        //    if ((double)stuckTimerWatch.ElapsedMilliseconds / 1000 >= .04 && (double)stuckTimerWatch.ElapsedMilliseconds / 1000 < .06)          // idk why this wont work?? 
        //        UnityEngine.Debug.Log("stuckTimer is .5f");

        //    //if ((double)stuckTimerWatch.ElapsedMilliseconds / 1000 >= 1.04 && (double)stuckTimerWatch.ElapsedMilliseconds / 1000 < 1.06)        // idk why this wont work?? 
        //    //    UnityEngine.Debug.Log("stuckTimer is 1.5f");

        //    if (stuckTimerWatch.ElapsedMilliseconds / 1000 >= 3.0)                                                       // if standing still for 3 sec so essentially basically being stuck
        //    {
        //        yield return UnstuckLogic();
        //    }
        //}
        #endregion

        #region bad unstuck - completely crashes unity, lul (using Sleep())
        //if (!bNavMeshAgentIsUnstucking)
        //{
        //    bNavMeshAgentIsUnstucking = true;           // prevent overriding of timer

        //    UnityEngine.Debug.Log("StuckTimer = 0s");

        //    System.Threading.Thread.Sleep(500);
        //    UnityEngine.Debug.Log("stuckTimer is .5f");

        //    System.Threading.Thread.Sleep(1000);
        //    UnityEngine.Debug.Log("stuckTimer is 1.5f");

        //    System.Threading.Thread.Sleep(1500);
        //    UnityEngine.Debug.Log("StuckTimer >= 3s");
        //    navmeshAgent.isStopped = true;                                                                                  // stop movement

        //    yield return UnstuckLogic();
        //}
        #endregion
        // maybe try yield return smh

        #region new new unstuck (with timerevent) -> WORKING!!!
        if (!bNavMeshAgentIsUnstucking && !bFoundValidPath)
        {
            bNavMeshAgentIsUnstucking = true;           // prevent overriding of timer

            stuckTimerTimer.Interval = TimeSpan.FromSeconds(3).TotalMilliseconds;  // 3s timer
            stuckTimerTimer.Elapsed += Timer_Elapsed;                              // when done, trigger this event basically
            stuckTimerTimer.AutoReset = false;                                     // extra saftey, should be unnecessary but w/e
            stuckTimerTimer.Start();                                               // Starts Timer

            UnityEngine.Debug.Log("StuckTimer initiated");

            yield return null;
        }

        #endregion
    }
    
    IEnumerator UnstuckLogic()
    {
        UnityEngine.Debug.Log("Trying to Unstuck...");
        navmeshAgent.isStopped = true;                                                                                  // stop movement

        for (int i = 0; i < 31; i++)    // for loop for testing ( will be while loop after checking if it works)
        {
            if (GetClosestCornerIndex(navmeshAgentPathCorners) > 0)
            {
                navmeshAgent.SetDestination((navmeshAgentPathCorners[GetClosestCornerIndex(navmeshAgentPathCorners) - 1]));     // Sets Destination to the closest previous Corner (basically just backtracking)
                navmeshAgent.isStopped = false;                                                                                 // resume movement
                UnityEngine.Debug.Log("Backtracking to last Corner");
                yield return new WaitForSeconds(1f);                                                                            // wait for 1 Seconds before going further (lazy goal detection)
                navmeshAgent.isStopped = true;                                                                              // stop movement
            }
            NavMesh.CalculatePath(navmeshAgent.transform.position, navmeshDestination.transform.position, NavMesh.AllAreas, calculatedPathFromCorner);  // get new path from current Agent location to destiantion
            UnityEngine.Debug.Log("calculating new Path; iteration = " + i);

            if (calculatedPathFromCorner.status == NavMeshPathStatus.PathComplete)                                          // if the new path is valid, use it with agent
            {
                navmeshAgent.path.ClearCorners();                                                                           // Clear old corners to prevent interference (not testest but could happen)
                navmeshAgent.path = calculatedPathFromCorner;                                                               // assign generated path to agent
                navmeshAgent.isStopped = false;                                                                             // resume movement
                UnityEngine.Debug.Log("Found new Valid Way! Starting Agent");
                bNavMeshAgentIsUnstucking = false;                                                                          // Since unstucking is done, reset bool
                bFoundValidPath = true;
                StopCoroutine(UnstuckLogic());
                break;
            }
            else if (calculatedPathFromCorner.status == NavMeshPathStatus.PathPartial)
                UnityEngine.Debug.Log("Only found Partial Path");
            else
                UnityEngine.Debug.Log("No Path found");


            if (i == 30)    // if no path was found - idk something should probaly happen to fix it
            {
                navmeshAgent.isStopped = true;
                UnityEngine.Debug.Log("after 30 attemps no valid way");
                bNavMeshAgentIsUnstucking = false;
                navmeshAgent.autoRepath = true;
                StopCoroutine(UnstuckLogic());
                break;
            }
        }
    }

    void Timer_Elapsed(object source, ElapsedEventArgs e)     // Event to Handle 3s timer for unstuck
    {
        stuckTimerTimer.Stop();                         // Stops  Timer
        UnityEngine.Debug.Log("3s passed -> STUCK");
        //StartCoroutine(UnstuckLogic());                 // throws "StartCoroutine_Auto_Internal can only be called from the main thread." 
        MainThread.ExecuteOnMainThread.Enqueue(() => { StartCoroutine(UnstuckLogic()); }); // enables starting a coroutine from a different thread than the unity main thread
    }

    void WhenFinished()                                     // Animation and Mesh stuff as soon as @Destination
    {
        xbotAnimatior.CrossFade("Bellydancing", 0f);            // Transition to next animation
        navmeshDestination.GetComponent<MeshRenderer>().enabled = false;         // make goal invis

        if (!bDebugTimeGotCalled)
        {
            UnityEngine.Debug.Log("Finished after: " + _time + "s");
            bDebugTimeGotCalled = true;
            navmeshAgent.isStopped = true;
            //navmeshAgent.angularSpeed = 0f;
        }
    }

    void StopGettingStuckInTheFuckingCorner()               // To prevent getting stuck in a NavMesh Corner (with changed TimeScale only)
    {
        float distanceST = Vector3.Distance(navmeshAgent.transform.position, navmeshAgent.steeringTarget); // get distance from position to steeringtarget (NOT destination) just next goto point essentially 
        if (distanceST <= 1.5f) //distance to next edge on nav mesh (basically the distance to skip)
        {
            if (_distanceStearingTarget < distanceST)
                navmeshAgent.transform.position = navmeshAgent.steeringTarget;
            else
                _distanceStearingTarget = distanceST;
        }
    }

    void OnDrawGizmos()                                     // Draw Path NavMeshAgent calculated (with corner points)
    {
        if (MazeScript.bDrawNavMeshPath)
        {
            #region RedLine

            if (navmeshAgent == null || navmeshAgent.path == null)                          // if there is no navmesh dont draw anything
                return;

            var line = GetComponent<LineRenderer>();
            if (line == null)
            {
                line = gameObject.AddComponent<LineRenderer>();
                line.material = new Material(Shader.Find("Sprites/Default")) { color = Color.red };
                line.startWidth = .5f;
                line.startColor = Color.red;
            }

            var path = navmeshAgent.path;

            line.positionCount = path.corners.Length;

            for (int i = 0; i < path.corners.Length; i++)
                line.SetPosition(i, path.corners[i]);           // connect all 'corners' (waypoints essentially) of the NavMeshPath

            #endregion
            
            #region CrossDots

            Gizmos.color = Color.green;

            if (navmeshAgentPathCorners.Length != 0)
            {
                for (int i = 0; i < navmeshAgentPathCorners.Length; i++)
                {
                    Gizmos.DrawSphere(navmeshAgentPathCorners[i], .25f);
                }
            }
            #endregion
        }

    }
    
    void CalculateAndTestPath()                             // Creates a Path and tests if its valid
    {
        var calculatedpath = new NavMeshPath();

        NavMesh.CalculatePath(navmeshAgent.transform.position, navmeshDestination.transform.position, NavMesh.AllAreas, calculatedpath);    // calculate a path (essentially being the same as the navmeshAgent path)

        navmeshAgentPathCorners = calculatedpath.corners;   // store all corners in array for visualization
        
        if (calculatedpath.status == NavMeshPathStatus.PathComplete)
            UnityEngine.Debug.Log("Path is Valid");
        
        if (calculatedpath.status == NavMeshPathStatus.PathPartial)
            UnityEngine.Debug.Log("Path is Partial");
        
        if (calculatedpath.status == NavMeshPathStatus.PathInvalid)     //literally never happened (21.06 happened first time lul)
            UnityEngine.Debug.Log("Path is Invalid");
    }

    int GetClosestCornerIndex(Vector3[] cornerPoints)       // Gets index of closest input node (for backtracking if stuck)
    {
        int bestTargetIndex = -1;                           // final target
        float closestDistance = Mathf.Infinity;             // for comparision
        Vector3 currentPosition = transform.position;       // pos of agent


        for (int index = 0; index < cornerPoints.Length; index++)
        {
            Vector3 directionToTarget = cornerPoints[index] - currentPosition;               // get vector direction for distance
            float distanceToTarget = directionToTarget.sqrMagnitude;                         // use squared distance instead of Distance() for performance (faster than Distance() since it square roots)
            if (distanceToTarget < closestDistance)                                          // compare new to previous smallest distance
            {
                closestDistance = distanceToTarget;
                bestTargetIndex = index;
            }
        }

        return bestTargetIndex;
    }

}
