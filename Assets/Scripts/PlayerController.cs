using System.Collections;
using UnityEngine.UI;
using UnityEngine;

public class PlayerController : MonoBehaviour
{

    //Attributes
    public float fWalkSpeed;                                // Adjust MovementSpeed
    public float fJumpSpeed;                                // Jump Height kind of

    //Player
    private Rigidbody playerRB;                             // Rigidbody of Player
    private CapsuleCollider playerCOL;                      // Collider of Player
    private Vector3 movementDir;                            // combintaion of vertical and horizontal movement
    private float MWheelInput;                              // For having constant Mwheel inuput
    [HideInInspector] public bool bNavMeshFinished = false; // Checking if finsihed for various things

    //Clock
    public Text Clock;                                      // Clock ref
    private float _time = 0f;                               // Timer for Clock
    private float fRoundTimeMinutes;                        // Minutes for Clock
    private float fRoundTimeSeconds;                        // Seconds for Clock

    void Awake()
    {
        playerRB = GetComponent<Rigidbody>();
        playerCOL = GetComponent<CapsuleCollider>();
    } 

    void Update()
    {
        StartCoroutine(FixMouseWheel());
            
        RoundTimer();

        float horizontalMovement = 0f;      // get set to 0 because CS0165
        float verticalMovement = 0f;        // get set to 0 because CS0165 

        //if (CanMove(transform.right * Input.GetAxisRaw("Horizontal")))
            horizontalMovement = Input.GetAxisRaw("Horizontal");                // Only store Axis Input if Player is able to move Horizontally

        //if (CanMove(transform.forward * Input.GetAxisRaw("Vertical")))
            verticalMovement = Input.GetAxisRaw("Vertical");                    // Only store Axis Input if Player is able to move Vertically 

        movementDir = (horizontalMovement * transform.right + verticalMovement * transform.forward).normalized;     // combine horizontal and vertical movement to create correct direction
    }

    void FixedUpdate()
    {
        Move();

        if (Input.GetKey(KeyCode.Space) || MWheelInput != 0f)
            if (CanJump())
                Jump();

    }
    
    #region PlayerMovement (Move + Jump and conditions)

    void Move()                             // Player Movement Logic
    {
        Vector3 yVelocity = new Vector3(0f, playerRB.velocity.y, 0f);       // Since we only have Forward and Sideways movement, Y will always be 0
        playerRB.velocity = movementDir * fWalkSpeed * Time.deltaTime;      // Apply MovementVelocity
        playerRB.velocity += yVelocity;                                     // Reapply Y-Velocity so it isnt 0
    }

    void Jump()
    {
        playerRB.velocity += new Vector3(0f, fJumpSpeed * Time.deltaTime, 0f);
    }

    bool CanMove(Vector3 dir)         // Checks if player is colliding with wall (check wether he can move or not)
    {
        float fDistanceToPoints = playerCOL.height / 2 - playerCOL.radius;      // Distance from center to Points
        float fRadius = playerCOL.radius * .95f;                                // should be the size of our collider, but it shouldnt hit the ground
        float fCastDistance = .5f;                                              // just something small since the castdistance shouldnt be to big

        Vector3 point1 = transform.position + playerCOL.center + fDistanceToPoints * Vector3.up;     //  since we scale an upwards vector3 its as long as the distance and we just add/sub
        Vector3 point2 = transform.position + playerCOL.center - fDistanceToPoints * Vector3.up;     //  adding transform to get points relative to the player rather than world


        RaycastHit[] hits = Physics.CapsuleCastAll(point1, point2, fRadius, dir, fCastDistance);      // Collects all Collisions with CapsuleCollider (basically if the player is touching the wall...)

        foreach (var hit in hits)
            if (hit.transform.tag == "LevelObject")             // Check if its Wall
                return false;

        return true;
    }

    bool CanJump()                          // Checks if the player is grounded (check wether he can jump or not)
    {
        float fDistanceToPoints = playerCOL.height / 2 - playerCOL.radius;      // Distance from center to Points
        float fRadius = playerCOL.radius;                                       // should be the size of our collider
        float fCastDistance = .1f;                                              // just something small since the castdistance shouldnt be to big

        Vector3 point1 = transform.position + playerCOL.center + fDistanceToPoints * Vector3.up;     //  since we scale an upwards vector3 its as long as the distance and we just add/sub
        Vector3 point2 = transform.position + playerCOL.center - fDistanceToPoints * Vector3.up;     //  adding transform to get points relative to the player rather than world


        RaycastHit[] hits = Physics.CapsuleCastAll(point1, point2, fRadius, -transform.up, fCastDistance);      // Checks if Player is touching the ground

        foreach (var hit in hits)
            if (hit.transform.tag == "LevelObject")     // check if its Ground
                return true;

        return false;

    }

    IEnumerator FixMouseWheel()
    {
        if (CanJump())
        {
            if (Input.GetAxis("Mouse ScrollWheel") != 0f)
            {
                MWheelInput = .3f;
                yield return new WaitForSeconds(.3f);
                MWheelInput = 0f;
            }
        }
    }

    #endregion

    void RoundTimer()                       // Dispaly Clock counting up
    {
        if (!bNavMeshFinished)
            _time += Time.deltaTime;                        // Count Time

        fRoundTimeMinutes = Mathf.FloorToInt(_time / 60f);  // Format Minutes
        fRoundTimeSeconds = Mathf.FloorToInt(_time % 60f);  // Format Seconds

        Clock.text = string.Format("Time: {0:0}:{1:00}", fRoundTimeMinutes, fRoundTimeSeconds);            // Apply  gameclock         
    }

    void OnTriggerEnter(Collider other)     // just for the bool to check wether or not the maze is finished
    {
        if (other.gameObject.CompareTag("NavMeshGoal"))
        {
            bNavMeshFinished = true;
            other.gameObject.transform.Translate(-Vector3.up * Time.deltaTime * 2f);
        }
    }
}
