using UnityEngine;
using System.Collections;

/*
 * A basic laser gun that fires a laser every few seconds.
 * 
 * This basic laser fires a full line from it's origin to it's destination for a moment before going on cooldown.
 * This allows the shot to hit the target instantly.
 */
public class LaserGun : ShipWeapon {

    /* The laser object that will be "fired" */
    private GameObject laserBeam;

    /* The particle system that activates when the laser collides with an object */
    public ParticleSystem laserHitParticles;

    /* Power determines how much "active" the laser is, which determines it's size, length, etc */
    [HideInInspector]
    public float laserCurrentPower;
    public float laserMaxPower;

    /* How much power is put into the laser when active and reduced when not active */
    public float fireWeaponPower;
    public float dissipatePower;

    /* How long and wide the laser will be when at full power */
    public float laserMaxLength;
    public float laserMaxWidth;

    /* The current length of the laser. Lasers will stop short if they collide into an object */
    public float currentLaserLength;

    /* Whether the laser has dissipated at the end or collided into an object */
    public bool laserCollision;
    
    /* For how long power will be put into a laser once fired */
    [HideInInspector]
    public float currentActiveTime;
    public float maxActiveTime;

    /* The base damage of the laser */
    public float baseDamage;

    /* The material of the laser's mesh */
    private Material laserMaterial;

    /* How many points will be used to define the base of the cones on the laser's end */
    private int circlePointCount;
    


    /* -------- Built-in Unity Functions ------------------------------------------------------- */

    public void Start() {
        /*
         * Initilize the laser beam of the gun
         */

        /* Set the material used for the laser's mesh */
        laserMaterial = new Material(Shader.Find("Unlit/Color"));
        laserMaterial.color = Color.blue;

        /* Set up the mesh filter and renderer for the laser beam */
        ResetMesh();
        
        /* Calculate how many points will be used to define the base of the cones on the laser's ends */
        circlePointCount = 20;

        /* Create the mesh of the laser beam and assign it's triangles */
        UpdateLaserMesh();
        SetLaserMeshTriangles();

        laserBeam = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        laserBeam.transform.parent = transform;
        laserBeam.SetActive(false);

        ResetBeam();
    }

    public override void Update() {
        /*
         * Reduce the cooldowns on the weapon, reduce the power of the laser and update it's position/size.
         */
        float time = Time.deltaTime;

        /* Reduce the cooldowns of the weapon */
        ReduceCooldown(time);
        
        /* Update the power of the laser using currentActiveTime */
        UpdateLaserPower(time);

        /* Check if the laser runs into anything */
        CheckForCollisions();

        /* Update the size of the laser to reflect any changes */
        UpdateLaserSize();

        /* Update the mesh of the laser for this frame */
        UpdateLaserMesh();
    }


    /* -------- Inherited Weapon Functions ------------------------------------------------------- */

    public override void FireWeaponRequest() {
        /*
         * Check the cooldowns of the gun to see if it can fire
         */

        if(currentCooldown <= 0) {
            SuccessfulFire();
        }
    }

    public override void SuccessfulFire() {
        /*
         * Fire the weapon and reset it's cooldown
         */
         
        currentCooldown = maxCooldown;

        /* Start putting power into the laser */
        currentActiveTime = maxActiveTime;
    }

    public override void ReduceCooldown(float time) {
        /*
         * Reduce the current cooldown
         */

        /* Reduce the firing cooldown */
        currentCooldown -= time;
        if(currentCooldown < 0) {
            currentCooldown = 0;
        }
    }


    /* -------- Collision Detection Functions ------------------------------------------------------- */

    public void CheckForCollisions() {
        /*
         * Check if the laser has collided with any colliders. Lasers detect if they hit a collider using
         * a sphere cast from their origin. The sphere's radius is equal to the laser's width and it is
         * cast along the laser's length.
         * 
         * If the collider hit has a SpaceObject attached to their parent, run it's HitByLaser() function.
         */
        float sphereRadius = GetLaserWidth();
        float laserLength = GetLaserLength();
        RaycastHit laserHitInfo;

        /* The direction the laser is heading */
        Vector3 laserDirection = transform.forward;

        /* Place the center of the sphere where the weapon's firing point is */
        Vector3 sphereCenter = transform.position;

        /* Check if the laser collides with any space objects */
        if(Physics.SphereCast(sphereCenter, sphereRadius, laserDirection, out laserHitInfo, laserLength - sphereRadius)) {
            SpaceObject hitObject = laserHitInfo.collider.transform.GetComponent<SpaceObject>();

            /* Stop the laser at the collision point */
            currentLaserLength = laserHitInfo.distance;
            laserCollision = true;

            /* Place the laser hit particle emitter at the hit point and emit a burst of particles */
            laserHitParticles.transform.position = sphereCenter + laserDirection*laserHitInfo.distance;
            EmitLaserHitParticles();

            /* Send the hit object a HitByLaser signal if it's a SpaceObject */
            if(hitObject != null) {
                hitObject.HitByLaser(this);
            }
            else {
                Debug.Log("LASER HIT SOMETHING THAT IS NOT A SPACEOBJECT");
            }
        }
        else {
            /* The laser continued for it's full length and did not collide with anything */
            currentLaserLength = GetLaserLength();
            laserCollision = false;
        }
    }


    /* -------- Update Functions -------------------------------------------------------------------- */
    
    public void UpdateLaserPower(float time) {
        /*
         * Update the current power of the laser. Depending on the active state of the laser, change it's power.
         * The active state of the laser is reduced on each frame.
         */

        /* If the laser is active, apply more power to the laser */
        if(currentActiveTime > 0) {
            IncreaseLaserPower(time);
        }
        /* If the laser is not active, reduce the current power of the laser */
        else {
            DecreaseLaserPower(time);
        }

        /* Reduce the remaining active time of the laser */
        currentActiveTime -= time;
        if(currentActiveTime < 0) {
            currentActiveTime = 0;
        }
    }

    public void UpdateLaserMesh() {
        /*
         * Create a mesh used as the laser beam. It's comprised of two cones on both ends of the laser.
         * Connecting both cones forms the body of the laser. The circle that forms the body and the 
         * base of the cones has a size and a vertex count relative to the maximum width of the laser.
         */
        float circleRadius = GetLaserWidth();
        float lengthOfLaser = currentLaserLength;
        Mesh mesh = transform.GetComponent<MeshFilter>().sharedMesh;
        Vector3[] startingCone;
        Vector3[] endingCone;
        Vector3[] laserMeshPoints;

        
        /* Set the length of the laser's starting cone */
        float closeConeLength = laserMaxLength*0.01f;
        if(closeConeLength < 5) {
            closeConeLength = 5;
        }
        if(closeConeLength > currentLaserLength) {
            closeConeLength = currentLaserLength;
        }

        /* Set the sizes of the laser's ending cone. If the laser is hitting something, make the cone flat */
        float endConeLength = 10;
        if(laserCollision) {
            endConeLength = 0;
        }



        /* Get the points that form the cone protruding from the weapon's local firing point, (0, 0, 0) */
        startingCone = GetConeSpoints(circlePointCount, circleRadius, closeConeLength);

        /* Derive the points of the cone on the end of the laser using the cone from the start of the laser */
        endingCone = new Vector3[startingCone.Length];
        for(int i = 0; i < startingCone.Length-1; i++) {
            endingCone[i] = startingCone[i] + new Vector3(0, 0, lengthOfLaser - closeConeLength - endConeLength);
        }
        endingCone[startingCone.Length-1] = startingCone[startingCone.Length-1] + new Vector3(0, 0, lengthOfLaser);

        /* Combine both array of points */
        laserMeshPoints = new Vector3[startingCone.Length + endingCone.Length];
        System.Array.Copy(startingCone, laserMeshPoints, startingCone.Length);
        System.Array.Copy(endingCone, 0, laserMeshPoints, startingCone.Length, endingCone.Length);

        /* Assign the points to the mesh */
        mesh.vertices = laserMeshPoints;

        /* Optimize and calculate the needed values for the mesh to be complete */
        mesh.RecalculateNormals();
        mesh.RecalculateBounds();
        mesh.Optimize();
        mesh.name = "Laser Beam";
        transform.GetComponent<MeshRenderer>().material = laserMaterial;
    }


    /* -------- Event Functions -------------------------------------------------------------------- */
    
    public void IncreaseLaserPower(float time) {
        /*
         * Increase the power of the laser and prevent it from going above it's max
         */

        laserCurrentPower += fireWeaponPower*time;

        if(laserCurrentPower > laserMaxPower) {
            laserCurrentPower = laserMaxPower;
        }
    }

    public void DecreaseLaserPower(float time) {
        /*
         * Decrease the power of the laser and prevent it from going bellow 0
         */

        laserCurrentPower -= dissipatePower*time;

        if(laserCurrentPower < 0) {
            laserCurrentPower = 0;
        }
    }
    
    public float GetLaserLength() {
        /*
         * Return the max length of the laser with it's current power
         */

        return (laserCurrentPower/laserMaxPower)*laserMaxLength;
    }

    public float GetLaserWidth() {
        /*
         * Get the current width of the laser
         */

        return (laserCurrentPower/laserMaxPower)*laserMaxWidth;
    }
    
    public float GetLaserDamage() {
        /*
         * Calculate how much damage the laser can cause in it's current state
         */
        float damage = baseDamage;

        /* The laser's damage is increased when it's current power is 90% or more of it's max */
        if(laserCurrentPower >= laserMaxPower*0.9f) {
            damage *= 5f;
        }
        /* The laser deals less damage when it's power is bellow 50% */
        else if(laserCurrentPower < laserMaxPower*0.5f) {
            damage *= 0.25f;
        }

        return damage;
    }

    public void UpdateLaserSize() {
        /*
         * Update the size of the laser. The size of the laser is affected by laserCurrentPower
         */
        float length, width;

        /* Get the current length and width of the laser */
        length = currentLaserLength;
        width = GetLaserWidth();

        /* Set the laser size */
        laserBeam.transform.localScale = new Vector3(width, length/2, width);

        /* Reposition the laser */
        laserBeam.transform.localPosition = new Vector3(0, 0, length/2f);

    }

    public void ResetBeam() {
        /*
         * Reset the properties of the laser beam back to it's neutral state
         */

        laserBeam.transform.localPosition = new Vector3(0, 0, laserMaxLength/2f);
        laserBeam.transform.localEulerAngles = new Vector3(90, 0, 0);
        laserBeam.transform.localScale = new Vector3(1, laserMaxLength/2f, 1);
        Destroy(laserBeam.GetComponent<CapsuleCollider>());
    }

    public void EmitLaserHitParticles() {
        /*
         * Emit hit sparks from the laser hitting an obecjt using the laserHitParticles particle system.
         * The amount of particles emitted is all relative to the damage dealt
         */
        float baseParticleSpeed = 20;
        float baseParticleLifetime = 0.5f;

        /* Set the radius of the shape used to emit particles relative to the laser's width */
        var particleShape = laserHitParticles.shape;
        particleShape.radius = GetLaserWidth();

        /* Get the amount of damage the laser will do */
        float laserDamage = GetLaserDamage();
        Debug.Log(laserDamage);

        /* The speed, lifetime and amount of particles emitted are all relative to the damage dealt */
        //Set their speed
        laserHitParticles.startSpeed = baseParticleSpeed*(laserDamage/15f);
        //Set the lifetime
        laserHitParticles.startLifetime = baseParticleLifetime*(laserDamage/15f);
        //Emit the particles
        laserHitParticles.Emit((int) laserDamage);
    }
    
    public void ResetMesh() {
        /*
         * Reset the mesh renderer and mesh filter on the gameObject so 
         * it can be used to draw dthe laser beam fired by the weapon.
         */

        /* Add an empty mesh filter that will hold the hemisphere */
        MeshFilter meshFilter = transform.GetComponent<MeshFilter>();
        if(meshFilter == null) {
            meshFilter = gameObject.AddComponent<MeshFilter>();
        }
        else {
            meshFilter.mesh = null;
        }

        /* Make sure a mesh renderer is active */
        MeshRenderer meshRenderer = transform.GetComponent<MeshRenderer>();
        if(meshRenderer == null) {
            gameObject.AddComponent<MeshRenderer>();
        }

        /* Ensure there is an empty mesh in the mesh filter */
        Mesh mesh = meshFilter.sharedMesh;
        if(mesh == null) {
            meshFilter.mesh = new Mesh();
            mesh = meshFilter.sharedMesh;
        }
        mesh.Clear();
    }
    
    public void SetLaserMeshTriangles() {
        /*
         * Set the triangles that define the polygons that form the mesh of the laser
         */
        Mesh mesh = transform.GetComponent<MeshFilter>().sharedMesh;

        /* Get a count of all the polygons needed to draw the laser and use an array to track each polygon */
        int coneTrigCount = (circlePointCount+1)*3;
        int connectingConesTrigCount = (circlePointCount+1)*6;
        int[] triangles = new int[coneTrigCount*2 + connectingConesTrigCount];


        /* Add the starting cone's polygons */
        for(int i = 0; i < circlePointCount-1; i++) {
            triangles[i*3 + 0] = circlePointCount;
            triangles[i*3 + 1] = i+1;
            triangles[i*3 + 2] = i;
        }
        triangles[coneTrigCount - 3] = circlePointCount;
        triangles[coneTrigCount - 2] = 0;
        triangles[coneTrigCount - 1] = circlePointCount-1;

        /* Add the ending cone's polygons, using an offset of where we left off */
        int currentPolygonOffset = coneTrigCount;
        for(int i = 0; i < circlePointCount-1; i++) {
            triangles[currentPolygonOffset + i*3 + 0] = (circlePointCount+1) + circlePointCount;
            triangles[currentPolygonOffset + i*3 + 1] = (circlePointCount+1) + i;
            triangles[currentPolygonOffset + i*3 + 2] = (circlePointCount+1) + i+1;
        }
        triangles[currentPolygonOffset + coneTrigCount - 3] = (circlePointCount+1) + circlePointCount - 1;
        triangles[currentPolygonOffset + coneTrigCount - 2] = (circlePointCount+1);
        triangles[currentPolygonOffset + coneTrigCount - 1] = (circlePointCount+1) + circlePointCount;

        /* Add the polygons that connect the cone ends, updating the current polygon offset */
        currentPolygonOffset += coneTrigCount;
        for(int i = 0; i < circlePointCount-1; i++) {
            triangles[currentPolygonOffset + i*6 + 0] = (circlePointCount+1) + i;
            triangles[currentPolygonOffset + i*6 + 1] = i;
            triangles[currentPolygonOffset + i*6 + 2] = i+1;
            triangles[currentPolygonOffset + i*6 + 3] = (circlePointCount+1) + i+1;
            triangles[currentPolygonOffset + i*6 + 4] = (circlePointCount+1) + i;
            triangles[currentPolygonOffset + i*6 + 5] = i+1;
        }
        triangles[triangles.Length - 6] = (circlePointCount+1) + circlePointCount-1;
        triangles[triangles.Length - 5] = 0;
        triangles[triangles.Length - 4] = (circlePointCount+1);
        triangles[triangles.Length - 3] = (circlePointCount+1) + circlePointCount-1;
        triangles[triangles.Length - 2] = (circlePointCount-1);
        triangles[triangles.Length - 1] = 0;

        /* Assign the polygons to the mesh */
        mesh.triangles = triangles;
    }


    /* -------- Helper Functions -------------------------------------------------------------------- */

    public Vector3[] GetConeSpoints(int pointCount, float radius, float length) {
        /*
         * Return a vector array that contains points forming a cone using the given parameters.
         * The returned points form a cone with a tip at (0, 0, 0) that extends in the positive Z axis.
         * 
         * pointCount = How many points form the base circle of the cone. 
         * radius = The radius of the cone's base.
         * length = How far the tip of the cone is from the base.
         */
        Vector3[] hemispherePoints = new Vector3[pointCount+1];
        float currentAngle;

        for(int i = 0; i < pointCount; i++) {
            currentAngle = ((float) i/pointCount) *Mathf.PI*2;
            hemispherePoints[i] = new Vector3(radius*Mathf.Cos(currentAngle), radius*Mathf.Sin(currentAngle), length);
        }
        /* Make the last point the tip of the hemisphere */
        hemispherePoints[pointCount] = new Vector3(0, 0, 0);

        return hemispherePoints;
    }

}
