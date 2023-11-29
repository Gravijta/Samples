using UnityEngine;
using System.Collections;
using System.Collections.Generic;

[AddComponentMenu("HnS/Camera/Player Camera")]
public class PlayerCamera : MonoBehaviour
{

    public bool canRotateCamera;

    //NEW
    private List<Shader> originalShaders;

    private List<float> blockingMaterialTimers;
    private List<Material> blockingMaterials;
    private List<Material> newBlockingMaterials;
    private List<bool> blockingMaterialIsBlocking;
    private float blockingObjectRaycastTimer = 0.5f;
    Color tempColor;
    //private float 

    //OLD
    Camera camera;
    public float perspectiveZoomSpeed = 0.5f;        // The rate of change of the field of view in perspective mode.
    public float orthoZoomSpeed = 0.5f;        // The rate of change of the orthographic size in orthographic mode.
    Rect tapRect;
    public bool lockCamera;
    Vector3 lastPos;
    Vector3 curPos;
    public Vector3 startPos;
    public float tod;
    public float sunrise;
    public float sunset;
    private AudioSource[] audioSource = new AudioSource[3];
    //public AudioClip morningAudio;
    public Transform target;
    public string cameraTagName = "CameraTarget";
    public float walkDistance;
    public float runDistance;
    public float height;
    public float xSpeed = 250.0f;
    public float ySpeed = 120.0f;
    public float heightDamping = 2.0f;
    public float rotationDamping = 3.0f;

    private Transform _myTransform;
    private float _x;
    private float _y;
    //private bool camButtonDown = false;



    void Awake()
    {

        _myTransform = transform;
        tapRect = new Rect(Screen.width * 0.3f, Screen.height * 0.2f, Screen.width * 0.4f, Screen.height * 0.6f);
        camera = Camera.main;
        lastPos = startPos;
    }

    void Start()
    {
        audioSource = GetComponents<AudioSource>();
        CameraSetup();
        //startPos = _myTransform.position - target.position;
        //lastPos = startPos;
        _x = _myTransform.eulerAngles.y;
        _y = _myTransform.eulerAngles.x;

        originalShaders = new List<Shader>();
        blockingMaterials = new List<Material>();
        blockingMaterialTimers = new List<float>();
        blockingMaterialIsBlocking = new List<bool>();

        RotateCamera();
    }

    void Update()
    {
        if (target != null)
        {

            if (canRotateCamera && Input.touchCount == 1 && tapRect.Contains(Input.GetTouch(0).position))
            {

                _x += Input.GetTouch(0).deltaPosition.x * xSpeed * 0.002f;
                _y -= Input.GetTouch(0).deltaPosition.y * ySpeed * 0.002f;

                if (_y > 80)
                {
                    _y = 80;
                }
                else if (_y < 10)
                {
                    _y = 10;

                }

                RotateCamera();
            }
            // If there are two touches on the device...
            if (Input.touchCount == 2 && tapRect.Contains(Input.GetTouch(0).position) && tapRect.Contains(Input.GetTouch(1).position))
            {
                // Store both touches.
                Touch touchZero = Input.GetTouch(0);
                Touch touchOne = Input.GetTouch(1);

                // Find the position in the previous frame of each touch.
                Vector2 touchZeroPrevPos = touchZero.position - touchZero.deltaPosition;
                Vector2 touchOnePrevPos = touchOne.position - touchOne.deltaPosition;

                // Find the magnitude of the vector (the distance) between the touches in each frame.
                float prevTouchDeltaMag = (touchZeroPrevPos - touchOnePrevPos).magnitude;
                float touchDeltaMag = (touchZero.position - touchOne.position).magnitude;

                // Find the difference in the distances between each frame.
                float deltaMagnitudeDiff = prevTouchDeltaMag - touchDeltaMag;

                // If the camera is orthographic...
                if (camera.orthographic)
                {
                    // ... change the orthographic size based on the change in distance between the touches.
                    camera.orthographicSize += deltaMagnitudeDiff * orthoZoomSpeed;

                    // Make sure the orthographic size never drops below zero.
                    camera.orthographicSize = Mathf.Max(camera.orthographicSize, 0.1f);
                }
                else
                {
                    // Otherwise change the field of view based on the change in distance between the touches.
                    camera.fieldOfView += deltaMagnitudeDiff * perspectiveZoomSpeed;

                    // Clamp the field of view to make sure it's between 0 and 180.
                    camera.fieldOfView = Mathf.Clamp(camera.fieldOfView, 10f, 120f);
                }
            }
            else
            {

                // Always look at the target
                Vector3 position = new Vector3(lastPos.x + target.position.x, lastPos.y + target.position.y, lastPos.z + target.position.z);// +target.position;
                _myTransform.position = position;
                if(!lockCamera)
                _myTransform.LookAt(target);
            }
        }
        else
        {
            GameObject go = GameObject.FindGameObjectWithTag(cameraTagName);

            if (go == null)
                return;

            target = go.transform;
            CameraSetup();
            RotateCamera();
        }

        if (lockCamera)
            return;
        blockingObjectRaycastTimer -= Time.deltaTime;
        CheckBlockingObject();

    }

    private void CheckBlockingObject()
    {

        if (blockingObjectRaycastTimer <= 0)
        {
            Ray ray = camera.ScreenPointToRay(new Vector3(Screen.width * 0.5f, Screen.height * 0.5f));  //raycast to the middle of the screen, regardless of size/pixels
            LayerMask mask = LayerMask.GetMask("CameraBlocker", "Default");    //only hit objects with "CameraBlocker" Layer (ie walls, roofs)
            RaycastHit[] hits;  //use this for raycasting through objects.  use a layerMask to only return desired objects
            float dist = Vector3.Distance(_myTransform.position, target.position);
            hits = Physics.RaycastAll(ray, dist, mask);


            newBlockingMaterials = new List<Material>();
            for (int i = 0; i < hits.Length; i++)
            {
                for (int ii = 0; ii < hits[i].transform.GetComponent<Renderer>().materials.Length; ii++)
                {
                    newBlockingMaterials.Add(hits[i].transform.GetComponent<Renderer>().materials[ii]);
                }
                //newBlockingMaterials.Add(hits[i].transform.GetComponent<Renderer>().material);

            }

            foreach (RaycastHit r in hits)
            {

                if (!blockingMaterials.Contains(r.transform.GetComponent<Renderer>().material))
                {
                    for (int ii = 0; ii < r.transform.GetComponent<Renderer>().materials.Length; ii++)
                    {
                        originalShaders.Add(r.transform.GetComponent<Renderer>().materials[ii].shader);
                        blockingMaterials.Add(r.transform.GetComponent<Renderer>().materials[ii]);
                        blockingMaterialTimers.Add(0.5f);
                        blockingMaterialIsBlocking.Add(true);
                    }
                    //blockingMaterials.Add(r.transform.GetComponent<Renderer>().material);
                    //blockingMaterialTimers.Add(0.5f);
                    //blockingMaterialIsBlocking.Add(true);
                }

            }
            blockingObjectRaycastTimer = 0.5f;
        }



        for (int i = 0; i < blockingMaterials.Count; i++)
        {
            if (!newBlockingMaterials.Contains(blockingMaterials[i]))
            {
                blockingMaterialTimers[i] -= Time.deltaTime;
                LerpObjectColor(blockingMaterials[i], 0.2f, 1, blockingMaterialTimers[i]);  //0.2, 1
                if (blockingMaterials[i].color.a >= 0.95f)
                {
                    blockingMaterials[i].shader = originalShaders[i];
                    //blockingMaterials[i].shader = Shader.Find("Standard");
                    //hits[i].transform.GetComponent<Renderer>().material.shader = Shader.Find("Standard");
                    blockingMaterials.RemoveAt(i);
                    originalShaders.RemoveAt(i);
                    blockingMaterialIsBlocking.RemoveAt(i);
                    blockingMaterialTimers.RemoveAt(i);
                }


            }
        }

        if (blockingMaterials.Count > 0)
        {

            for (int i = 0; i < blockingMaterials.Count; i++)
            {
                if (blockingMaterialTimers[i] > 0 && blockingMaterialIsBlocking[i] == true)
                {
                    blockingMaterialTimers[i] -= Time.deltaTime;
                    blockingMaterials[i].shader = Shader.Find("Transparent/Diffuse");       //Shader.Find("Transparent/Diffuse");FX/Glass/Stained BumpDistort

                    LerpObjectColor(blockingMaterials[i], 1, 0.2f, blockingMaterialTimers[i]);
                    if (blockingMaterials[i].color.a <= 0.25f)
                    {
                        blockingMaterialIsBlocking[i] = false;
                        blockingMaterialTimers[i] = 0.5f;
                    }
                }

            }
        }
    }

    private void LerpObjectColor(Material m, float startAlpha, float endAlpha, float timer)
    {
        //Debug.Log("Lerp start, end, timer: " + startAlpha + ", " + endAlpha + ", " + timer);
        tempColor = m.color;
        tempColor.a = Mathf.Lerp(endAlpha, startAlpha, timer);
        m.color = tempColor;
    }

    private void RotateCamera()
    {
        if (lockCamera)
            return;
        Quaternion rotation = Quaternion.Euler(_y, _x, 0);

        Vector3 position = rotation * new Vector3(0, 0, -walkDistance) + target.position;

        _myTransform.rotation = rotation;
        _myTransform.position = position;
        lastPos = _myTransform.position - target.position;
        _myTransform.LookAt(target);
    }

    public void CameraSetup()
    {
        //Debug.Log("CAMERA CRAP! " + gameObject.name + "pos: " + _myTransform.position);
        //_myTransform.eulerAngles = new Vector3(68.69f, 314.9999f, 0);
        _myTransform.position = target.position + startPos;

		if (lockCamera)
			return;
		_myTransform.LookAt(target);
        _x = _myTransform.eulerAngles.y;
        _y = _myTransform.eulerAngles.x;
        //startPos = _myTransform.position - target.position;
        //lastPos = startPos;
        _myTransform.LookAt(target);

    }

}
