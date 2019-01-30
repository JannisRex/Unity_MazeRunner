using UnityEngine;

public class CameraController : MonoBehaviour
{
    public GameObject navmeshAgent;
    public GameObject Player;
    public Maze MazeScript;


    // ref to container for enabling/disabling
    public Transform PlayerHierachy;
    public Transform NavMeshHierachy;

    void LateUpdate()           // Set Correct Camera Settings and enable/disable Hierachy of others (FirstPerson / NavMesh / TopDown)
    {
        if (MazeScript.bUseFirstPersonController)
            FirstPersonCamera();
        else if (MazeScript.bUseNavMesh)
            NavMeshActiveCamera();
        else
            TopDownCamera();
    }

    void FirstPersonCamera()    // Settings for FirstPerson Camera
    {
        gameObject.SetActive(false);    // Disables Main Camera

        NavMeshHierachy.gameObject.SetActive(false);    // Disables NavMesh Hierachy
        PlayerHierachy.gameObject.SetActive(true);      // Enables FirstPerson Hierachy
    }   

    void NavMeshActiveCamera()  // Settings for NavMesh Camera
    {
        PlayerHierachy.gameObject.SetActive(false); // Disables FirstPerson Hierachy
        NavMeshHierachy.gameObject.SetActive(true); // Enables NavMesh Hierachy

        transform.localPosition = navmeshAgent.transform.position + new Vector3(0f, 1f, -1f);   // Initial Camera Pos behind Agent
        transform.eulerAngles = new Vector3(20f, 0f, 0f);   // Correct Rotation from 'followview'
    }
    
    void TopDownCamera()        // Settings for TopDown Camera
    {
        PlayerHierachy.gameObject.SetActive(false);     // Disables FirstPerson Hierachy
        NavMeshHierachy.gameObject.SetActive(false);    // Disables NavMesh Hierachy
        
        float averageSize = (MazeScript.iSizeX + MazeScript.iSizeY) / 2;    // Get Size of Maze

        if (MazeScript.fWallLength == 1f)   // if wall Prefab with Length of 1 is choosen
            transform.localPosition = new Vector3(0f, averageSize, -.5f);
        else                                // if wall Prefab with Length of 2 is choosen
            transform.localPosition = new Vector3(averageSize / 2, averageSize * 2, averageSize / 2 - .5f);

        transform.eulerAngles = new Vector3(90f, 0f, 0f);   // top down angle
    }      
}
