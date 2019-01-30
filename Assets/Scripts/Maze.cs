using UnityEngine;
using System.Collections.Generic;
using System.Collections;
using UnityEditor;
using UnityEngine.AI;

public class Maze : MonoBehaviour
{
    public struct Cell                              // Giving attributes to each cell (struct instead of class to prevent NullReferenceError)
    {
        public bool bVisited;                       // wether or not the cell already got 'visited'
        public GameObject north;                    // 1 (Input for Switch-Case to Break randomized Wall)
        public GameObject west;                     // 2 (Input for Switch-Case to Break randomized Wall)
        public GameObject east;                     // 3 (Input for Switch-Case to Break randomized Wall)
        public GameObject south;                    // 4 (Input for Switch-Case to Break randomized Wall)
    }

    //Maze
    public float fMazeCreationSpeed;                            // delay @ coroutine -> just for visuals (if delay should be removed change IEnum back to func)
    public GameObject WallPrefab1;                              // Ref to Wall (cube) for instantiation (Length 1)
    public GameObject WallPrefab2;                              // Ref to Wall (cube) for instantiation (Length 2)
    private GameObject WallPrefabCorrectSize;                   // to cycle between both prefabs without having to change in Editor
    public float fWallLength;                                   // Length of Walls
    public int iSizeX;                                          // X - Size of Maze
    public int iSizeY;                                          // Y - Size of Maze (is Z axis in vector since its 2d basically)
    public Color WallColor = Color.white;                       // Color of Walls
    public Color GroundColor = Color.black;                     // Color of Ground
    private Vector3 MazeCreationStartingPos;                    // initial pos  (bottom left corner where grid building starts)
    private Vector3 MazeCreationCurrentPos;                     // iterated pos of each wall of each chamber    (basically just the pos where the Wall gets placed)
    [HideInInspector] public GameObject MazeWallsContainer;     // For Holding all Instantiated Walls -> all walls are child of this (circumvent mess in Editor)
    [HideInInspector] public GameObject[] walls;                // Array holding all walls to cycle through
    [HideInInspector] public GameObject ground;                 // needs publicss
    private Cell[] cells;                                       // All Cells (x * y)  and to reference each cell's attributes
    private List<int> visitedCellsForBackTracking;              // list of visited cells (for backtracking)
    private int iCurrentCell = 0;                               // Starting Cell for getting neighbours
    private int iVisitedCells = 0;                              // Cells that are visited to stop recursive function (compare to max cells)
    private int iCurrentNeighbour = 0;                          // pos of neighbour as number
    private int iBackTrackIndex = 0;                            // for backtracking
    private int iWallDirectionToBreak = 0;                      // to determine wall to destroy
    private bool bStartedBuilding = false;                      // if building process has started
    private bool bGroundCreated = false;                        // to prevent multiple grounds
    public bool bUseMaterialInsteadOfColor = false;             // can be checked in Editor to choose between color and material
    public bool bShowMazeCreation = false;                      // instant maze or watch creating
    public bool bUseFirstPersonController = false;              // wether or not to use FirstPerson CameraController and PlayerController 
    public bool bUseNavMesh = false;                            // wether or not to use NavMesh (Agent + Destination)
    public bool bUsePrebakedGround = false;                     // For not Spawning a Ground if using prebaked Ground for NavMesh
    public bool bDrawNavMeshPath = true;                        // Displays the path, which the agent will go
    public bool bDrawReverseNavMeshPath = true;                 // Calculates and draws a reversed Path

    //FirstPerson
    public GameObject FirstPersonModel;                         // Acutally just a collider (maybe add a model)
    public GameObject FirstPersonGoal;                          // Goal for Triggers and such

    // NavMesh
    public float fTimeScale = 1f;                               // TimeScale for testing
    public NavMeshAgent NavMeshAgent;                           // ref to Agent
    public GameObject NavMeshDestination;                       // ref to Destination
    [HideInInspector] public Vector3 NavMeshStartingPosition;   // SpawnPosition for NavMeshAgent
    [HideInInspector] public Vector3 NavMeshGoalPosition;       // DestinationPoint for NavMeshAgent
    public Material wallMaterial;                               // wallmaterial
    public Material groundMaterial;                             // groundMaterial

    // Debug
    private bool bDebugIssued = false;                          // Prevent Debug getting printed 13k times in console xd

    

    void Awake()
    {
        GetCorrectWallPrefab();     // Choose between Length1 and Length2

        CreateGrid();               // Start Maze Creation Process ( Grid -> Cells -> Algorithm -> Recursion)
    }

    void GetCorrectWallPrefab()                                 //For not having to change the prefab in the editor
    {
        if (fWallLength == 2f)
            WallPrefabCorrectSize = WallPrefab2;
        else
            WallPrefabCorrectSize = WallPrefab1;
    }

    void CreateGrid()                                           // Builds a closed Grid with given X-Size and Y-Size (just walls at every possible 'wall position')
    {
        GameObject tempwall;            // for hierarchy and removing
        MazeWallsContainer = new GameObject();   // Create Empty GameObject as Container for Walls (prevent messed up editor)
        MazeWallsContainer.name = "MazeWalls";   // name it accordingly

        MazeCreationStartingPos = new Vector3((-iSizeX / 2) + fWallLength / 2, 0f, (-iSizeY / 2) + fWallLength / 2);        // Get startingPos which essentially is just the bottom left corner

        // For Vertical Walls
        for (int i = 0; i < iSizeY; i++)            // The Y-Position of the X-Wall
        {                                           // building up the maze structure by proceeding step for step (basically shifting one wall distance each iteration)
            for (int j = 0; j <= iSizeX; j++)       // The X-Position of the X-Wall
            {
                MazeCreationCurrentPos = new Vector3(MazeCreationStartingPos.x + (j * fWallLength) - fWallLength / 2, 0f, MazeCreationStartingPos.z + (i * fWallLength) - fWallLength / 2); // create squares of walls essentially to get maze structure

                tempwall = Instantiate(WallPrefabCorrectSize, MazeCreationCurrentPos, Quaternion.identity) as GameObject;   // Spawn Prefab Wall
                tempwall.GetComponent<MeshRenderer>().material.color = WallColor;
                tempwall.transform.parent = MazeWallsContainer.transform;    // Sets all Walls as Child of MazeWall
            }
        }

        // For Horizontal Walls
        for (int i = 0; i <= iSizeY; i++)           // The Y-Position of the Y-Wall
        {                                           // building up the maze structure by proceeding step for step (basically shifting one wall distance each iteration) 
            for (int j = 0; j < iSizeX; j++)        // The X-Position of the Y-Wall
            {
                MazeCreationCurrentPos = new Vector3(MazeCreationStartingPos.x + (j * fWallLength), 0f, MazeCreationStartingPos.z + (i * fWallLength) - fWallLength); // create squares of walls essentially to get maze structure

                tempwall = Instantiate(WallPrefabCorrectSize, MazeCreationCurrentPos, Quaternion.Euler(0f, 90f, 0f)) as GameObject;   // Spawn Prefab Wall
                tempwall.GetComponent<MeshRenderer>().material.color = WallColor;
                tempwall.transform.parent = MazeWallsContainer.transform;    // Sets all Walls as Child of MazeWall
            }
        }

        AllocateWallsToCells();
    }

    void AllocateWallsToCells()                                 // Creates Cells consisting of 4 Walls each and sets cardinal points ( for removing later and creating the actual maze)
    {
        visitedCellsForBackTracking = new List<int>();      // fix NullReferenceError
        visitedCellsForBackTracking.Clear();                // safety I guess

        int children = MazeWallsContainer.transform.childCount;      // basically the wall count
        GameObject[] walls = new GameObject[children];               // Array to hold all walls (size is the ammount of children, so the ammount of all instantiated walls)
        cells = new Cell[iSizeX * iSizeY];                           // Collect all cells -> so they get cardinal points and we can work with them

        GameObject[] PossibleExitWalls = new GameObject[iSizeX];         // All Upper Vertical - Walls of which 1 will be the exit
        GameObject[] PossibleEntranceWalls = new GameObject[iSizeX];     // All Lower Vertical - Walls of which 1 will be the entrance


        for (int i = 0; i < children; i++)                  // Put each individual Wall in Array
        {
            walls[i] = MazeWallsContainer.transform.GetChild(i).gameObject;  // get each wall in walls Array
        }


        for (int cellCount = 0, columnCount = 0, rowCount = 0; cellCount < cells.Length; cellCount++, columnCount++)       // Properly assign cardinal points to each wall of each cell
        {
            cells[cellCount] = new Cell();                           // Create a new Cell with 4 Walls to store wall objects as 'attributes' for each cell

            if (columnCount >= iSizeX)                                  // If the row ends go to new collumn (catch jumps and switch Y basically)
            {
                rowCount++;                 // 1 step forward on the Y-Axis
                columnCount = 0;            // first row in forward collumn
            }

            cells[cellCount].west = walls[cellCount + rowCount];                                // set West - Wall accordingly

            cells[cellCount].east = walls[cellCount + 1 + rowCount];                            // set East - Wall accordingly

            cells[cellCount].south = walls[((iSizeX + 1) * iSizeY) + cellCount];                // set Sotuh - Wall accordingly

            cells[cellCount].north = walls[((iSizeX + 1) * iSizeY) + iSizeX + cellCount];       // set North - Wall accordingly
        }

        #region Destroy Entrance/Exit Wall and get Starting/Goal Location

        // Get Possible Entrance and Exit Walls in Array to destroy 1 of each and open the maze
        for (int i = 0; i < iSizeX; i++)    // Gets all lower outer horizontal walls
        {
            PossibleEntranceWalls[i] = walls[(iSizeX + 1) * iSizeY + i];
            PossibleEntranceWalls[i].GetComponent<MeshRenderer>().material.color = Color.blue;    // checking if correct walls get set ( just for debugging)    
        }

        for (int i = 0; i < iSizeX; i++)
        {
            PossibleExitWalls[i] = walls[(iSizeX + 1) * iSizeY + (iSizeX * iSizeY) + i];
            PossibleExitWalls[i].GetComponent<MeshRenderer>().material.color = Color.green;         // checking if correct walls get set ( just for debugging)
        }

        // Choose random walls to destroy
        int randomIndexEntrance = Random.Range(0, iSizeX);
        int randomIndexExit = Random.Range(0, iSizeX);

        //Destroy 1 Walls on each outer line
        if (PossibleEntranceWalls[randomIndexEntrance])
            Destroy(PossibleEntranceWalls[randomIndexEntrance]);

        if (PossibleExitWalls[randomIndexExit])
            Destroy(PossibleExitWalls[randomIndexExit]);

        #endregion

        #region NavMeshPositioning

        //Checking which Cell the Walls belong to, to set start and exit position
        for (int i = 0; i < cells.Length; i++)
        {
            if (cells[i].south == PossibleEntranceWalls[randomIndexEntrance])
                NavMeshStartingPosition = cells[i].south.transform.position + new Vector3(0f, -.4f, .25f);

            if (cells[i].north == PossibleExitWalls[randomIndexExit])
                NavMeshGoalPosition = cells[i].north.transform.position + new Vector3(0f, -.4f, -.25f);
        }

        #endregion

        if (!bShowMazeCreation)
            CreateMaze();                               // if func without delay
        else
            StartCoroutine(CreateMazeCouroutine());     // if IEnum for delay
    }

    void GotoNextCell()                                         // Checks Neighbour Cells of each cell (just gets surrounding cells) and randoms one
    {
        int[] neighbours = new int[4];              // max 4 neighbourcells
        int[] connectedWalls = new int[4];          // every cell consists of 4 walls
        int count = 0;                              // essentially just the cardinal points as numbers (gets counted up after each 'neighbour check')

        for (int cellPos = 0; cellPos < cells.Length; cellPos++)
        {
            if (cellPos == iCurrentCell + iSizeX && iCurrentCell < iSizeX * iSizeY - iSizeX)        // Checks for NORTH neighbour (north neighbour is just 1 row above so +xSize and it cant be in the top row)
            {
                if (cells[cellPos].bVisited == false)
                {
                    neighbours[count] = cellPos;            // store the neighbourcell at countX

                    if (neighbours[count] > iSizeX * iSizeY - 1 || neighbours[count] < 0)
                        Debug.Log("NORTH: " + neighbours[count] + "with count=" + count + " with current=" + iCurrentCell);

                    connectedWalls[count] = 1;              // save that the stored Wall at countX is a North Wall
                    count++;                                // increment count to save next wall at next index
                }

            }

            if (cellPos == (iCurrentCell - 1) && IsSameRow(cellPos))                                // Check for WEST neighbour (west neighbour is just 1  cell to the left, but has to be on the same row)
            {
                if (cells[cellPos].bVisited == false)
                {
                    neighbours[count] = cellPos;            // store the neighbourcell at countX

                    if (neighbours[count] > iSizeX * iSizeY - 1 || neighbours[count] < 0)
                        Debug.Log("WEST: " + neighbours[count] + "with count=" + count + " with current=" + iCurrentCell);

                    connectedWalls[count] = 2;              // save that the stored Wall at countX is a West Wall
                    count++;                                // increment count to save next wall at next index
                }
            }

            if (cellPos == (iCurrentCell + 1) && IsSameRow(cellPos))                                // Check for EAST neighbour (east neighbour is just 1 cell to the right, but has to be on the same row)
            {
                if (cells[cellPos].bVisited == false)
                {
                    neighbours[count] = cellPos;            // store the neighbourcell at countX

                    if (neighbours[count] > iSizeX * iSizeY - 1 || neighbours[count] < 0)
                        Debug.Log("EAST: " + neighbours[count] + "with count=" + count + " with current=" + iCurrentCell);

                    connectedWalls[count] = 3;              // save that the stored Wall at countX is a East Wall
                    count++;                                // increment count to save next wall at next index
                }
            }

            if (cellPos == iCurrentCell - iSizeX && iCurrentCell > iSizeX - 1)                      // Checks for SOUTH neighbour (south neighbour is just 1 row below so -xSize and it cant be in the bottom row)
            {
                if (cells[cellPos].bVisited == false)
                {
                    neighbours[count] = cellPos;            // store the neighbourcell at countX

                    if (neighbours[count] > iSizeX * iSizeY - 1 || neighbours[count] < 0)
                        Debug.Log("SOUTH: " + neighbours[count] + "with count=" + count + " with current=" + iCurrentCell);

                    connectedWalls[count] = 4;              // save that the stored Wall at countX is a South Wall
                    count++;                                // increment count to save next wall at next index
                }
            }

        }

        if (count != 0)         // if there is an unvisited neighbour basically
        {
            int randomnumber = Random.Range(0, count);      // get a random number
            iCurrentNeighbour = neighbours[randomnumber];   // get a random neighbour

            if (iCurrentNeighbour > iSizeX * iSizeY - 1 || iCurrentNeighbour < 0)       // For Debugging purposes
                Debug.Log(iCurrentNeighbour);

            iWallDirectionToBreak = connectedWalls[randomnumber];    // chose a random wall to break (North, East, West or South)
        }

        else if (iBackTrackIndex > 0)    // if there is no neighbour found and there are cells to backtrack to
        {
            //Debug.Log("BACKUP @ cell=" + iCurrentCell);

            iCurrentCell = visitedCellsForBackTracking[iBackTrackIndex];   // go to the last visited cell and make it current cell

            if (iCurrentCell > iSizeX * iSizeY - 1 || iCurrentNeighbour < 0)
                Debug.Log(iCurrentCell);

            iBackTrackIndex--;   // decrement BackingupValue to further backtrack on next iteration if there is no unvisited neighbour again
        }
    }

    void CreateMaze()                                           // Actually building the Maze by removing walls and creating a path through
    {
        int iTotalCells = iSizeX * iSizeY;                              // max ammount is X * Y 

        while (iVisitedCells < iTotalCells)                             // As long as there are unvisited cells
        {
            if (bStartedBuilding)
            {
                GotoNextCell();                                         // Gets a Random Neighbour to visit/break
                if (cells[iCurrentNeighbour].bVisited == false && cells[iCurrentCell].bVisited == true)
                {
                    //Debug.Log("'CreateMaze' after get neighbour if");
                    BreakWall();
                    cells[iCurrentNeighbour].bVisited = true;           // the Neighbour, whose wall got broken, gets marked as visited
                    iVisitedCells++;                                    // visitedcells count increment to have break condition for recursion
                    visitedCellsForBackTracking.Add(iCurrentCell);      // Add visited Cell to List for backtracking
                    //Debug.Log(iCurrentCell);
                    iCurrentCell = iCurrentNeighbour;                   // go to neighbour cell and get its neighbour again to proceed further with the maze

                    if (visitedCellsForBackTracking.Count > 0)          // For every Cell added to the List the index gets incremented to always have the last visited cell available for backtrack
                    {
                        iBackTrackIndex = visitedCellsForBackTracking.Count - 1;    // -1 since you want the last cell and not the current cell
                        //Debug.Log("backup changed" + iBackingUp);
                    }
                }
            }
            else                                                        // just gets executed once to get a random start position to start the recursion
            {
                iCurrentCell = Random.Range(0, iTotalCells);            // Get a Random starting Cell
                cells[iCurrentCell].bVisited = true;                    // mark startint cell as visited to not go here again
                iVisitedCells++;                                        // visitedcells count increment to have break condition for recursion
                bStartedBuilding = true;                                // Start the 'building process'
            }

            Invoke("CreateMaze", 0f);                                   // recursion
        }
        if (iVisitedCells == iTotalCells)                               // as soon as the wall breaking stops we can create the wall
            CreateFloor();                                              // if executed earlier it sometimes gets destroyed aswell ??

    }

    IEnumerator CreateMazeCouroutine()                          // Same as func ( just for showing maze creation process)
    {
        int iTotalCells = iSizeX * iSizeY;                              // max ammount is X * Y 

        while (iVisitedCells < iTotalCells)                             // As long as there are unvisited cells
        {
            if (bStartedBuilding)
            {
                GotoNextCell();                                         // Gets a Random Neighbour to visit/break
                if (cells[iCurrentNeighbour].bVisited == false && cells[iCurrentCell].bVisited == true)
                {
                    //Debug.Log("Line 183 after get neighbour if");
                    BreakWall();
                    cells[iCurrentNeighbour].bVisited = true;           // the Neighbour, whose wall got broken, gets marked as visited
                    iVisitedCells++;                                    // visitedcells count increment to have break condition for recursion
                    visitedCellsForBackTracking.Add(iCurrentCell);      // Add visited Cell to List for backtracking
                    //Debug.Log(iCurrentCell);
                    iCurrentCell = iCurrentNeighbour;                   // go to neighbour cell and get its neighbour again to proceed further with the maze

                    if (visitedCellsForBackTracking.Count > 0)          // For every Cell added to the List the index gets incremented to always have the last visited cell available for backtrack
                    {
                        iBackTrackIndex = visitedCellsForBackTracking.Count - 1;    // -1 since you want the last cell and not the current cell
                        //Debug.Log("backup changed" + iBackingUp);
                    }
                }
            }
            else                                                        // just gets executed once to get a random start position to start the recursion
            {
                iCurrentCell = Random.Range(0, iTotalCells);            // Get a Random starting Cell
                cells[iCurrentCell].bVisited = true;                    // mark startint cell as visited to not go here again
                iVisitedCells++;                                        // visitedcells count increment to have break condition for recursion
                bStartedBuilding = true;                                // Start the 'building process'
            }

            yield return new WaitForSeconds(fMazeCreationSpeed);        // Delay between 'wall breaking' to follow the process  (Dont remove this and keep the return on next line -> crashes)
            yield return CreateMazeCouroutine();                        // recursion

        }
        if (iVisitedCells == iTotalCells)                               // as soon as the wall breaking stops we can create the wall
            CreateFloor();                                              // if executed earlier it sometimes gets destroyed aswell ??
    }

    bool IsSameRow(int cellPos)                                 // Checks wether or not the cell that is being checked is on the same row as the starting cell
    {
        int iRowOfStartingCell = -666;        // dummy values (just to have a not null value to check if something has changed)
        int iRowOfCellToCheck = -666;         // dummy values (just to have a not null value to check if something has changed)

        for (int cellcount = 0, rowcount = 0; cellcount < iSizeX * iSizeY; cellcount += iSizeX, rowcount++)         // iterates through all cells and rows
        {
            if (iCurrentCell >= cellcount && iCurrentCell <= cellcount + iSizeX - 1)                                // checks on which row the StartingCell is positioned
                iRowOfStartingCell = rowcount;                                                                      // if row is found, store it

            if (cellPos >= cellcount && cellPos <= cellcount + iSizeX - 1)                                          // checks on which row the cell that is being checked is
                iRowOfCellToCheck = rowcount;                                                                       // if row is found, store it

            if (iRowOfStartingCell != -666 && iRowOfCellToCheck != -666)                                            // When both rows got found
            {
                if (iRowOfStartingCell == iRowOfCellToCheck)                                                        // Check wether its the same row or not
                    return true;

                else
                    return false;
            }
        }
        return false;
    }

    void BreakWall()                                            // Destroy randomized walls
    {
        //Debug.Log("Breaking!");
        //Debug.Log("Breaking: current=" + iCurrentCell  + "wall:" + iWallToBreak);


        switch (iWallDirectionToBreak)  // cardinal points as numbers to choose a wall
        {
            case 1: Destroy(cells[iCurrentCell].north); break;
            case 2: Destroy(cells[iCurrentCell].west); break;
            case 3: Destroy(cells[iCurrentCell].east); break;
            case 4: Destroy(cells[iCurrentCell].south); break;
        }
    }

    void CreateFloor()                                          // Creates a Floor below the Maze
    {
        if (!bGroundCreated)        // only create ground once
        {
            if (!bUsePrebakedGround)    // only create ground if there is no prebaked for navmesh
            {
                Vector3 position = new Vector3(MazeCreationStartingPos.x, -0.5f, MazeCreationStartingPos.z);    // initial try of correct positioning (doesnt work at all)

                if (fWallLength == 1f)
                {
                    if (iSizeX % 2 != 0 && iSizeY % 2 != 0)             // X = odd  &  Y = odd
                    {
                        position = new Vector3(.5f, -.5f, 0f);        // correct pos for this case somehow
                    }
                    else if (iSizeX % 2 == 0 && iSizeY % 2 == 0)        // X = even  &  Y = even
                    {
                        position = new Vector3(0f, -.5f, -.5f);     // correct pos for this case somehow
                    }
                    else if (iSizeX % 2 != 0 && iSizeY % 2 == 0)        // X = odd  &  Y = even
                    {
                        position = new Vector3(.5f, -.5f, -.5f);     // correct pos for this case somehow
                    }
                    else if (iSizeX % 2 == 0 && iSizeY % 2 != 0)        // X = even  &  Y = odd
                    {
                        position = new Vector3(0f, -.5f, 0f);          // correct pos for this case somehow
                    }

                }
                else if (fWallLength == 2f)
                {
                    if (iSizeX % 2 != 0 && iSizeY % 2 != 0)             // X = odd  &  Y = odd
                    {
                        position = new Vector3((iSizeX / 2) + .5f, -.5f, (iSizeX / 2) - .5f);
                    }
                    else if (iSizeX % 2 == 0 && iSizeY % 2 == 0)        // X = even  &  Y = even
                    {
                        position = new Vector3(iSizeX / 2, -.5f, (iSizeX / 2) - 1);
                    }
                    else if (iSizeX % 2 != 0 && iSizeY % 2 == 0)        // X = odd  &  Y = even
                    {
                        position = new Vector3(iSizeX / 2 + 1, -.5f, (iSizeY / 2) - 1);
                    }
                    else if (iSizeX % 2 == 0 && iSizeY % 2 != 0)        // X = even  &  Y = odd
                    {
                        position = new Vector3(iSizeX / 2, 0f, (iSizeY / 2) - .5f);
                    }
                }

                ground = Instantiate(WallPrefabCorrectSize, position, Quaternion.Euler(0, -.5f, 90)) as GameObject;   // Instantiate Wall Prefab, at correct location and flip it

                if (fWallLength == 2f)
                    ground.transform.localScale = new Vector3(ground.transform.localScale.x, iSizeX * 2, iSizeY * 2);   // fix the size accordingly ( For WallLength = 2)
                else
                    ground.transform.localScale = new Vector3(ground.transform.localScale.x, iSizeX, iSizeY);   // fix the size accordingly ( For WallLength = 1)

                ground.name = "Ground";                                                                     // name it accordingly

                Destroy(ground.GetComponent<NavMeshObstacle>());                                            // wall prefab got Obstacle Component (maybe create new prefab for ground)
                GameObjectUtility.SetStaticEditorFlags(ground, StaticEditorFlags.NavigationStatic);         // Set Navigation static for baking

                ground.GetComponent<MeshRenderer>().material.color = GroundColor;                           // color it black to distinguish better between wall and ground

                bGroundCreated = true;                                                                      // to prevent multiple layers of the ground

                if (bUseMaterialInsteadOfColor)
                    SetColorAndMaterial();
            }

            NavMeshPositioning();
            FPPositioning();
        }

        if (!bDebugIssued)
        {
            Debug.Log("done");
            bDebugIssued = true;
        }
    }

    void FPPositioning()                                        // Set correct Pos of FirstPersonModel and Goal (just visual unlike NavMesh)
    {
        FirstPersonGoal.GetComponent<MeshRenderer>().material.color = Color.red;    // color the goal red for visibility

        FirstPersonModel.transform.position = NavMeshStartingPosition;
        FirstPersonGoal.transform.position = NavMeshGoalPosition;
    }

    void NavMeshPositioning()                                   // Placing agent and goal at correct pos
    {
        NavMeshDestination.transform.position = NavMeshGoalPosition;
        NavMeshAgent.Warp(NavMeshStartingPosition);             // Warp instead of SetPosition to prevent NavMesh errors

        //Debug.Log("Goal Pos. : " + NavMeshGoalPosition);
        //Debug.Log("Start Pos. : " + NavMeshStartingPosition);
    }

    void SetColorAndMaterial()                                  // Give Walls and Ground a Texture (prefab no material workaround) -> find better solution
    {
        ground.GetComponent<MeshRenderer>().material = groundMaterial;

        int children = MazeWallsContainer.transform.childCount;
        GameObject[] allwalls = new GameObject[children];

        for (int i = 0; i < children; i++)                  // Put each individual Wall in Array
        {
            allwalls[i] = MazeWallsContainer.transform.GetChild(i).gameObject;  // get each wall in walls Array
        }


        foreach (var wall in allwalls)
        {
            wall.GetComponent<MeshRenderer>().material = wallMaterial;  // set wall material
        }

        NavMeshDestination.GetComponent<MeshRenderer>().material.color = Color.red;     // Color the goal red for visibility I guess
    }
}
