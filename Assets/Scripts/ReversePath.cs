using UnityEngine;
using UnityEngine.AI;
using System.Collections;

public class ReversePath : MonoBehaviour
{
    // Just for debugging. No actual purpose in the maze/navmesh

    public Transform navmeshDestination;    // ref to reversed Destination (so basically the Start Point of the original navmesh)
    public NavMeshAgent navmeshAgent;       // ref to agent (doesnt acutally walk, but is needed for calculations)
    public Maze MazeScript;                 // ref to maze for scripts and variables
    public NavMeshPath path;                // reversed path


    void Start()
    {
        StartCoroutine(WaitBeforeStarting());
    }

    IEnumerator WaitBeforeStarting()        // Wait till Map is generated and Locations are set to prevent instant finish and/or not starting at all
    {
        yield return new WaitForSeconds(2f);                // lazy safety here. Start and Goal pos get set at the end of script so they cant be used to early
        navmeshAgent.Warp(MazeScript.NavMeshGoalPosition);
        navmeshDestination.transform.position = MazeScript.NavMeshStartingPosition;
    }

    void OnDrawGizmos()                     // Draw independent calculated path from end to start
    {
        path = new NavMeshPath();   // for storing path
        
        if (MazeScript.bDrawReverseNavMeshPath) // if bool checked in Editor
        {
            if (navmeshAgent == null || navmeshAgent.path == null)
                return;

            var line = GetComponent<LineRenderer>();    // Get Line renderer to draw line later 
            if (line == null)   // will create line on first run if there is none
            {
                line = gameObject.AddComponent<LineRenderer>();
                line.material = new Material(Shader.Find("Sprites/Default")) { color = Color.blue };
                line.startWidth = .5f;
                line.startColor = Color.blue;
            }

            NavMesh.CalculatePath(navmeshAgent.transform.position, navmeshDestination.transform.position, NavMesh.AllAreas, path); // calculate reverse Path ( Destination -> Start)

            line.positionCount = path.corners.Length;       // count of segments of line

            for (int i = 0; i < path.corners.Length; i++)
                line.SetPosition(i, path.corners[i]);       // Connect all Waypoints of reversed NavMeshPath
        }


    }
}
