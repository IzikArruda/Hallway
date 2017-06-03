using UnityEngine;
using System.Collections;

/* ---------- Common Game Objects ------------------------------------------------------------------------------ */

/* An object that has an "active" variable to allow easy removal */
public class ActiveObject {
    public GameObject gameObject = null;
    public bool active = true;
}

/* Coordinates for a position on a 2D plane */
public class Coords {
    public float x, y;
}

/* Values used with a single photon shot */
public class Photon : ActiveObject {
    public float x, y, dx, dy, size;
}

/* All pertinent values used for tracking a ship */
public class Ship {
    public GameObject gameObject = null;
    public float x, y, dx, dy, size, phi, speed, control, turnSpeed;
    public float flickerRate, currentFlicker;
    public int damaged;
}

/* Static dots in the background of the asteroids game that flicker between 100-0% opacity at varying rates */
public class BackgroundStar {
    public GameObject gameObject = null;
    public float x, y, flicker, flickerRate;
}

/* An asteroid for the game. Contains coordinates for each of it's vertices that make up the asteroid */
public class Asteroid : ActiveObject {
    public int nVertices;
    public float x, y, phi, dx, dy, dphi, size;
    public Coords[] coords;
}

/* Values used to define a dot that moves in one direction */
public class Dust : ActiveObject {
    public float x, y, dx, dy, lifetime;
}

/* A triangle that rotates and moves around the map */
public class Debris : ActiveObject {
    public float x, y, i, dx, dy, size, lifetime;
    public float phi, delta, dphi, ddelta;
    public scoreGain linkedScore = null;
    //public Coords[] coords;
}

/* An object which tracks how much time is left on a debris and displays it using OpenGL draw commands */
/* When the player gains score, use this object to show how much they gained and where */
public class scoreGain : ActiveObject {
    public int score;
    public float x, y, dy, lifetime;
}

/*
 * Handles a 2D asteroids game drawn using GL.Draw commands. When the player has control of the game,
 * they can move the ship using the WASD keys and fire photons with left-click. The game runs
 * inpependant of the player, I.E it updates on it's own and accepts inputs at the player's update rate.
 * 
 * The asteroids canvas is 1.55 units wide and 2.25 high. Canvas starts at (2, 0, 2.4) and ends at (2, 2.25, 3.95).
 * The play field starts at (1.25f, 2.4f) and ends at (2.25f, 3.95f).
 */
public class AsteroidsGame : MonoBehaviour {


    /* ---------- Helper Variables --------------------------------------------------------------------------- */
    private System.Random rand = new System.Random();
    private float PI = Mathf.PI;


    /* ---------- Transmission Variables --------------------------------------------------------------------------- */
    /* The timing of each animation state when handling the transmission box animations.
     * 0 - StartWait: Wait before starting the animation
     * 1 - TransLabel: The label of the transmission box is entering the scene
     * 2 - TransBox: The transmission messages' box animation starts 
     * 3 - Wait1: Do nothing
     * 4 - StaticBox: The static box animates into the scene and it's static is drawn starting here 
     * 5 - StaticLabel: The static label is typed out 
     * 6 - TransMessage: The transmission message is typed out 
     * 7 - Finished: Do nothing and loop this stage */
    private int[] transTextStateTimings = { 50, 35, 25, 15, 20, -1, -1, 0};
    private enum transTextStates {
        StartWait = 0,
        TransLabel = 1,
        TransBox = 2,
        Wait1 = 3,
        StaticBox = 4,
        StaticLabel = 5,
        TransMessage = 6,
        Finished = 7
    }

    /* The current state of the transmission animation the player is in and the time spent in it */
    int currentTransAnimState;
    float currentTransAnimTime;

    /* How much time it takes between letters to be typed */
    float typingTimeInterval = 20;

    /* A prefab of a gameObject with a textMesh that is used for in-world text */
    public GameObject textObjectPrefab;
    public Font textObjectFont;
    public float textSize;

    /* A large set of random 0s and 1s used when drawing the static */
    int[] staticPool;

    /* The distance between each static line in the static window */
    float staticLineSeperator;

    /* Transmission Label bellow the transmission text box */
    TextMesh transLabel;
    string transLabelText = "Incomming Transmission";
    Vector3 transLabelStart, transLabelEnd;

    /* labels the static box. Placed above the trans box and to the right of the static box. */
    TextMesh staticLabel;
    string staticLabelText = "?????";
    float staticLabelWidth, staticLabelHeight, staticLabelSpacing;

	/* Values used with the message of the transmission */
	TextMesh transMessage;
	string transmissionMessage = "This is the transmission message that will be line broken. lets see how it looks on a new line";

    /* Transmission Box used as a background for the transmission message along with it's sizes */
    GameObject transBox;
    float transBoxWidth;
    float transBoxWidthRatio;
    float transBoxHeight;
    float transBoxHeightRatio;
    float transBoxTop;
    float transBoxBottom;
    float transBoxRight;
    float transBoxLeft;
    
    /* Static window is a black mesh that has openGL draw calls infront of it to emulate static */
    GameObject staticBox;
    float staticBoxBottom;
    float staticBoxLeft;
    float staticBoxWidth;
    float staticBoxHeight;
    float currentStaticBoxBottom;
    float currentStaticBoxLeft;
    float currentStaticBoxWidth;
    float currentStaticBoxHeight;



    /* ---------- World Drawing Variables ------------------------------------------------------------- */
    /* Materials needed when drawing the game */
    public Shader AsteroidsShader;
    public Material OpenGLMaterial;
    public Material OpenGL;
    public Material White;
    public Material Black;
    public Material Blue;

    /* The limited play area of the asteroids game */
    public float z;
    public float xMax;
    public float xMin;
    public float xWidth;
    public float yMax;
    public float yMin;
    public float yHeight;

    /* How much depth a layer has. Changes how much the drawn objects protrude from the canvas */
    public float layerSize = 0.01f;


    /* ---------- Input Variables --------------------------------------------------------------------------- */
    /* The state of the keys used to control the asteroids game */
    int up = 0;
    int down = 0;
    int left = 0;
    int right = 0;
    //0 = ready to fire. 1 = firing shot. 2 = holding firing button, wont fire
    int firePhoton = 0;


    /* ---------- Container/Tracker ArrayLists/Variables  ------------------------------------------------- */
    ArrayList freeScoreGains = new ArrayList();
    ArrayList debris = new ArrayList();
    ArrayList photonResidue = new ArrayList();
    ArrayList dustParticles = new ArrayList();
    ArrayList backgroundStars = new ArrayList();
    ArrayList asteroids = new ArrayList();
    ArrayList photons = new ArrayList();
    Ship ship = new Ship();



    /* ---------- Asteroid and Photon Parameters ---------------------------------------------------------- */
    /* The max and minimum variance in length an asteroid's vertex can be from it's center */
    float asteroidVarianceMax = 3f;
    float asteroidVarianceMin = 2f;

    /* The minimum size an asteroids can have before fully being destroyed */
    float asteroidSizeMin = 0.02f;

    /* The max and min velocity an asteroid can travel along one axis */
    float asteroidSpeedMax = 0.025f;
    float asteroidSpeedMin = 0.00075f;

    /* the initial speed of a photon once fired */
    float photonSpeed = 0.01f;


    /* ---------- Game State Variables --------------------------------------------------------------------------- */

    /* The state the asteroids game is in. It saved between unloads and player unliking
     * Inactive: The player has not interacted with the game yet.
     * AsteroidStage: The player is playing the game and is expected to finish it
     * PausedAsteroid: The player left the game and did not finish it
     * Transmission: The player destroyed all the asteroids and will recieve a transmission */
    int gameState = (int) State.Inactive;
    enum State {
        Inactive,
        AsteroidStage,
        PausedAsteroid,
        TransmissionAnimation,
        Finished
    }


    /* -------- Built-in Unity Functions ------------------------------------------------------- */

    void Start() {
        /*
         * Set the starting values of the objects used in the asteroids game
         */
        CalculateCanvasSize();

        /* Set up the materials that will be used to draw the game */
        InitColors();

        /* Set the ship's starting stats */
        InitShip();

        /* Create a starry background as a playing field */
        InitBackground();

        //StartGame();
        //AsteroidsComplete();
    }

    void CalculateCanvasSize() {
        /*
         * Calculate the size of the asteroid canvas using the box collider attached to the same gameObject
         * this script is attached to. It takes into account the center and size values of the collider.
         */

        float xOffset = GetComponent<BoxCollider>().center.x;
        xMin = xOffset;
        xMax = xOffset + GetComponent<BoxCollider>().size.x;
        xWidth = (xMax - xMin);
        xMin -= xWidth/2f;
        xMax -= xWidth/2f;

        float yOffset = GetComponent<BoxCollider>().center.y;
        yMin = yOffset;
        yMax = yOffset + GetComponent<BoxCollider>().size.y;
        yHeight = (yMax - yMin);
        yMin -= yHeight/2f;
        yMax -= yHeight/2f;

        z = 0;
    }

    void OnRenderObject() {
        /* 
         * Draw the asteroids game when the object linked to it is rendered
         */
         
        DrawGame();
    }

    void Update() {
        /*
         * Run this Update function every frame. Depending on the game state and the pause state,
         * only certain functions will be run every frame.
         */

        /* The game is waiting for a player to give it an input */
        if(gameState == (int) State.Inactive) {
            if(UpdatePauseState()) {
                /* Unpausing to start the game for the first time */
                StartGame();
            }
        }

        /* The user has control over the game */
        if(gameState == (int) State.AsteroidStage || gameState == (int) State.TransmissionAnimation || gameState == (int) State.Finished) {
            UpdateBackground();
            UpdateShip();
            UpdatePhotons();
            UpdateAsteroids();
            UpdateDust();
            UpdatePhotonResidue();
            UpdateDebris();
            UpdateScoreGain();
            CollisionAsteroidPhoton();
            CollisionAsteroidShip();
            CollisionDebrisShip();
        }

        /* Check if the user as cleared the area of asteroids and debris to advance to the transmission stage */
        if(gameState == (int) State.AsteroidStage) {
            AsteroidsComplete();
        }

        /* The user left the game, so wait for user input before starting again */
        if(gameState == (int) State.PausedAsteroid) {
            if(UpdatePauseState()) {
                /* Unpause to let the player continue where they left off */
                gameState = (int) State.AsteroidStage;
            }
        }

        /* The user will be waiting for the transmission text to finish appearing */
        if(gameState == (int) State.TransmissionAnimation) {
            UpdateTransmission();
        }
    }
    
    void DrawGame() {
        /*
         * Run a set of functions that will draw the rest of the asteroids game using OpenGL commands. 
         */

        /* Draw parts of the asteroid game using OpenGL Draw commands */
        DrawBackgroundStars(0);
        DrawPhotonResidue(1);
        DrawDust(2);
        DrawScoreGain(3);

        /* Draw the static box's static if the game is in the proper transmission state or has finished the animation */
        if((currentTransAnimState >= (int)transTextStates.StaticBox && gameState == (int) State.TransmissionAnimation)
            || gameState == (int) State.Finished) {
            DrawTransmissionStaticBox(5);
        }
    }

    /* -------- OpenGL Drawing Functions ------------------------------------------------------- */
    
    void DrawBackgroundStars(int layer) {
        /*
         * Draw each star in the background as a set of short lines seeing that GL cannot draw points
         */
        Vector3 starWorldPosition;

        OpenGL.SetPass(0);
        GL.Begin(GL.LINES);
        foreach(BackgroundStar star in backgroundStars) {
            starWorldPosition = star.gameObject.transform.position;
            GL.Color(new Color(255f, 255f, 255f, Mathf.Sin(star.flicker)));
            GL.Vertex3(starWorldPosition.x + layerSize/50f, starWorldPosition.y, starWorldPosition.z);
            GL.Vertex3(starWorldPosition.x - layerSize/50f, starWorldPosition.y, starWorldPosition.z);

            GL.Vertex3(starWorldPosition.x, starWorldPosition.y + layerSize/50f, starWorldPosition.z);
            GL.Vertex3(starWorldPosition.x, starWorldPosition.y - layerSize/50f, starWorldPosition.z);

            GL.Vertex3(starWorldPosition.x, starWorldPosition.y, starWorldPosition.z + layerSize/50f);
            GL.Vertex3(starWorldPosition.x, starWorldPosition.y, starWorldPosition.z - layerSize/50f);
        }
        GL.End();
    }
    
    void DrawDust(int layer) {
        /*
         * Draw all dust particles as a short line protruding from the background.
         * The less lifetime a dust has, the more opacity it will have.
         */
        Vector3 dustWorldPosition;

        OpenGL.SetPass(0);
        GL.Begin(GL.LINES);
        foreach(Dust dust in dustParticles) {
            dustWorldPosition = dust.gameObject.transform.position;
            GL.Color(new Color(1, 1, 1, (dust.lifetime)));
            GL.Vertex3(dustWorldPosition.x + layerSize/50f, dustWorldPosition.y, dustWorldPosition.z);
            GL.Vertex3(dustWorldPosition.x - layerSize/50f, dustWorldPosition.y, dustWorldPosition.z);

            GL.Vertex3(dustWorldPosition.x, dustWorldPosition.y + layerSize/50f, dustWorldPosition.z);
            GL.Vertex3(dustWorldPosition.x, dustWorldPosition.y - layerSize/50f, dustWorldPosition.z);

            GL.Vertex3(dustWorldPosition.x, dustWorldPosition.y, dustWorldPosition.z + layerSize/50f);
            GL.Vertex3(dustWorldPosition.x, dustWorldPosition.y, dustWorldPosition.z - layerSize/50f);
        }
        GL.End();
    }

    void DrawPhotonResidue(int layer) {
        /*
         * Draw all photon residue particles as a short line protruding from the background.
         * The less lifetime a residue has, the more opacity it will have.
         */
        Vector3 residueWorldPosition;

        OpenGL.SetPass(0);
        GL.Begin(GL.LINES);
        foreach(Dust residue in photonResidue) {
            residueWorldPosition = residue.gameObject.transform.position;
            GL.Color(new Color(0, 0, 1, (residue.lifetime)));
            GL.Vertex3(residueWorldPosition.x + layerSize/50f, residueWorldPosition.y, residueWorldPosition.z);
            GL.Vertex3(residueWorldPosition.x - layerSize/50f, residueWorldPosition.y, residueWorldPosition.z);

            GL.Vertex3(residueWorldPosition.x, residueWorldPosition.y + layerSize/50f, residueWorldPosition.z);
            GL.Vertex3(residueWorldPosition.x, residueWorldPosition.y - layerSize/50f, residueWorldPosition.z);

            GL.Vertex3(residueWorldPosition.x, residueWorldPosition.y, residueWorldPosition.z + layerSize/50f);
            GL.Vertex3(residueWorldPosition.x, residueWorldPosition.y, residueWorldPosition.z - layerSize/50f);
        }
        GL.End();
    }
    
    void DrawScoreGain(int layer) {
        /*
         * Draw all the scoreGain objects in the game using GLDraw commands. The difference between linked
         * scoreGains and free ones are their score amount that gets displayed and color.
         */
        Color scoreColor;

        //Draw the free scoreGains
        foreach(scoreGain s in freeScoreGains) {
            //DrawFreeScoreGain(s, s.score, Color.green);
            DrawScoreGain(s, s.score, Color.green);
        }

        //Draw the scoreGains still linked to their debris
        foreach(Debris d in debris) {
            scoreColor = new Color(1, d.lifetime/35f, d.lifetime/35f, 1);
            DrawScoreGain(d.linkedScore, CalculateDebrisScore(d), scoreColor);
        }
    }
    
    void DrawScoreGain(scoreGain s, float number, Color color) {
        /*
         * Draw the score of the given scoreGain object using OpenGL commands.
         * Number is the given number to be drawn using the given color.
         */
        Vector3 start, newStart;
        float xOffset;
        int[] digits;
        int digitCount = 0;

        /* Get the positions of the score in the world */
        Vector3 worldPosition = s.gameObject.transform.position;
        Vector3 xTranslation = transform.TransformPoint(s.gameObject.transform.localPosition + new Vector3(1, 0, 0)) - worldPosition;
        Vector3 yTranslation = transform.TransformPoint(s.gameObject.transform.localPosition + new Vector3(0, 1, 0)) - worldPosition;
        
        /* Get the sizes and spacing of the digits */
        float charWidth = textSize*xWidth;
        float charHeight = textSize*2*yHeight;
        float charSpacing = charWidth/2f;

        /* Get the amount of digits that will be drawn */
        digitCount++;
        while(number >= 10) {
            number /= 10;
            digitCount++;
        }

        /* Create an array that holds all the digits to draw */
        digits = new int[digitCount];
        for(int i = 0; i < digitCount; i++) {
            digits[i] = Mathf.FloorToInt(number%10f);
            number *= 10;
        }

        /* Calculate how much space the text will take to find where to start drawing it to center the text */
        start = worldPosition;
        //Adjust the x value to properly center it
        start -= xTranslation*(((digitCount)*charWidth + (digitCount-1)*charSpacing)/2f);
        //Raise the y value so it is not overlapping the debris
        start += yTranslation*charHeight;

        //Draw a line from the  start to the center to make sure its in the proper position
        GL.Begin(GL.LINES);
        GL.Color(color);
        for(int i = 0; i < digitCount; i++) {
            xOffset = ((i+1)*charWidth + i*charSpacing);
            newStart = start + xTranslation*xOffset;
            DrawDigitalNumber(digits[i], newStart, xTranslation, yTranslation, charWidth, charHeight);
        }
        GL.End();
    }

    void DrawDigitalNumber(int d, Vector3 start, Vector3 xTran, Vector3 yTran, float width, float height) {
        /*
         * Use GL.Vertex3 calls to draw the given integer between 0 and 9. 
         * Assumes a GL.Begin(Lines) is already called. 
         * 
         * Drawing a "digital number" means emulating the look of a digital clock:
         * There are seven lines, each used by varies numbers.
         */
        Vector3 point1, point2, digitWidth, digitHeight;
        digitWidth = xTran*width;
        digitHeight = yTran*height;

        /* Top-Left */
        if(d == 0 || d == 4 || d == 5 || d == 6 || d == 8 || d == 9 || d == 0) {
            point1 = start + digitHeight;
            point2 = start + digitHeight/2f;
            GL.Vertex3(point1.x, point1.y, point1.z);
            GL.Vertex3(point2.x, point2.y, point2.z);
        }

        /* Top-Middle */
        if(d == 0 || d == 2 || d == 3 || d == 5 || d == 6 || d == 7 || d == 8 || d == 9 || d == 0) {
            point1 = start + digitHeight;
            point2 = start + digitHeight + digitWidth;
            GL.Vertex3(point1.x, point1.y, point1.z);
            GL.Vertex3(point2.x, point2.y, point2.z);
        }

        /* Top-Right */
        if(d == 0 || d == 1 || d == 2 || d == 3 || d == 4 || d == 7 || d == 8 || d == 9 || d == 0) {
            point1 = start + digitHeight + digitWidth;
            point2 = start + digitHeight/2f + digitWidth;
            GL.Vertex3(point1.x, point1.y, point1.z);
            GL.Vertex3(point2.x, point2.y, point2.z);
        }

        /* Middle */
        if(d == 2 || d == 3 || d == 4 || d == 5 || d == 6 || d == 8 || d == 9) {
            point1 = start + yTran*height/2f;
            point2 = start + yTran*height/2f + xTran*width;
            GL.Vertex3(point1.x, point1.y, point1.z);
            GL.Vertex3(point2.x, point2.y, point2.z);
        }

        /* Bottom-Left */
        if(d == 0 || d == 2 || d == 6 || d == 8 || d == 0) {
            point1 = start;
            point2 = start + digitHeight/2f;
            GL.Vertex3(point1.x, point1.y, point1.z);
            GL.Vertex3(point2.x, point2.y, point2.z);
        }

        /* Bottom line */
        if(d == 0 || d == 2 || d == 3 || d == 5 || d == 6 || d == 8 || d == 9 || d == 0) {
            point1 = start;
            point2 = start + digitWidth;
            GL.Vertex3(point1.x, point1.y, point1.z);
            GL.Vertex3(point2.x, point2.y, point2.z);
        }

        /* Bottom-Right */
        if(d == 0 || d == 1 || d == 3 || d == 4 || d == 5 || d == 6 || d == 7 || d == 8 || d == 9 || d == 0) {
            point1 = start + digitWidth;
            point2 = start + digitHeight/2f + digitWidth;
            GL.Vertex3(point1.x, point1.y, point1.z);
            GL.Vertex3(point2.x, point2.y, point2.z);
        }
    }

    void DrawTransmissionStaticBox(int layer) {
        /*
         * Draw the static box
         * 
         * Each transAnimState value has different timings taken from transTextStateTimings. Each part of the 
         * transmission box reacts differently to whatever animation state it is currently on.
         */
        Vector3 worldPosition = staticBox.gameObject.transform.position;
        Vector3 xTranslation = transform.TransformPoint(staticBox.gameObject.transform.localPosition + new Vector3(1, 0, 0)) - worldPosition;
        Vector3 yTranslation = transform.TransformPoint(staticBox.gameObject.transform.localPosition + new Vector3(0, 1, 0)) - worldPosition;
        Vector3 width = xTranslation*currentStaticBoxWidth;
        Vector3 height = yTranslation*currentStaticBoxHeight;
        Vector3 start = staticBox.transform.position - width/2 - height/2;
        Vector3 newPoint;
        float lineCount;


        /* Draw a set of random red lines lower part of the window for the person's body */
        lineCount = (width.magnitude + height.magnitude)*400;
        GL.Begin(GL.LINES);
        GL.Color(Color.white);
        for(int i = 0; i < lineCount; i++) {
            newPoint = start + height*Rand(0, 0.45f) + width*Rand(0, 1);
            GL.Vertex3(newPoint.x, newPoint.y, newPoint.z);
            newPoint = start + height*Rand(0, 0.45f) + width*Rand(0, 1);
            GL.Vertex3(newPoint.x, newPoint.y, newPoint.z);
        }
        GL.End();


        /* Draw a set of random lines within a circle slightly above the center of the frame */
        lineCount /= 2f;
        GL.Begin(GL.LINES);
        GL.Color(Color.white);
        Coords randPoint;
        for(int i = 0; i < lineCount; i++) {
            randPoint = RandPointInCircle();
            newPoint = start + height/1.5f + width/2f + randPoint.x*width/4f + randPoint.y*height/3f;
            GL.Vertex3(newPoint.x, newPoint.y, newPoint.z);
            randPoint = RandPointInCircle();
            newPoint = start + height/1.5f + width/2f + randPoint.x*width/4f + randPoint.y*height/3f;
            GL.Vertex3(newPoint.x, newPoint.y, newPoint.z);
        }
        GL.End();


        /* Draw a set of lines that connect the circle to the lower window part */
        lineCount /= 2f;
        GL.Begin(GL.LINES);
        GL.Color(Color.white);
        for(int i = 0; i < lineCount; i++) {
            newPoint = start + height*Rand(0, 0.45f) + width*Rand(0.25f, 0.75f);
            GL.Vertex3(newPoint.x, newPoint.y, newPoint.z);
            randPoint = RandPointInCircle();
            newPoint = start + height/1.75f + width/2f + randPoint.x*width/8f + randPoint.y*height/4f;
            GL.Vertex3(newPoint.x, newPoint.y, newPoint.z);
        }
        GL.End();



        /* Parse the static pool for the layout of the random lines comming from the top to bottom */
        lineCount = width.magnitude/staticLineSeperator;
        //lineCount = 0;
        int currentStaticIndex = Mathf.FloorToInt(Rand(0, staticPool.Length-1));
        GL.Begin(GL.LINES);
        GL.Color(Color.green);
        for(int i = 0; i < lineCount; i++) {
            if(staticPool[currentStaticIndex] == 1) {
                newPoint = start + width*(i/lineCount);
                GL.Vertex3(newPoint.x, newPoint.y, newPoint.z);
                newPoint += height;
                GL.Vertex3(newPoint.x, newPoint.y, newPoint.z);
            }
            /* Increment to the next value in the static pool, looping back to 0 if we pass it's size */
            currentStaticIndex++;
            if(currentStaticIndex >= staticPool.Length) {
                currentStaticIndex = 0;
            }
        }
        GL.End();


        /* Parse the static pool for the random lines comming from the left to right */
        lineCount *= 2;
        //lineCount = 0;
        currentStaticIndex = Mathf.FloorToInt(Rand(0, staticPool.Length-1));
        GL.Begin(GL.LINES);
        GL.Color(Color.green);
        for(int i = 0; i < lineCount; i++) {
            if(staticPool[currentStaticIndex] == 1) {
                newPoint = start + height*(i/lineCount);
                GL.Vertex3(newPoint.x, newPoint.y, newPoint.z);
                newPoint += width;
                GL.Vertex3(newPoint.x, newPoint.y, newPoint.z);
            }
            /* Increment to the next value in the static pool, looping back to 0 if we pass it's size */
            currentStaticIndex++;
            if(currentStaticIndex >= staticPool.Length) {
                currentStaticIndex = 0;
            }
        }
        GL.End();


    }


    /* -------- Mesh Creation functions ------------------------------------------------------- */
    
    void CreateShipMesh(int layer) {
        /*
         * Draw the ship the user will be controlling. The opacity of the ship will be set 
         * by the ship's damaged value. Opacity is set by not assigning a material to the ship
         * 
         * If the ship is damaged, it will flicker in and out of being visible. How fast it flickers
         * is dependent on it's damaged value (odd = invisible). Invisibility is set by disabling the meshRenderer
         */
        Material[] materials;
        Vector3[] shipVertices = new Vector3[6];
        Vector2[] shipUVs;
        int[] shipWhiteTriangles = new int[3];
        int[] shipBlackTriangles = new int[3];
        int[][] shipTriangles = new int[2][];
        Coords[] shipPoints, shipPointsSmall;

        /* Calculate the ship's vertices */
        shipPoints = CalculateShipPoints(1);
        shipPointsSmall = CalculateShipPoints(0.9f);

        /* Create the necessary arrays to define the ship as a mesh */
        shipVertices[0] = new Vector3(shipPoints[0].x - ship.x, shipPoints[0].y - ship.y, z - layerSize*layer);
        shipVertices[1] = new Vector3(shipPoints[1].x - ship.x, shipPoints[1].y - ship.y, z - layerSize*layer);
        shipVertices[2] = new Vector3(shipPoints[2].x - ship.x, shipPoints[2].y - ship.y, z - layerSize*layer);
        shipVertices[3] = new Vector3(shipPointsSmall[0].x - ship.x, shipPointsSmall[0].y - ship.y, z - layerSize*(layer+0.5f));
        shipVertices[4] = new Vector3(shipPointsSmall[1].x - ship.x, shipPointsSmall[1].y - ship.y, z - layerSize*(layer+0.5f));
        shipVertices[5] = new Vector3(shipPointsSmall[2].x - ship.x, shipPointsSmall[2].y - ship.y, z - layerSize*(layer+0.5f));
        shipUVs = CalculateUVs(shipVertices);
        shipWhiteTriangles = new int[] { 0, 1, 2 };
        shipBlackTriangles = new int[] { 3, 4, 5 };
        shipTriangles[0] = shipWhiteTriangles;
        shipTriangles[1] = shipBlackTriangles;

        /* Set the material of the ship */
        materials = new Material[] { White, Black };

        /* Assign the arrays to the mesh to draw the ship */
        SetMesh(ship.gameObject, shipVertices, shipUVs, shipTriangles, materials);
    }
    
    void CreatePhotonMesh(Photon photon, int layer) {
        /*
         * Photons are circles with varying radi between edges. The amount of edges increase the larger the photon.
         */
        Vector3[] photonVertices;
        Vector2[] photonUVs;
        int[] photonTriangles;
        float x, y;

        /* Calculate how many vertices this photon will be comprised of */
        int edges = Mathf.CeilToInt(photon.size * 500f);

        /* Initilize the vertices and triangles arrays and add the first set */
        photonVertices = new Vector3[edges+1];
        photonVertices[0] = new Vector3(0, 0, z - layerSize*layer);
        photonTriangles = new int[(edges+1)*3];
        photonTriangles[0] = 0;
        photonTriangles[1] = edges;
        photonTriangles[2] = 1;

        /* Calculate the position of each vertice that makes up the photon */
        for(int i = 0; i < edges; i++) {
            x = Mathf.Sin(i*PI/edges*2)*photon.size + Rand(-0.1f, 0.1f)*photon.size;
            y = Mathf.Cos(i*PI/edges*2)*photon.size + Rand(-0.1f, 0.1f)*photon.size;

            /* Add the new vertex to the photon's vertices list */
            photonVertices[i+1] = new Vector3(x, y, z - layerSize*layer);

            /* Add a triangle to the photon's mesh list */
            photonTriangles[(i+1)*3 + 0] = 0;
            photonTriangles[(i+1)*3 + 1] = i;
            photonTriangles[(i+1)*3 + 2] = i+1;
        }

        /* Calculate the UVs of the vertices */
        photonUVs = CalculateUVs(photonVertices);

        /* Give the photon's mesh the appropriate arrays */
        SetMesh(photon.gameObject, photonVertices, photonUVs, photonTriangles, Blue);
    }
    
    void CreateAsteroidsMesh(Asteroid asteroid, int layer) {
        /*
         * Creates a mesh for the given asteroid. Asteroids are made of two sets of black and white polygons, 
         * with the black ones being smaller but a larger layer priorety. 
         */
        Material[] materials;
        Vector3[] asteroidVertices;
        Vector3[] doubledAsteroidVertices;
        Vector2[] asteroidUVs;
        Coords coords;
        int[] asteroidTrianglesWhite;
        int[] asteroidTrianglesBlack;
        int[][] asteroidTriangles = new int[2][];
        float normalZ = z - layerSize*layer;
        float bumbedZ = z - layerSize*(layer+0.75f);

        /* Set up an array to hold all the vertices of the asteroid and insert the center point */
        asteroidVertices = new Vector3[asteroid.coords.Length + 1];
        asteroidVertices[0] = new Vector3(0, 0, normalZ);

        /* Add each vertex into the vertices array */
        for(int i = 0; i < asteroid.coords.Length; i++) {
            coords = AsteroidCoordPosition(asteroid, 1, i);
            asteroidVertices[i+1] = new Vector3(coords.x - asteroid.x, coords.y - asteroid.y, normalZ);
        }

        /* Set up an array to define the triangles that make up the asteroid's mesh and insert the first */
        asteroidTrianglesWhite = new int[(asteroid.coords.Length + 1)*3];
        asteroidTrianglesWhite[0] = 0;
        asteroidTrianglesWhite[1] = asteroid.coords.Length;
        asteroidTrianglesWhite[2] = 1;
        /* Add every triangle needed to define the asteroid */
        for(int i = 0; i < asteroid.coords.Length; i++) {
            asteroidTrianglesWhite[(i+1)*3 + 0] = 0;
            asteroidTrianglesWhite[(i+1)*3 + 1] = i;
            asteroidTrianglesWhite[(i+1)*3 + 2] = i+1;
        }

        /* Double all vertices, but make them 10% smaller to draw the black mesh of the asteroid */
        doubledAsteroidVertices = new Vector3[asteroidVertices.Length*2];
        doubledAsteroidVertices[asteroidVertices.Length] = new Vector3(0, 0, bumbedZ);
        for(int i = 0; i < asteroidVertices.Length; i++) {
            doubledAsteroidVertices[i] = asteroidVertices[i];
        }
        for(int i = 1; i < asteroidVertices.Length; i++) {
            doubledAsteroidVertices[asteroidVertices.Length + i].x = asteroidVertices[i].x*0.95f;
            doubledAsteroidVertices[asteroidVertices.Length + i].y = asteroidVertices[i].y*0.95f;
            doubledAsteroidVertices[asteroidVertices.Length + i].z = bumbedZ;
        }

        /* Create another triangle set for the new set of vertices */
        asteroidTrianglesBlack = new int[(asteroid.coords.Length + 1)*3];
        for(int i = 0; i < asteroidVertices.Length; i++) {
            asteroidTrianglesBlack[(i*3) + 0] = asteroidVertices.Length;
            asteroidTrianglesBlack[(i*3) + 1] = asteroidTrianglesWhite[(i*3) + 1] + asteroidVertices.Length;
            asteroidTrianglesBlack[(i*3) + 2] = asteroidTrianglesWhite[(i*3) + 2] + asteroidVertices.Length;
        }

        /* Combine both triangle sets into a 2D array to be used as sub meshes */
        asteroidTriangles[0] = asteroidTrianglesWhite;
        asteroidTriangles[1] = asteroidTrianglesBlack;

        /* Set up the materials list of the colors to be used for */
        materials = new Material[] { White, Black };

        /* Set up the UVs for all the vertices of the asteoird */
        asteroidUVs = CalculateUVs(doubledAsteroidVertices);

        /* Set up the mesh of this asteroid using the calculated vertices, triangles and UVs */
        SetMesh(asteroid.gameObject, doubledAsteroidVertices, asteroidUVs, asteroidTriangles, materials);
    }

    void CreateBoxMesh(GameObject GO, float topY, float bottomY, float leftX, float rightX, float z) {
        /*
         * Create a box mesh using the given parameters and link it to the given gameObject's mesh
         */
        Vector3[] boxVertices = new Vector3[4];
        Vector2[] boxUVs;
        int[] boxTriangles;

        boxVertices[0] = new Vector3(leftX, bottomY, z);
        boxVertices[1] = new Vector3(leftX, topY, z);
        boxVertices[2] = new Vector3(rightX, topY, z);
        boxVertices[3] = new Vector3(rightX, bottomY, z);

        boxUVs = CalculateUVs(boxVertices);

        boxTriangles = new int[] {
                0, 1, 2,
                2, 3, 0
        };

        SetMesh(GO.gameObject, boxVertices, boxUVs, boxTriangles, Black);

    }

    /* -------- Update functions ------------------------------------------------------- */

    bool UpdatePauseState() {
        /*
         * Return true if the user gave an input that will cause the game to unpause
         * 
         * Check if the user has given any accepted inputs to unpause the game.
         * 
         * Start the game if the user unpauses the game from gameState = Inactive.
         * This state means the game did not recieve any input since it's initilization.
         */
        bool unPause = false;

        if(firePhoton == 1 || left == 1 || right == 1 || up == 1 || down == 1) {
            unPause = true;
        }

        return unPause;
    }

    void UpdateBackground() {
        /*
         * Increment the flicker value for each star by their flickerRate amount
         */

        foreach(BackgroundStar star in backgroundStars) {
            star.flicker += star.flickerRate * 60*Time.deltaTime;
            if(star.flicker > PI) {
                star.flicker -= PI;
            }
        }
    }

    void UpdateDust() {
        /*
         * Advance all the dust particles' positions and clean up any that reach the end of their lifetime
         */

        foreach(Dust dust in dustParticles) {
            dust.lifetime -= 60*Time.deltaTime;
            if(dust.lifetime < 0) {
                dust.active = false;
            }
            else {
                dust.x += dust.dx *60*Time.deltaTime;
                dust.y += dust.dy *60*Time.deltaTime;

                /* If the dust goes off the screen's edges, remove it */
                if(dust.x < xMin || dust.x > xMax || dust.y < yMin || dust.y > yMax) {
                    dust.active = false;
                }
                /* Dust that is still active will have it's linked gameObject's postion updated */
                else {
                    dust.gameObject.transform.localPosition = new Vector3(dust.x, dust.y, 0);
                }
            }
        }

        CleanupInactiveObjects(dustParticles);
    }

    void UpdatePhotonResidue() {
        /*
         * Advance all the photon residue particles' positions and clean up any that reach the end of their lifetime
         */

        foreach(Dust residue in photonResidue) {
            residue.lifetime -= Time.deltaTime;
            if(residue.lifetime < 0) {
                residue.active = false;
            }
            else {
                residue.x += residue.dx *60*Time.deltaTime;
                residue.y += residue.dy *60*Time.deltaTime;
            }

            /* If the residue goes off the screen's edges, remove it */
            if(residue.x < xMin || residue.x > xMax || residue.y < yMin || residue.y > yMax) {
                residue.active = false;
            }
            /* Residue that is still active will have it's linked gameObject's postion updated */
            else {
                residue.gameObject.transform.localPosition = new Vector3(residue.x, residue.y, 0);
            }
        }

        CleanupInactiveObjects(photonResidue);
    }

    void UpdateShip() {
        /*
         * Move the ship depending on the current pressed keys and adjust the ship's damaged value
         */

        /* turn the ship only if either left or right is pressed (left XOR right). Prevent phi from overflowing.
		 * If the ship is missing it's left/right piece, it cannot turn left/right. If the user has both pieces
		 * and holds down both keys, the ship will not turn. If the user holds down both keys and is missing a
		 * piece, the ship will spin as if the thruster on the missing piece is not working */
        if(!((left == 1) && (right == 1)) &&  ((left == 1) || (right == 1))) {
            ship.phi += ((((right)*2*PI)/ship.turnSpeed) + (-1*((left)*2*PI)/ship.turnSpeed) * Time.deltaTime*60);
            if(ship.phi > 2*PI) {
                ship.phi -= 2*PI;
            }
            else if(ship.phi < 0) {
                ship.phi += 2*PI;
            }
        }

        /* advance the ship only if either up or down is pressed. 5% (default value of "shipControl")
		 * of the ship's current speed vector  is lost, then 5% of the ship's "thrust vector"
		 * (where the ship's thrusters are pushing the ship)is added the ship current speed vector.
		 * This will prevent the ship from exceeding a dx or dy of 0.05*up*sin(shipPhi + (90*PI/180)).
		 * Holding both directions would implement an "air brake" where the ship loses 5% of it's current
		 * speed every tick, eventually setting it to a complete standstill. If the ship is missing it's
		 * back piece, it cannot apply forward or backwards thrust or use the "air brake" */
        if(up == 1 || down == 1) {
            ship.dy = ((1-ship.control)*ship.dy + ship.control*up*Mathf.Sin(ship.phi + (90*PI/180))
                    - ship.control*down*Mathf.Sin(ship.phi + (90*PI/180)) * Time.deltaTime*60);
            ship.dx = ((1-ship.control)*ship.dx + ship.control*up*Mathf.Cos(ship.phi - (90*PI/180))
                    - ship.control*down*Mathf.Cos(ship.phi - (90*PI/180)) * Time.deltaTime*60);
        }


        /* displace the ship's origin depending in it's speed (dx and dy, which will not go above 1).
         * Increasing shipSpeed will increase the ship's acceleration along with it's max displacement distance */
        ship.x += (ship.speed*ship.dx) * Time.deltaTime*60;
        ship.y += (ship.speed*ship.dy) * Time.deltaTime*60;

        /* if the ship passes the screen boundaries + the ship length
         * (ship's variable "size"), wrap it around to the other side */
        if(ship.x < xMin - ship.size) {
            ship.x = xMax + ship.size;
        }
        else if(ship.x > xMax + ship.size) {
            ship.x = xMin - ship.size;
        }
        if(ship.y < yMin - ship.size) {
            ship.y = yMax + ship.size;
        }
        else if(ship.y > yMax + ship.size) {
            ship.y = yMin - ship.size;
        }

        /* Update the ship's damage state */
        UpdateShipDamage();

        /* Update the ship's gameObject and mesh with it's new position and rotation */
        ship.gameObject.transform.localPosition = new Vector3(ship.x, ship.y, 0);
        ship.gameObject.transform.localEulerAngles = new Vector3(0, 0, -360*ship.phi/(2*PI));

    }

    void UpdateShipDamage() {
        /*
         * Whenever the ship is in the damaged state (damaged > 0), decrement the currentFlicker value
         * by a framerate independent value. Once it reaches 0, decrement damaged and add flickerRate back
         * to the currentFlicker value. 
         * 
         * This is to have the damage state decrement at a rate frame-rate independent 
         */

        if(ship.damaged > 0) {
            ship.currentFlicker -= 0.5f*60*Time.deltaTime;

            if(ship.currentFlicker <= 0) {
                ship.currentFlicker += ship.flickerRate;
                ship.damaged--;
            }
        }
    }

    void UpdatePhotons() {
        /*
         * advance photon laser shots, eliminating those that have gone past the window boundaries
         */

        foreach(Photon photon in photons) {
            photon.x += photon.dx * Time.deltaTime*60;
            photon.y += photon.dy * Time.deltaTime*60;

            /* If a photon reaches past the screen bounderies, set it to be inactive */
            if(photon.x < xMin-photon.size || photon.x > xMax+photon.size || photon.y < yMin-photon.size || photon.y > yMax+photon.size) {
                photon.active = false;
            }
            /* Update the photon's gameObject if it's still active */
            else {
                photon.gameObject.transform.localPosition = new Vector3(photon.x, photon.y, 0);
                photon.gameObject.transform.localEulerAngles = new Vector3(0, 0, Rand(0, 360));
            }
        }

        /* Cleanup any photons that have been set to inactive */
        CleanupInactiveObjects(photons);
    }

    void UpdateAsteroids() {
        /*
         * Advance and rotate all active asteroids, looping from the other side of they pass the screen's edge.
         */

        foreach(Asteroid asteroid in asteroids) {
            /* Advance and rotate the asteroids */
            asteroid.x += asteroid.dx * 60*Time.deltaTime;
            asteroid.y += asteroid.dy * 60*Time.deltaTime;
            asteroid.phi += asteroid.dphi * 60*Time.deltaTime;

            /* Keep the rotation angle between (0, PI) */
            if(asteroid.phi > 2*PI) {
                asteroid.phi -= 2*PI;
            }
            else if(asteroid.phi < 0) {
                asteroid.phi += 2*PI;
            }

            /* Roll the asteroid to the other side of the screen if they pass an edge */
            if(asteroid.x < xMin - asteroid.size*asteroidVarianceMax) {
                //Past the left edge
                asteroid.x = xMax + asteroid.size*asteroidVarianceMax;
            }
            else if(asteroid.x > xMax + asteroid.size*asteroidVarianceMax) {
                //Past the right edge
                asteroid.x = xMin - asteroid.size*asteroidVarianceMax;
            }
            if(asteroid.y < yMin - asteroid.size*asteroidVarianceMax) {
                //Past the bottom edge
                asteroid.y = yMax + asteroid.size*asteroidVarianceMax;
            }
            else if(asteroid.y > yMax + asteroid.size*asteroidVarianceMax) {
                asteroid.y = yMin - asteroid.size*asteroidVarianceMax;
            }



            /* Move the asteroid's mesh to match it's X and Y position */
            asteroid.gameObject.transform.localPosition = new Vector3(asteroid.x, asteroid.y, 0);
            asteroid.gameObject.transform.localEulerAngles = new Vector3(0, 0, -360*asteroid.phi/(2*PI));
        }

    }

    void UpdateDebris() {
        /*
         * Advance, rotate and lower the lifetime of debris in the field. Slow the debris'
         * rotation and velocity overtime to allow the user to collect them easier despite
         * not being physically accurate. Once a debris' lifetime reaches 0, destroy it
         */
        Debris d;
        int redFrames = 25;

        for(int i = 0; i < debris.Count; i++) {
            d = (Debris) debris[i];
            d.lifetime -= 60*Time.deltaTime;
            d.x += d.dx *60*Time.deltaTime;
            d.y += d.dy *60*Time.deltaTime;
            d.phi += d.dphi *60*Time.deltaTime;
            d.delta += d.ddelta *60*Time.deltaTime;
            d.dx *= 0.995f;
            d.dy *= 0.995f;
            d.dphi *= 0.995f;
            d.ddelta *= 0.995f;
            /* prevent the debri's phi from overflowing */
            if(d.phi > 2*PI) {
                d.phi -= 2*PI;
            }
            else if(d.phi < 0) {
                d.phi += 2*PI;
            }
            
            /* Set the color of the debris to either flashing yellow or turning red */
            if(d.lifetime < redFrames) {
                d.gameObject.GetComponent<MeshRenderer>().material.color = new Color(1, (d.lifetime/redFrames), 0, 1);
            }
            else {
                d.gameObject.GetComponent<MeshRenderer>().material.color = new Color(1, 1, (Mathf.FloorToInt(d.lifetime)%3), 1);
            }

            /* Do not let the debris stay active for too long */
            if(d.lifetime <= 0) {
                /* If the debris' lifetime runs out, explode into dust particles */
                InitDust(d.x, d.y);
                d.active = false;
                DeleteScoreGain(d.linkedScore);

            }
            /* Delete the debris if it goes offscreen */
            else if(d.x - asteroidSizeMin > xMax || d.x + asteroidSizeMin < xMin ||
                    d.y - asteroidSizeMin > yMax || d.y + asteroidSizeMin < yMin) {
                d.active = false;
                DeleteScoreGain(d.linkedScore);
            }
            /* Update the debris' gameObject's position if it's still active along with it's linked scoreGain */
            else {
                d.gameObject.transform.localPosition = new Vector3(d.x, d.y, 0);
                d.gameObject.transform.localEulerAngles = new Vector3(360*d.delta/2*PI, 360*d.phi/2*PI, 0);
                //Update the debris' linked score's position
                d.linkedScore.gameObject.transform.localPosition = d.gameObject.transform.localPosition;
                d.linkedScore.x = d.x;
                d.linkedScore.y = d.y;
                d.linkedScore.dy = d.dy;
            }
        }

        CleanupInactiveObjects(debris);
    }

    void UpdateScoreGain() {
        /*
         * Update all the free scoreGain objects by counting down their lifetime until they are removed.
         * They also move along the Y axis and will be deleted if they reach past the screen
         */

        foreach(scoreGain s in freeScoreGains) {
            s.y += s.dy *60*Time.deltaTime;
            s.dy *= 0.995f;
            s.lifetime -= 60f*Time.deltaTime;

            /* If the scoreGain goes off the screen's edges, remove it */
            if(s.x < xMin || s.x > xMax || s.y < yMin || s.y > yMax) {
                s.active = false;
            }
            /* If the scoreGain's lifetime runs out, remove it */
            else if(s.lifetime < 0) {
                s.active = false;
            }
            /* Adjust the scoreGain's gameObject to reflect it's new position */
            else {
                s.gameObject.transform.localPosition = new Vector3(s.x, s.y, 0);
            }
        }

        CleanupInactiveObjects(freeScoreGains);
    }

    void UpdateTransmission() {
        /*
         * Update the transTime value so the transmission text window can proceed through it's animation.
         * This will handle the animation timing and state changing. It will also detect when the animation
         * is complete.
         */
        float normalizedTime = currentTransAnimTime / transTextStateTimings[currentTransAnimState];
        

        /* While animating the transmission label, LERP it into it's final position */
        if(currentTransAnimState == (int) transTextStates.TransLabel) {
            transLabel.transform.localPosition = Vector3.Lerp(transLabelStart, transLabelEnd, Mathf.Sin(normalizedTime*PI/2f));
        }
        else if(currentTransAnimState == (int) transTextStates.TransLabel+1 && currentTransAnimTime == 0) {
            transLabel.transform.localPosition = transLabelEnd;
        }


        /* Animate the transmission box by increasing it's width */
        if(currentTransAnimState == (int) transTextStates.TransBox) {
            float currentBoxWidthRatio = currentTransAnimTime/transTextStateTimings[currentTransAnimState];        
            /* Create the mesh of the box with */
            transBox.transform.localPosition = new Vector3((transBoxLeft + transBoxRight)/2, (transBoxTop + transBoxBottom)/2, -layerSize);
            transBox.transform.localEulerAngles = Vector3.zero;
            CreateBoxMesh(transBox, transBoxHeight/2, -transBoxHeight/2, -currentBoxWidthRatio*transBoxWidth/2, currentBoxWidthRatio*transBoxWidth/2, 0);
        }
        /* Ensure the transBox is redrawn in the desired sizes for the last time */
        else if(currentTransAnimState == (int) transTextStates.TransBox+1 && currentTransAnimTime == 0) {
            transBox.transform.localPosition = new Vector3((transBoxLeft + transBoxRight)/2, (transBoxTop + transBoxBottom)/2, -layerSize);
            transBox.transform.localEulerAngles = Vector3.zero;
            CreateBoxMesh(transBox, transBoxHeight/2, -transBoxHeight/2, -transBoxWidth/2, transBoxWidth/2, 0);
        }

        
        /* While in the static box animating state, redraw the static's mesh to animate it */
        if(currentTransAnimState == (int) transTextStates.StaticBox) {
            currentStaticBoxWidth = staticBoxWidth*(Mathf.Sin(((1*PI/2) + normalizedTime*2*PI)) + 1)/2f;
            currentStaticBoxHeight = staticBoxHeight*(Mathf.Sin((3*PI/2) + normalizedTime*3*PI) + 1)/2f;
            currentStaticBoxBottom = staticBoxBottom + staticBoxHeight/2f - currentStaticBoxHeight/2f;
            currentStaticBoxLeft = staticBoxLeft + staticBoxWidth/2f - currentStaticBoxWidth/2f;
            staticBox.transform.localPosition = new Vector3(currentStaticBoxLeft + currentStaticBoxWidth/2, currentStaticBoxBottom + currentStaticBoxHeight/2, -layerSize*2);
            CreateBoxMesh(staticBox, currentStaticBoxHeight/2, -currentStaticBoxHeight/2, -currentStaticBoxWidth/2, currentStaticBoxWidth/2, 0);
        }
        /* Make sure the static box's mesh is drawn in the desired size once it's done animating */
        else if(currentTransAnimState == (int) transTextStates.StaticBox+1 && currentTransAnimTime == 0) {
            currentStaticBoxBottom = staticBoxBottom;
            currentStaticBoxLeft = staticBoxLeft;
            currentStaticBoxWidth = staticBoxWidth;
            currentStaticBoxHeight = staticBoxHeight;
            staticBox.transform.localPosition = new Vector3(currentStaticBoxLeft + currentStaticBoxWidth/2, currentStaticBoxBottom + currentStaticBoxHeight/2, -layerSize*2);
            CreateBoxMesh(staticBox, currentStaticBoxHeight/2, -currentStaticBoxHeight/2, -currentStaticBoxWidth/2, currentStaticBoxWidth/2, 0);

            /* Have the static box label start from the top right corner of the static box */
            staticLabel.transform.localPosition = new Vector3(currentStaticBoxLeft + currentStaticBoxWidth + staticLabelSpacing, transBoxTop + staticLabelSpacing + staticLabelSpacing, -layerSize*2);
        }
        
        

        /* Animate the static box's label by writting it's characters after every typingLeterInterval time */
        if(currentTransAnimState == (int) transTextStates.StaticLabel) {
            staticLabel.text = "";
            for(int i = 0; i < staticLabelText.Length && i < currentTransAnimTime/typingTimeInterval; i++) {
                staticLabel.text += staticLabelText[i];
            }
        }
        /* Ensure the staticLabel will stop animating with it's full text */
        else if(currentTransAnimState == (int) transTextStates.StaticLabel+1 && currentTransAnimTime == 0) {
            staticLabel.text = staticLabelText;
        }

        /* Animate the transmissionMessage by typing it's characters sequentially */
        if(currentTransAnimState == (int) transTextStates.TransMessage) {
            transMessage.text = "";
            for(int i = 0; i < transmissionMessage.Length && i < currentTransAnimTime/(typingTimeInterval/5); i++) {
                transMessage.text += transmissionMessage[i];
            }
        }
        /* Ensure the whole message is displayed once it finishes animating */
        else if(currentTransAnimState == (int) transTextStates.TransMessage+1 && currentTransAnimTime == 0) {
            transMessage.text = transmissionMessage;
        }
        

        /* Increment the time spent in the transmission state */
        currentTransAnimTime += 60*Time.deltaTime;

        /* Check if the transTime reached it's time limit for it to change into the next animation state */
        if(currentTransAnimTime > transTextStateTimings[currentTransAnimState]) {

            /* Check if it reached the end of the final animation state */
            if(currentTransAnimState >= transTextStateTimings.Length - 1) {
                gameState = (int) State.Finished;
            }

            /* Advance to the next animation state and reset the timing */
            else {
                currentTransAnimTime = 0;
                currentTransAnimState++;
            }
        }
    }


    /* -------- Collision functions ------------------------------------------------------- */

    void CollisionAsteroidPhoton() {
        /*
	     * Detect collisions with asteroids and photons by using circle-cirlce and circle-polygon collision detection.
         * There are collision checks for each photon - asteroid : 
         * 
         * Check if the photon is touching the asteroid's maximum radius to run a basic collision check
         * to see if the photon is close enough to the asteroid to potentially hit it. This can be
         * found by comparing the distance between the center of the photon + the asteroid
         * with the distance that is the maximum asteroid size + the photon size.
         * 
         * Check if the photon is touching the asteroid's minimum radius. If this is true,
         * then we know the photon is definitly hitting the asteroid
         * 
         * Once we know the photon is within the asteroid's minimum and maximum radius, we can run
         * a proper circle - line collision detection for each the asteroid's vertices.
         * 
         * Once a collision occurs and the damage is applied, set both the asteroid and the photon's active
         * value to "false" to prevent any other collisions between the two.
         * 
         * Once all collisions have been calculated, delete the photons and asteroids that have collided.
         */
        Coords p1, p2;
        Asteroid a;
        Photon p;

        for(int i = 0; i < asteroids.Count; i++) {
            a = (Asteroid) asteroids[i];
            for(int ii = 0; ii < photons.Count; ii++) {
                p = (Photon) photons[ii];
                /* Check if the photon is touching the asteroid's maximum potential radius */
                if(Mathf.Pow(a.x - p.x, 2) + Mathf.Pow(a.y - p.y, 2) <= Mathf.Pow(a.size*asteroidVarianceMax + p.size, 2)) {

                    /* Check if the photon is touching the asteroid's minimum potential radius */
                    if(Mathf.Pow(a.x - p.x, 2) + Mathf.Pow(a.y - p.y, 2) <= Mathf.Pow(a.size*asteroidVarianceMin + p.size, 2)) {
                        /* Delete the photon and asteroid that collided together and reset the for loops */
                        AsteroidDamaged(p, a);
                        i--;
                        ii = photons.Count;

                    }
                    /* Check whether a collision has happened between the photon and the asteroid's vertices */
                    else {
                        /* Grab the coordinates of the last vertex */
                        p1 = AsteroidCoordPosition(a, 1, a.coords.Length-1);

                        for(int iii = 0; iii < a.coords.Length; iii++) {
                            /* Save the previous vertex's postion and get the next point */
                            p2 = p1;
                            p1 = AsteroidCoordPosition(a, 1, iii);

                            /* Check if the photon is touching the line made of the two vertexes */
                            if(LineCircleCollision(p.x, p.y, p.size, p1.x, p1.y, p2.x, p2.y)) {
                                /* Delete the photon and asteroid that collided together and reset the for loops */
                                AsteroidDamaged(p, a);
                                i--;
                                ii = photons.Count;
                                iii = a.coords.Length;
                            }
                        }
                    }
                }
            }
        }

        /* Delete any asteoroids and photons that have been set to inactive */
        CleanupInactiveObjects(asteroids);
        CleanupInactiveObjects(photons);
    }

    void CollisionAsteroidShip() {
        /*
         * Handle any collisions between the player ship and an asteroid. Start by checking if 
         * the player ship is touching the asteroid's maximum radius before checking for
         * a lin - line collision between the ship and the asteroid
         */
        Coords[] shipPoints;
        Coords coords;
        float x1, y1, x2, y2;

        /* Get the position of each point on the player ship */
        shipPoints = CalculateShipPoints(1);

        foreach(Asteroid a in asteroids) {
            /* Check if the ship is close enough to the asteroid's minimum radius to always be touching it */
            if(Mathf.Pow(a.x - ship.x, 2) + Mathf.Pow(a.y - ship.y, 2) <= Mathf.Pow(a.size*asteroidVarianceMin, 2)) {
                ShipDamaged();
                ShipKnockback(a);
            }

            /* Ensure the ship is close enough to the asteroid before running collision checks */
            else if(Mathf.Pow(a.x - ship.x, 2) + Mathf.Pow(a.y - ship.y, 2) <=
                    Mathf.Pow(a.size*asteroidVarianceMax + ship.size, 2)) {

                /* Grab the coordinates of the last vertex of the asteroid */
                coords = AsteroidCoordPosition(a, 1, a.coords.Length-1);
                x1 = coords.x;
                y1 = coords.y;

                for(int i = 0; i < a.coords.Length; i++) {
                    /* Save the previous vertex's postion */
                    x2 = x1;
                    y2 = y1;

                    /* Get the coordinates of the current vertex */
                    coords = AsteroidCoordPosition(a, 1, i);
                    x1 = coords.x;
                    y1 = coords.y;

                    /* Run a line - line collision check with each ship vertex and the two saved asteroid vertexes */
                    if(LineCollision(shipPoints[0].x, shipPoints[0].y, shipPoints[1].x, shipPoints[1].y, x1, y1, x2, y2) ||
                            LineCollision(shipPoints[1].x, shipPoints[1].y, shipPoints[2].x, shipPoints[2].y, x1, y1, x2, y2) ||
                            LineCollision(shipPoints[2].x, shipPoints[2].y, shipPoints[0].x, shipPoints[0].y, x1, y1, x2, y2)) {
                        ShipDamaged();
                    }
                }
            }
        }
    }

    void CollisionDebrisShip() {
        /*
         * Check whether the ship collides into a piece of debris (ship radius touches debris origin)
         * to give them "points" and remove the debris to simulate the ship picking up the debris.
         * When the debris gets removed, add it's linked scoreGain to the freeScoreGains list.
         */
        Debris d;

        for(int i = 0; i < debris.Count; i++) {
            d = (Debris) debris[i];
            if(Mathf.Pow(d.x - ship.x, 2) + Mathf.Pow(d.y - ship.y, 2) <= Mathf.Pow(ship.size, 2)) {
                Debug.Log("Picked up Debris");
                d.active = false;
                freeScoreGains.Add(d.linkedScore);
                d.linkedScore.score = CalculateDebrisScore(d);
            }
        }

        CleanupInactiveObjects(debris);
    }


    /* -------- Initilization functions ------------------------------------------------------- */

    GameObject InitMesh(GameObject GO, string name) {
        /*
         * Create a gameObject with a mesh attached and link it to the given GO.
         * If there is already a gameObject linked to it, do nothing.
         */
         
        if(GO == null) {
            GO = new GameObject();
            GO.name = name;

            /* Insert the given gameObject into it's approriate container. Create the container if does not exist */
            if(transform.Find(name + " Container") == null) {
                GameObject childContainer = new GameObject();
                childContainer.name = name + " Container";
                childContainer.transform.parent = this.transform;
                childContainer.transform.position = transform.position;
                childContainer.transform.rotation = transform.rotation;
            }

            /* Set and create the parameters of the given gameObject */
            GO.transform.parent = transform.Find(name + " Container");
            GO.transform.localPosition = Vector3.zero;
            GO.transform.localEulerAngles = Vector3.zero;
            GO.transform.localScale = Vector3.one;
            GO.AddComponent<MeshFilter>();
            GO.AddComponent<MeshRenderer>();
        }

        return GO;
    }
    
    void InitColors() {
        /*
         * Initialize the materials needed to draw the asteroids game
         */
        AsteroidsShader = Shader.Find("Unlit/Color");

        OpenGL = new Material(Shader.Find("Hidden/Internal-Colored"));
        OpenGL.hideFlags = HideFlags.HideAndDontSave;
        OpenGL.SetInt("_SrcBlend", (int) UnityEngine.Rendering.BlendMode.SrcAlpha);
        OpenGL.SetInt("_DstBlend", (int) UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
        OpenGL.SetInt("_Cull", (int) UnityEngine.Rendering.CullMode.Off);
        OpenGL.SetInt("_ZWrite", 0);

        White = new Material(AsteroidsShader);
        White.color = Color.white;
        White.hideFlags = HideFlags.HideAndDontSave;
        White.SetInt("_SrcBlend", (int) UnityEngine.Rendering.BlendMode.SrcAlpha);
        White.SetInt("_DstBlend", (int) UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
        White.SetInt("_Cull", (int) UnityEngine.Rendering.CullMode.Off);
        White.SetInt("_ZWrite", 0);

        Black = new Material(AsteroidsShader);
        Black.color = Color.black;
        Black.hideFlags = HideFlags.HideAndDontSave;
        Black.SetInt("_SrcBlend", (int) UnityEngine.Rendering.BlendMode.SrcAlpha);
        Black.SetInt("_DstBlend", (int) UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
        Black.SetInt("_Cull", (int) UnityEngine.Rendering.CullMode.Off);
        Black.SetInt("_ZWrite", 0);

        Blue = new Material(AsteroidsShader);
        Blue.color = new Color(0, 0, 0.75f, 1f);
        Blue.hideFlags = HideFlags.HideAndDontSave;
        Blue.SetInt("_SrcBlend", (int) UnityEngine.Rendering.BlendMode.SrcAlpha);
        Blue.SetInt("_DstBlend", (int) UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
        Blue.SetInt("_Cull", (int) UnityEngine.Rendering.CullMode.Off);
        Blue.SetInt("_ZWrite", 0);
    }
    
    void InitBackground() {
        /* 
         * Create a set of points on the asteroid's field that will flicker to represent background stars.
         * The larger the playing field, the more stars will be used
         */
        BackgroundStar star;

        for(float i = xMin; i < xMax; i += 0.1f) {
            for(float ii = yMin; ii < yMax; ii += 0.1f) {
                star = new BackgroundStar();
                star.gameObject = InitMesh(star.gameObject, "Star");
                star.x = i + Rand(0, 0.1f);
                star.y = ii + Rand(0, 0.1f); ;
                star.flicker = Rand(0, -PI);
                star.flickerRate = Rand(0.001f, 0.05f);
                backgroundStars.Add(star);
                star.gameObject.transform.localPosition = new Vector3(star.x, star.y, 0);
            }
        }
    }
    
    void InitDust(Asteroid a, Asteroid b) {
        /*
         * Create dust particles bewteen the two asteroids
         */
        Dust dust;
        int dustCount;
        float x, y, dx, dy;

        /* Get the point of origin for the dust particles to be between both asteroids */
        x = (a.x + b.x)/2;
        y = (a.y + b.y)/2;

        /* Give the particle a fraction of both asteroids' speeds */
        dx = (a.dx*0.3f + b.dx*0.7f);
        dy = (a.dy*0.3f + b.dy*0.7f);

        /* The dust count is directly proportional to asteroid size and uses values relative to the asteroid's */
        dustCount = Mathf.FloorToInt((a.size + b.size)*500f);
        for(int i = 0; i < dustCount; i++) {
            dust = new Dust();
            dust.gameObject = InitMesh(dust.gameObject, "Dust");

            /* Give the particles varying speeds */
            dust.dx = dx*Rand(-0.25f, 1.3f);
            dust.dy = dy*Rand(-0.25f, 1.3f);

            /* Don't have all particles start in the same position */
            dust.x = x + asteroidVarianceMin * a.size+b.size*Rand(-1, 1);
            dust.y = y + asteroidVarianceMin * a.size+b.size*Rand(-1, 1);
            dust.x += dust.dx *10*Time.deltaTime*60;
            dust.y += dust.dy *10*Time.deltaTime*60;

            dust.lifetime = Mathf.FloorToInt(300*Rand(0.75f, 1.25f));
            dustParticles.Add(dust);
            dust.gameObject.transform.localPosition = new Vector3(dust.x, dust.y, 0);
        }
    }

    void InitDust(float x, float y) {
        /*
         * Emit a set of dust particles from the given point
         */
        Dust dust;
        Coords coords;
        float dustcount;

        dustcount = Rand(10, 20);
        for(int i = 0; i < dustcount; i++) {
            dust = new Dust();
            dust.gameObject = InitMesh(dust.gameObject, "Dust");
            coords = RandPointInCircle();
            dust.dx = Rand(-asteroidSpeedMax/10f, asteroidSpeedMax/10f);
            dust.dy = Rand(-asteroidSpeedMax/10f, asteroidSpeedMax/10f);
            dust.x = x + coords.x*asteroidSizeMin;
            dust.y = y + coords.y*asteroidSizeMin;
            dust.lifetime = 100*Rand(0.8f, 1.2f);
            dustParticles.Add(dust);
            dust.gameObject.transform.localPosition = new Vector3(dust.x, dust.y, 0);
        }
    }

    void InitPhotonResidue(Photon p, Asteroid a) {
        /*
         * Create photon residue where a photon was just destroyed. Use the position and velocity
         * of a photon to determine the residue's path. If the given asteroid is not null,
         * have it's velocity redirect the residue.
         */
        Coords coords;
        Dust residue;
        int residueCount;
        float asteroidVelocityX, asteroidVelocityY;

        /* The velocity of the asteroid if applicable */
        if(a != null) {
            asteroidVelocityX = a.dx;
            asteroidVelocityY = a.dy;
        }
        else {
            asteroidVelocityX = p.dx;
            asteroidVelocityY = p.dy;
        }

        /* The residue count is directly proportional to the photon's size */
        residueCount = Mathf.FloorToInt((p.size)*10000f);
        for(int i = 0; i < residueCount; i++) {
            residue = new Dust();
            residue.gameObject = InitMesh(residue.gameObject, "Photon Residue");

            /* The residue will start within the photon's radius */
            coords = RandPointInCircle();
            residue.x = p.x + p.size*coords.x;
            residue.y = p.y + p.size*coords.y;

            /* Inherit the velocity from the photon and the asteroid */
            residue.dx = (p.dx*Rand(0.6f, 0.9f) + asteroidVelocityX*Rand(0.1f, 0.4f));
            residue.dy = (p.dy*Rand(0.6f, 0.9f) + asteroidVelocityY*Rand(0.1f, 0.4f));

            /* Add some randomness to the residue's velocity */
            residue.dx += p.dy * Rand(-0.2f, 0.2f);
            residue.dy += p.dx * Rand(-0.2f, 0.2f);

            residue.lifetime = Mathf.FloorToInt(Rand(0.75f, 1.75f));
            photonResidue.Add(residue);
            residue.gameObject.transform.localPosition = new Vector3(residue.x, residue.y, 0);
        }
    }
    
    void InitShip() {
        /*
         * Initilize the ship's values and it's position in the game then draw it's mesh
         */

        ship.x = xMin + (xMax - xMin)/2f;
        ship.y = yMin + (yMax - yMin)/2f;
        ship.size = 0.05f;
        ship.phi = 0;
        ship.speed = 0.005f;
        ship.control = 0.05f;
        ship.dx = 0;
        ship.dy = 0;
        ship.turnSpeed = 90f;
        ship.damaged = 0;
        ship.flickerRate = 1;
        ship.currentFlicker = 0;
        ship.gameObject = InitMesh(ship.gameObject, "Ship");

        CreateShipMesh(2);
        ship.gameObject.transform.localPosition = new Vector3(ship.x, ship.y, 0);
        ship.gameObject.transform.localEulerAngles = new Vector3(0, 0, -360*ship.phi/(2*PI));
    }

    void InitPhoton() {
        /*
         * Fire a photon that inherits the player ship's location, direction and velocity.
         * There can only be a limited amount of photons in play.
         */

        if(photons.Count < 10) {
            Photon firedPhoton = new Photon();

            /* First create the gameObject and the mesh linked to the photon */
            firedPhoton.gameObject = InitMesh(firedPhoton.gameObject, "Photon");

            /* Give the photons a varying size */
            firedPhoton.size = Rand(0.0175f, 0.0275f);

            /* Start the photon one photon length away from the player ship */
            firedPhoton.x = ship.x + firedPhoton.size*Mathf.Sin(ship.phi);
            firedPhoton.y = ship.y + firedPhoton.size*Mathf.Cos(ship.phi);

            /* Have the photon's direction inherit the ship's facing direction */
            firedPhoton.dx = photonSpeed*Mathf.Sin(ship.phi);
            firedPhoton.dy = photonSpeed*Mathf.Cos(ship.phi);

            /* Have the photons inherit a fraction of the ship's velocity */
            firedPhoton.dx += ship.dx*0.005f;
            firedPhoton.dy += ship.dy*0.005f;

            /* Add the photon to the list of active photons */
            photons.Add(firedPhoton);

            /* Draw the mesh of the photon */
            CreatePhotonMesh(firedPhoton, 3);
            firedPhoton.gameObject.transform.localPosition = new Vector3(firedPhoton.x, firedPhoton.y, 0);
            firedPhoton.gameObject.transform.localEulerAngles = new Vector3(0, 0, Rand(0, 360));
        }
        else {
            Debug.Log("too many photons in play");
        }
    }

    void InitAsteroid(float size) {
        /*
         * Create an asteroid object on the edges of the screen. Velociy, rotation and
         * shape are generated randomly using the given size value as reference point.
         */
        Asteroid asteroid;

        /* Create the asteroid object along with the vertices that define it  */
        asteroid = InitEmptyAsteroid(size);

        /* Set the asteroid's starting position on either the top or left edge of the screen */
        if(Rand(0, 1f) > 0.5f) {
            asteroid.x = Rand(xMin, xMax);
            asteroid.y = yMin - asteroid.size*asteroidVarianceMax;
        }
        else {
            asteroid.x = xMin - asteroid.size*asteroidVarianceMax;
            asteroid.y = Rand(yMin, yMax);
        }

        /* Prevent the asteroid from starting with a low velocity to ensure it is not sitting outside the screen's edges */
        asteroid.dx = Rand(asteroidSpeedMin*3, asteroidSpeedMax/5f);
        asteroid.dy = Rand(asteroidSpeedMin*3, asteroidSpeedMax/5f);
        if(Rand(0, 1f) > 0.5f) {
            asteroid.dx *= -1;
        }
        if(Rand(0, 1f) > 0.5f) {
            asteroid.dy *= -1;
        }

        /* Set the rotation and rotation speed of the asteroid randomly */
        asteroid.phi = Rand(0, 2*PI);
        asteroid.dphi = Rand(-0.075f, 0.075f);

        //asteroid.dphi = 0;
        //asteroid.dx = 0;
        //asteroid.dy = 0;
        //asteroid.x = xMin + (xMax - xMin)/3f;
        //asteroid.y = yMin + (yMax - yMin)/2f;

        /* Add the asteroid to the list of active asteroids */
        asteroids.Add(asteroid);
    }

    Asteroid InitEmptyAsteroid(float size) {
        /*
         * Create an asteroid object with the given size, but only populate
         * the coords, nVertices variables and it's gameobject with mesh.
         */
        Asteroid asteroid = new Asteroid();
        Coords coords;
        float theta;
        float r;

        /* Give the asteroid an amount of vertices relative to it's size */
        asteroid.size = size;
        asteroid.nVertices = 4 + Mathf.FloorToInt(asteroid.size * 200);
        asteroid.coords = new Coords[asteroid.nVertices];

        for(int i = 0; i < asteroid.nVertices; i++) {
            theta = 2f * PI * i/(float) asteroid.nVertices;
            /* Have all vertices' distances from the asteroid's center remain within the variance limits */
            r = asteroid.size * Rand(asteroidVarianceMin, asteroidVarianceMax);
            coords = new Coords();
            coords.x = -r * Mathf.Sin(theta);
            coords.y = r * Mathf.Cos(theta);
            asteroid.coords[i] = coords;
        }

        /* Create the gameObject and mesh used to define the asteroid */
        asteroid.gameObject = InitMesh(asteroid.gameObject, "Asteroid");

        /* Draw the mesh for the asteroid once it's positional values are set */
        CreateAsteroidsMesh(asteroid, 4);
        asteroid.gameObject.transform.localPosition = new Vector3(asteroid.x, asteroid.y, 0);
        asteroid.gameObject.transform.localEulerAngles = new Vector3(0, 0, -360*asteroid.phi/(2*PI));

        return asteroid;
    }

    void InitDebris(Asteroid a) {
        /*
         * Create debris objects from a destroyed asteroid. The debris will inherit properties of the asteroid.
         * Debris pieces are made of 6 points: two center points with different depths and four points which
         * form a triangle in the same depth situated between the two center pieces. The goal is to
         * make a 3d piece of debris with 8 sides
         */
        Vector3[] debrisVertices;
        Vector2[] debrisUVs;
        int[] debrisTriangles;
        Debris d;
        Coords coords;
        float debrisCount = Mathf.FloorToInt(Rand(2.5f, 3.5f));

        for(int i = 0; i < debrisCount; i++) {
            d = new Debris();
            d.gameObject = InitMesh(d.gameObject, "Debris");
            d.size = a.size / 2f;

            /* Create the two center pieces */
            debrisVertices = new Vector3[6];
            debrisVertices[0] = new Vector3(0, d.size*Rand(1.25f, 2.75f), 0);
            debrisVertices[5] = new Vector3(0, -d.size*Rand(1.25f, 2.75f), 0);

            /* Create the rest of the vertices that forms the rest of the debris */
            debrisVertices[1] = new Vector3(d.size*Rand(1.25f, 2.75f), 0, 0);
            debrisVertices[2] = new Vector3(-d.size*Rand(1.25f, 2.75f), 0, 0);
            debrisVertices[3] = new Vector3(0, 0, d.size*Rand(1.25f, 2.75f));
            debrisVertices[4] = new Vector3(0, 0, -d.size*Rand(1.25f, 2.75f));

            /* Create the triangles that form the 8 sided shape */
            debrisTriangles = new int[] {
                    0, 3, 1,
                    0, 1, 4,
                    0, 2, 3,
                    0, 4, 2,
                    5, 1, 3,
                    5, 4, 1,
                    5, 3, 2,
                    5, 2, 4
                    };

            /* Calculate the UVs of the debris' vertices */
            debrisUVs = CalculateUVs(debrisVertices);

            /* Create the gameObject and mesh for the debris */
            SetMesh(d.gameObject, debrisVertices, debrisUVs, debrisTriangles, White);
            
            /* Set the variables used to define the debris */
            coords = RandPointInCircle();
            d.x = a.x + coords.x * a.size;
            d.y = a.y + coords.y * a.size;
            d.phi = Rand(0.0f, 2*PI);
            d.dphi = Rand(-0.05f, 0.05f);
            d.delta = Rand(0.0f, 2*PI);
            d.ddelta = Rand(-0.05f, 0.05f);
            d.lifetime = Mathf.FloorToInt(Rand(175, 275));
            d.dx = Rand(0.4f, 0.75f)*a.dx + asteroidSpeedMin*Mathf.Sin((i*Rand(0.8f, 1.2f))*2*PI/debrisCount);
            d.dy = Rand(0.4f, 0.75f)*a.dy + asteroidSpeedMin*Mathf.Cos((i*Rand(0.8f, 1.2f))*2*PI/debrisCount);

            debris.Add(d);
            d.gameObject.transform.localPosition = new Vector3(d.x, d.y, 0);
            d.gameObject.transform.localEulerAngles = new Vector3(360*d.delta/2*PI, 360*d.phi/2*PI, 0);

            /* Create the debris' linked scoreGain object */
            InitScoreGain(d);
        }
    }
   
    void InitScoreGain(Debris d) {
        /*
         * Create a scoreGain object and link it to the given debris. It will not be added into
         * the scoreGains list until it's linked debris is picked up. 
         */

        if(d.linkedScore == null) {
            scoreGain newScore = new scoreGain();
            newScore.gameObject = InitMesh(newScore.gameObject, "ScoreGain");
            newScore.lifetime = 100;
            newScore.gameObject.transform.localPosition = d.gameObject.transform.localPosition;
            newScore.x = d.x;
            newScore.y = d.y;
            newScore.dy = d.dy;
            d.linkedScore = newScore;
        }
    }

    void InitTransmissionObjects() {
        /*
         * Initilize all gameObjects that will be used with the transmission window and it's animations.
         * Use a prefab of a gameObject with an attached textMesh to be used to draw text into the asteroids game.
         */
         Transform transmissionContainer;
         
         transmissionContainer = transform.Find("Transmission Container");

        /* Create a "Transmission Container" gameObject as a child in the main transform to hold all the new gameObjects */
        if(transmissionContainer == null) {
            GameObject containerObject = new GameObject();
            containerObject.name = "Transmission Container";
            containerObject.transform.parent = this.transform;
            containerObject.transform.position = transform.position;
            containerObject.transform.rotation = transform.rotation;
            transmissionContainer = containerObject.transform;
        }
        
        /* Create the label bellow the transmission text box labelling this as a transmission */
        InitTransmissionTransLabel(transmissionContainer);

        /* Create the box used as a background for the main text of the transmission */
        InitTransmissionTransBox(transmissionContainer);

        /* Create the label for the static box above the transmission text box */
        InitTransmissionStaticLabel(transmissionContainer);
        
        /* Create the background for the static box */
        InitTransmissionStaticBox(transmissionContainer);

        /* Create the message text mesh for the transBox */
        InitTransmissionTransMessage(transmissionContainer);
    }

    void InitTransmissionTransLabel(Transform parent) {
        /*
         * Create the transmission label that slides into view from bellow the bottom of the canvas
         * to bellow the transmission main text box. It is used to label the transmission windows
         * to indicate to the player the windows and words that will appear are from a transmission.
         * 
         * There only exists one transmission label, transLabel.
         *
         * It's position is not relative to any other transmission object.
         */
        float labelPositionXRatio = 0.05f;
        float labelPositionYRatio = 0.025f;

        GameObject newTextObject = Instantiate(textObjectPrefab);
        newTextObject.transform.parent = parent;
        newTextObject.GetComponent<ApplyFont>().fontObject = textObjectFont;
        transLabel = newTextObject.GetComponent<TextMesh>();
        transLabel.name = "Trans Label";
        transLabel.text = transLabelText;
        transLabel.font = textObjectFont;
        transLabel.color = Color.grey;
        transLabel.anchor = TextAnchor.LowerLeft;
        transLabel.characterSize = 0.01f;
        transLabel.fontSize = 50;
        transLabelEnd = new Vector3(xMin + xWidth*labelPositionXRatio, yMin + yHeight*labelPositionYRatio, -layerSize);
        transLabelStart = transLabelEnd - new Vector3(0, yHeight/2f, -layerSize);
        transLabel.transform.localPosition = transLabelStart;
        transLabel.transform.localEulerAngles = Vector3.zero;
    }

    void InitTransmissionTransBox(Transform parent) {
        /*
         * Create the box used as a background for the main transmission text.
         * The edge ratios determines the size of the transmission text box.
         * The box's position and sizes is calculated when it is animated.
         * 
         * There only exists one box mesh for the transmission text, transBox. It's position is not 
         * relative to any other transmission gameObjects, but it's height should be relative to the transLabel
         */
        float heightRatio = 0.1f;
        float widthRatio = 0.9f;
        transBoxWidthRatio = widthRatio;
        transBoxHeightRatio = heightRatio;
        transBoxWidth = xWidth*transBoxWidthRatio;
        transBoxHeight = yHeight*transBoxHeightRatio;

        /* The box's y position is anchored to the transLabel's end position */
        transBoxBottom = transLabelEnd.y + textObjectFont.lineHeight*transLabel.characterSize*0.3f;
        transBoxTop = transBoxBottom + yHeight*transBoxHeightRatio;
        transBoxLeft = xMin + xWidth*(1 - widthRatio)/2f;
        transBoxRight = transBoxLeft + xWidth*transBoxWidthRatio;

		/* Set up the box's gameObject */
        transBox = InitMesh(transBox, "Transmission");
        transBox.transform.parent = parent;
        transBox.name = "Main Text Box";
    }

    void InitTransmissionStaticLabel(Transform parent) {
        /*
         * Create the label used to give a name to the static box. It is placed
         * to the right of the static box and above the transmission text box.
         * The label's position is set when the staticBox is done animating.
         * 
         * There exists only one label used for the static, staticLabel
         */

        GameObject newTextObject = Instantiate(textObjectPrefab);
        newTextObject.transform.parent = parent;
        newTextObject.GetComponent<ApplyFont>().fontObject = textObjectFont;
        staticLabel = newTextObject.GetComponent<TextMesh>();
        staticLabel.text = staticLabelText;
        staticLabel.font = textObjectFont;
        staticLabel.color = Color.grey;
        staticLabel.anchor = TextAnchor.LowerLeft;
        staticLabel.characterSize = 0.01f;
        staticLabel.fontSize = 75;

        /* Set the time spent animating the static label text being typed out
         * to be relative to how much text will be drawn onto the label */
        transTextStateTimings[(int)transTextStates.StaticLabel] = Mathf.CeilToInt(staticLabelText.Length * typingTimeInterval);

        staticLabel.text = "";
        staticLabelSpacing = textObjectFont.lineHeight*staticLabel.characterSize*0.05f;
        staticLabel.transform.localEulerAngles = Vector3.zero;
    }

    void InitTransmissionStaticBox(Transform parent) {
        /*
         * Initilize the box used as a background for the static.
         * The box's size and position is set when it is animated
         * 
         * There only exists one static box, transStaticBox
         */
        staticBox = InitMesh(staticBox, "Transmission");
        staticBox.name = "Static Box";
        staticBox.transform.localEulerAngles = Vector3.zero;

        /* Set the sizes of the static mesh's box */
        staticBoxBottom = transBoxBottom + yHeight*(transBoxHeightRatio/5f);
        staticBoxLeft = transBoxLeft - xWidth*(1 - transBoxWidthRatio)/3;
        staticBoxWidth = xWidth*0.15f;
        staticBoxHeight = yHeight*0.25f;
        currentStaticBoxBottom = staticBoxBottom;
        currentStaticBoxLeft = staticBoxLeft;
        currentStaticBoxWidth = staticBoxWidth;
        currentStaticBoxHeight = staticBoxHeight;

        /* Create an array of static (random 1-0 values) to use as a pool to generate static.
		 * The amount of entries in the pool is relative to the height of the static box */
        staticLineSeperator = 0.001f;
        staticPool = new int[Mathf.FloorToInt(5*staticBoxHeight/staticLineSeperator)];
        for(int i = 0; i < staticPool.Length; i++) {
            if(Rand(1, 0) < 0.5f) {
                staticPool[i] = 0;
            }
            else {
                staticPool[i] = 1;
            }
        }
    }
    
    void InitTransmissionTransMessage(Transform parent){
    	/*
    	 * Calculate where the transmission message will be placed using the transBox and staticBox's position values.
     	 *
     	 * There exists only one message, transMessage.
    	 */
    
    	GameObject newTextObject = Instantiate(textObjectPrefab);
        newTextObject.transform.parent = parent;
        newTextObject.GetComponent<ApplyFont>().fontObject = textObjectFont;
        transMessage = newTextObject.GetComponent<TextMesh>();
        transMessage.name = "Trans Message";
        transMessage.text = transmissionMessage;
        transMessage.font = textObjectFont;
        transMessage.lineSpacing = 0.75f;
        transMessage.alignment = TextAlignment.Left;
        transMessage.transform.localEulerAngles = Vector3.zero;

        /* Calculate the text sizes. By keeping fontSize*charSize = 1, the font will properly fit
         * into a height of 0.1. Depending on the height of the transBox, adjust the modVal to make the text fit */
        float modVal = transBoxHeight*10f;
        int fontSize = 100;
        float charSize = (1f/fontSize) * modVal;
        transMessage.characterSize = charSize;
        transMessage.fontSize = fontSize;
        
        /* Position the text in the transmission box */
        float startX = staticBoxLeft + staticBoxWidth;
        float startY = transBoxBottom + transBoxHeight;
        transMessage.transform.localPosition = new Vector3(startX, startY, -layerSize*1.5f);
        transMessage.anchor = TextAnchor.UpperLeft;

        /* Calculate how much width the transmission message has to work with */
        float messageMaxWidth = transBoxRight - startX;

        /* Format the transmissionMessage to include line breaks to prevent the text from leaving the transmission box.
         * Any time a line break is inserted, the text will get smaller and need to recalculate it's sizes. */
        float startingFontSize = transMessage.fontSize;
        int currentLineCount = 1;
        int maxLineCount = 1;
        int lastSpaceChar = 0;
        bool insertedLineBreak = false;
        do {
            CharacterInfo characterInfo;
            string currentMessageText = "";
            float currentMessageWidth = 0;
            char charSymbol;
            insertedLineBreak = false;
            
            for(int i = 0; i < transMessage.text.Length; i++) {
                /* Get the next character in the message and add it's length */
                charSymbol = transMessage.text[i];
                transMessage.font.GetCharacterInfo(charSymbol, out characterInfo, transMessage.fontSize);
                currentMessageWidth += characterInfo.advance*transMessage.characterSize/10;

                /* If the character is a line break character, reset the message width */
                if(charSymbol.Equals('\n')) {
                    currentMessageWidth = 0;
                }
                /* Track the last position of the newest space character to break on an empty space */
                else if(charSymbol.Equals(' ')) {
                    lastSpaceChar = i;
                }

                /* Ff needed, insert a line break and restart the loop with the a font size */
                if(currentMessageWidth >= messageMaxWidth) {
                    currentLineCount++;
                    currentMessageWidth = 0;
                    insertedLineBreak = true;

                    //Insert the line break at the last empty space
                    currentMessageText = currentMessageText.Insert(lastSpaceChar, "\n");
                    currentMessageText = currentMessageText.Substring(0, lastSpaceChar) + "\n";

                    //Update transMessage's text to reflect the new linebreak
                    currentMessageText += transMessage.text.Substring(lastSpaceChar+1);
                    transMessage.text = currentMessageText;
                    i = transMessage.text.Length;

                    //If the new lines are now overflowing the message box, change the font size and reset the message
                    if(currentLineCount > maxLineCount) {
                        transMessage.fontSize = (int) startingFontSize/currentLineCount;
                        maxLineCount++;
                        currentLineCount = 1;
                        transMessage.text = transmissionMessage;
                    }
                    
                }else {
                    /* Add the character to the new message */
                    currentMessageText += charSymbol;
                }
            }
        } while(insertedLineBreak);

        /* Once the line breaks have been properly inserted, update the transmissionMessage value with the new string */
        transmissionMessage = transMessage.text;
        transMessage.text  = "";

        /* Calculate how long it will take to animate the new message using it's character count */
        transTextStateTimings[(int) transTextStates.TransMessage] = Mathf.CeilToInt(transmissionMessage.Length * typingTimeInterval/5);
    }

    /* -------- In-Game trigger functions ------------------------------------------------------- */

    public void StartGame() {
        /*
         * Start the asteroid minigame by spawning three asteroids and setting the game state to 1
         */
        Debug.Log("start game");
        gameState = (int) State.AsteroidStage;
        InitAsteroid(0.04f);
        InitAsteroid(0.05f);
        InitAsteroid(0.07f);
    }

    void ShipDamaged() {
        /*
         * Handle any damage that will be send towards the player ship. 
         */

        if(ship.damaged == 0) {
            ship.damaged = 60;
            ship.currentFlicker = ship.flickerRate;
            Debug.Log("player damaged");
        }
    }

    void ShipKnockback(Asteroid a) {
        /*
         * Apply a knockback to the player ship when they get too close to an asteroid. 
         * The force of the knockback is determined by the size of the asteroid and the 
         * position the ship is relative to the asteroid's center.
         */
        float angle;

        /* Find the angle of the vertex formed by the line between the ship and asteroid's center */
        angle = Mathf.Atan((a.y - ship.y) / (a.x - ship.x));
        if(a.x - ship.x < 0) {
            ship.dx += a.size*2*Mathf.Cos(angle);
            ship.dy += a.size*2*Mathf.Sin(angle);
        }
        else {
            ship.dx -= a.size*2*Mathf.Cos(angle);
            ship.dy -= a.size*2*Mathf.Sin(angle);
        }

        /* Apply a random amount of rotation velocty to the ship */
        ship.phi += a.size*Rand(0.5f, 2f);
    }

    void AsteroidDamaged(Photon p, Asteroid a) {
        /*
         * When an asteroid is shot with a photon, it will get damaged. Whenever an asteroid is damaged,
         * destroy the photon and split the hit asteroids into two smaller asteroids. 
         */
        Asteroid child;
        float newSize;
        int childAsteroidCount;

        /* Split the original asteroid into two asteroids with sizes of 80% of the original */
        newSize = a.size * 0.8f;
        childAsteroidCount = 2;

        /* if the new size is double the minimum asteroid size, split the asteroid into 3 at 60% the original */
        if(newSize > asteroidSizeMin*2) {
            newSize = a.size * 0.6f;
            childAsteroidCount = 3;
        }

        /* Create the child asteroids if they are large enough */
        if(newSize > asteroidSizeMin) {
            for(int i = 0; i < childAsteroidCount; i++) {
                child = InitEmptyAsteroid(newSize);
                /* add some distance relative to the photon's direction to the child's position so all asteroids are not overlapping */
                if(p.dy < 0) {
                    child.x = a.x - Mathf.Sin(Mathf.Atan(p.dx/p.dy) + PI/2 - i*PI/(1+childAsteroidCount))*(child.size + a.size);
                    child.y = a.y - Mathf.Cos(Mathf.Atan(p.dx/p.dy) + PI/2 - i*PI/(1+childAsteroidCount))*(child.size + a.size);
                }
                else {
                    child.x = a.x + Mathf.Sin(Mathf.Atan(p.dx/p.dy) + PI/2 - i*PI/(1+childAsteroidCount))*(child.size + a.size);
                    child.y = a.y + Mathf.Cos(Mathf.Atan(p.dx/p.dy) + PI/2 - i*PI/(1+childAsteroidCount))*(child.size + a.size);
                }

                /* inherent a portion of its parents rotational speed and prevent it from spinning too fast */
                child.dphi = a.dphi*Rand(0.5f, 1.4f);
                if(child.dphi > 0.2) {
                    child.dphi = 0.2f;
                }
                else if(child.dphi < -0.2) {
                    child.dphi = -0.2f;
                }

                /* Use the photon's speed, the parent's speed and position of origin to determine the child's velocity */
                if(a.y - child.y >= 0) {
                    child.dx = -(Mathf.Sin(Mathf.Atan((a.x - child.x) / (a.y - child.y)))*child.size)*0.1f + a.dx*a.size*10 + p.dx*0.2f;
                    child.dy = -(Mathf.Cos(Mathf.Atan((a.x - child.x) / (a.y - child.y)))*child.size)*0.1f + a.dy*a.size*10 + p.dy*0.2f;
                }
                else {
                    child.dx = (Mathf.Sin(Mathf.Atan((a.x - child.x) / (a.y - child.y)))*child.size)*0.1f + a.dx*a.size*10 + p.dx*0.2f;
                    child.dy = (Mathf.Cos(Mathf.Atan((a.x - child.x) / (a.y - child.y)))*child.size)*0.1f + a.dy*a.size*10 + p.dy*0.2f;
                }

                /* Ensure the asteroid is not moving slow nough to remain outside the screen edges */
                if(child.dx < asteroidSpeedMin && child.dx > -asteroidSpeedMin) {
                    child.dx = child.dx/Mathf.Abs(child.dx) * asteroidSpeedMin;
                }
                if(child.dy < asteroidSpeedMin && child.dy > -asteroidSpeedMin) {
                    child.dy = child.dy/Mathf.Abs(child.dy) * asteroidSpeedMin;
                }

                /* Add the child to the asteroids list */
                asteroids.Add(child);

                /* Create dust particles bewteen the child asteroid and the original asteroid */
                InitDust(child, a);

            }
        }

        /* Split the original asteroid into debris */
        else {
            InitDebris(a);
        }

        /* Create photon residue where the photon was destroyed */
        InitPhotonResidue(p, a);

        /* Create dust where the original asteroid was */
        InitDust(a, a);

        /* Destroy the original asteroid and photon */
        a.active = false;
        CleanupInactiveObjects(asteroids);
        p.active = false;
        CleanupInactiveObjects(photons);
    }

    public int CalculateDebrisScore(Debris d) {
        /*
         * Calculate how much score the given debris piece is worth.
         * The score is proportional to the lifetime and has an upper limit.
         */
        int score;

        if(d.lifetime > 150) {
            score = 15;
        }
        else {
            score = Mathf.CeilToInt(d.lifetime/10f);
        }

        return score;
    }

    void AsteroidsComplete() {
        /*
         * Check if the player has destroyed all asteroids and there is no more debris floating around.
         * If that is the case, transition into the next stage/state of the game and create
         * TextMeshes that will be used with the transmission boxes.
         */

        if(asteroids.Count <= 0 &&  debris.Count <= 0) {
            Debug.Log("start next stage");

            /* Set up the text meshes and gameObjects used with the transmission windows */
            InitTransmissionObjects();

            /* Set the states of the transmission animation to their starting values */
            currentTransAnimState = 0;
            currentTransAnimTime = 0;

            /* Update the game state to reflect the new stage */
            gameState = (int) State.TransmissionAnimation;
        }
    }


    /* -------- Cleanup functions ------------------------------------------------------- */

    void CleanupInactiveObjects(ArrayList objects) {
        /*
         * Remove any objects in the given arrayList that contains an active variable set to false.
         */
        ArrayList inactiveObjectIndex = new ArrayList();
        int index = 0;

        /* Search the given list for inactive objects and track their index */
        foreach(ActiveObject a in objects) {
            if(!a.active) {
                inactiveObjectIndex.Add(index);
                if(a.gameObject != null) {
                    Destroy(a.gameObject);
                }
            }
            index++;
        }

        /* Remove the inactive objects from the given list in reverse order */
        inactiveObjectIndex.Reverse();
        foreach(int i in inactiveObjectIndex) {
            objects.RemoveAt(i);
        }
    }

    void DeleteScoreGain(scoreGain score) {
        /*
         * Delete the given scoreGain object from the game. This is called when a debris is
         * set to inactive and it needs to delete it's linked scoreGain object.
         */

        Destroy(score.gameObject);
    }

    /* -------- Outside Event functions ------------------------------------------------------- */

    public void ConvertInputs(UserInputs inputs) {
        /*
         * Convert a UserInput object into a set of variables that will be handled by the Androids game as inputs.
         * Asteroids uses the movement keys to control the ship and the spacebar/left-click to fire photon shots.
         */

        if(inputs.playerMovementXRaw != 1) { right = 0; } else { right = 1; }
        if(inputs.playerMovementXRaw != -1) { left = 0; } else { left = 1; }
        if(inputs.playerMovementYRaw != 1) { up = 0; } else { up = 1; }
        if(inputs.playerMovementYRaw != -1) { down = 0; } else { down = 1; }

        /* Ensure the player has to tap the left-mouse button/sapcebar to fire a photon shot */
        if(inputs.leftMouseButton == true || inputs.spaceBar == true) {
            if(firePhoton == 0) {
                InitPhoton();
                firePhoton = 1;
            }
            else if(firePhoton == 1) {
                firePhoton = 2;
            }
        }
        else {
            firePhoton = 0;
        }
    }

    public void LinkPlayer() {
        /*
         * Runs when a player gets linked to the game and sending inputs
         */

    }

    public bool UnlinkPlayer() {
        /*
         * Attempt to unlink the player from the asteroids game. Do not unlink the player if the game is in the 
         * animation state. Unlinking the game outside the finished state will set it to the inactive state,
         * waiting for a player to re-link themselves.
         */
        bool unlinked;

        /* Do not unlink the player in the animation state */
        if(gameState == (int) State.TransmissionAnimation) {
            unlinked = false;
        }

        /* Unlink the player and set their inputs to neutral */
        else {
            unlinked = true;
            up = 0;
            down = 0;
            left = 0;
            right = 0;
            firePhoton = 0;
            
            /* Set the game to inactive if they have not yet finished the asteroids game */
            if(gameState != (int) State.Finished) {
                gameState = (int) State.PausedAsteroid;
            }
        }

        return unlinked;
    }


    /* -------- Helper functions ------------------------------------------------------- */

    public Coords AsteroidCoordPosition(Asteroid asteroid, float sizeMod, int index) {
        /*
         * Calculate the position of the given asteroid's coordinate defined with the given index.
         * The sizeMod indicates how much of a size increase/decrease the asteroid will undergo
         */
        Coords newCoord = new Coords();
        float x, y;

        x = asteroid.x + Mathf.Sqrt(Mathf.Pow(asteroid.coords[index].x*sizeMod, 2) + Mathf.Pow(asteroid.coords[index].y*sizeMod, 2))
                *Mathf.Sin((asteroid.phi)+index*(2*PI)/(float) asteroid.nVertices);
        y = asteroid.y + Mathf.Sqrt(Mathf.Pow(asteroid.coords[index].x*sizeMod, 2) + Mathf.Pow(asteroid.coords[index].y*sizeMod, 2))
                *Mathf.Cos((asteroid.phi)+index*(2*PI)/(float) asteroid.nVertices);

        newCoord.x = x;
        newCoord.y = y;

        return newCoord;
    }

    public Coords[] CalculateShipPoints(float sizeMode) {
        /*
         * Return an array of 3 coordinates that represent the world position of the player ship's vertices.
         * The given sizeMod value will be used to alter the ship's overall size.
         */
        Coords[] shipPoints;
        Coords point;

        /* The ship is made of 3 points */
        shipPoints = new Coords[3];

        /* Get the nose/front point of the ship */
        point = new Coords();
        point.x = ship.x + ship.size*sizeMode*Mathf.Sin(ship.phi);
        point.y = ship.y + ship.size*sizeMode*Mathf.Cos(ship.phi);
        shipPoints[0] = point;

        /* Get one of the back points of the ship */
        point = new Coords();
        point.x = ship.x + ship.size*sizeMode*0.6f*Mathf.Sin(ship.phi + (135*PI/180));
        point.y = ship.y + ship.size*sizeMode*0.6f*Mathf.Cos(ship.phi + (135*PI/180));
        shipPoints[1] = point;

        /* Get the other back point of the ship */
        point = new Coords();
        point.x = ship.x + ship.size*sizeMode*0.6f*Mathf.Sin(ship.phi + (225*PI/180));
        point.y = ship.y + ship.size*sizeMode*0.6f*Mathf.Cos(ship.phi + (225*PI/180));
        shipPoints[2] = point;


        return shipPoints;
    }

    public bool LineCircleCollision(float x0, float y0, float rad, float x1, float y1, float x2, float y2) {
        /*
         * Return true if the given information gives us a circle that is in contact with a line
         */
        float lambda, dist;
        bool collision = false;

        lambda = ((x0 - x1)*(x2 - x1) + (y0 -y1)*(y2 - y1)) / (Mathf.Pow((x2 - x1), 2) + Mathf.Pow((y2 - y1), 2));
        dist = Mathf.Pow((x1 - x0 + lambda*(x2 - x1)), 2) + Mathf.Pow((y1 - y0 + lambda*(y2 - y1)), 2);

        if((lambda >= 0 && lambda <=1 && dist <= Mathf.Pow(rad, 2))) {
            collision = true;
        }

        return collision;
    }

    public bool LineCollision(float Ax, float Ay, float Bx, float By, float Cx, float Cy, float Dx, float Dy) {
        /*
         * Check if there is a collision between the two lines (A, B) and (C, D)
         */
        bool collision = false;
        float oriA;
        float oriB;
        float oriC;
        float oriD;

        /* get the orientations of the 4 possible 3 line combinations. 0 = colinear. 1 = clockwise. 2 = counterclockwise */
        /* orientation of A, B and C */
        oriD = (By - Ay) * (Cx - Bx) - (Bx - Ax) * (Cy - By);
        if(oriD > 0) {
            oriD = 1;
        }
        else if(oriD < 0) {
            oriD = 2;
        }

        /* orientation of A,B and D */
        oriC = (By - Ay) * (Dx - Bx) - (Bx - Ax) * (Dy - By);
        if(oriC > 0) {
            oriC = 1;
        }
        else if(oriC < 0) {
            oriC = 2;
        }

        //orientation of C, D and A
        oriB = (Dy - Cy) * (Ax - Dx) - (Dx - Cx) * (Ay - Dy);
        if(oriB > 0) {
            oriB = 1;
        }
        else if(oriB < 0) {
            oriB = 2;
        }

        /* orientation of C, D and B */
        oriA = (Dy - Cy) * (Bx - Dx) - (Dx - Cx) * (By - Dy);
        if(oriA > 0) {
            oriA = 1;
        }
        else if(oriA < 0) {
            oriA = 2;
        }

        /* There is a collision if the orientation between a line and either dots of the other line are different */
        if(oriD != oriC && oriB != oriA) {
            collision = true;
        }

        /* Check whether a point is positioned on a line while its colinear with said line */
        /*Check if C is on the line AB and if they are colinear */
        else if(oriD == 0 && ((Cx <= Ax || Cx <= Bx) && (Cx >= Ax || Cx >= Bx) &&
        ((Cy <= Ay || Cy <= By) && (Cy >= Ay || Cy >= By)))) {
            collision = true;
        }

        /* Check if D is on the line AB and if they are colinear */
        else if(oriC == 0 && ((Dx <= Ax || Dx <= Bx) && (Dx >= Ax || Dx >= Bx) &&
        ((Dy <= Ay || Dy <= By) && (Dy >= Ay || Dy >= By)))) {
            collision = true;
        }

        /* Check if A is on the line CD and if they are colinear */
        else if(oriB == 0 && ((Ax <= Cx || Ax <= Dx) && (Ax >= Cx || Ax >= Dx) &&
        ((Ay <= Cy || Ay <= Dy) && (Ay >= Cy || Ay >= Dy)))) {
            collision = true;
        }

        /* Check if B is on the line CD and if they are colinear */
        else if(oriA == 0 && ((Bx <= Cx || Bx <= Dx) && (Bx >= Cx || Bx >= Dx) &&
        ((By <= Cy || By <= Dy) && (By >= Cy || By >= Dy)))) {
            collision = true;
        }

        return collision;
    }

    public float Rand(float min, float max) {
        /*
         * Return a random float between the two given values
         */
        float random = (float) rand.NextDouble();

        return min + (max-min)*random;
    }

    public Coords RandPointInCircle() {
        /*
         * Return the coordinates of a uniformly random point within a circle of radius 1 at (0, 0)
         */
        Coords coords = new Coords();
        float angle, u, r;

        angle = Rand(0, 2*PI);
        u = Rand(0, 1) + Rand(0, 1);
        if(u > 1) { r = 2-u; } else { r = u; }
        coords.x = r*Mathf.Cos(angle);
        coords.y = r*Mathf.Sin(angle);

        return coords;
    }

    public void GLDrawRectangle(float z, float y, float x, float width, float height, int DrawingType, Color color) {
        /*
         * Draw a rectangle using the GL.Vertex3 calls. The user also gives the coodinates of the rect's corners by
         * giving the initial point, width and height. The given DrawType will change how the vertex commands
         * are called and the given color will obviously set the color.
         */

        GL.Begin(DrawingType);
        GL.Color(color);

        if(DrawingType == GL.LINES) {
            GL.Vertex3(z, y, x);
            GL.Vertex3(z, y + height, x);
            GL.Vertex3(z, y + height, x);
            GL.Vertex3(z, y + height, x - width);
            GL.Vertex3(z, y + height, x - width);
            GL.Vertex3(z, y, x - width);
            GL.Vertex3(z, y, x - width);
            GL.Vertex3(z, y, x);
        }

        else if(DrawingType == GL.QUADS) {
            GL.Vertex3(z, y, x);
            GL.Vertex3(z, y + height, x);
            GL.Vertex3(z, y + height, x - width);
            GL.Vertex3(z, y, x - width);
        }

        GL.End();
    }

    public Vector2[] CalculateUVs(Vector3[] vertices) {
        /*
         * Calculate the UVs of the given array of vertices
         */
        Vector2[] UVs = new Vector2[vertices.Length];

        for(int i = 0; i < vertices.Length; i++) {
            UVs[i] = new Vector2(vertices[i].x, vertices[i].z);
        }

        return UVs;
    }

    public void SetMesh(GameObject GO, Vector3[] Vertices, Vector2[] UVs, int[] triangles, Material material) {
        /*
         * Take the mesh of the given gameObject and assign the given vertex, UV and triangle
         * arrays to it. Apply the material to the MeshRenderer.
         */

        Mesh mesh = GO.GetComponent<MeshFilter>().mesh;
        mesh.Clear();
        mesh.vertices = Vertices;
        mesh.uv = UVs;
        mesh.triangles = triangles;

        MeshRenderer meshRenderer = GO.GetComponent<MeshRenderer>();
        meshRenderer.material = material;
    }

    public void SetMesh(GameObject GO, Vector3[] Vertices, Vector2[] UVs, int[][] triangles, Material[] material) {
        /*
         * Just like SetMesh, but will create submeshes using the set of triangles and materials.
         * The array of triangles and the array of materials must have the same amount of entries
         */

        Mesh mesh = GO.GetComponent<MeshFilter>().mesh;
        mesh.Clear();
        mesh.vertices = Vertices;
        mesh.uv = UVs;

        mesh.subMeshCount = triangles.Length;
        for(int i = 0; i < triangles.Length; i++) {
            mesh.SetTriangles(triangles[i], i);
        }

        MeshRenderer meshRenderer = GO.GetComponent<MeshRenderer>();
        meshRenderer.materials = material;
    }
}
