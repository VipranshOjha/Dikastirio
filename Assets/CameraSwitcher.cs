using UnityEngine;

public class CameraSwitcher : MonoBehaviour
{
    public Camera judgeCam;
    public Camera prosecutionCam;
    public Camera defenseCam;
    public Camera witnessCam;
    public Camera defendantCam;

    private Camera activeCam;

    void Start()
    {
        Debug.Log("CameraSwitcher Started");

        // Start with JudgeCam active
        SetActiveCamera(judgeCam);
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Alpha1))
        {
            Debug.Log("Key 1 pressed");
            SetActiveCamera(judgeCam);
        }
        if (Input.GetKeyDown(KeyCode.Alpha2))
        {
            Debug.Log("Key 2 pressed");
            SetActiveCamera(prosecutionCam);
        }
        if (Input.GetKeyDown(KeyCode.Alpha3))
        {
            Debug.Log("Key 3 pressed");
            SetActiveCamera(defenseCam);
        }
        if (Input.GetKeyDown(KeyCode.Alpha4))
        {
            Debug.Log("Key 4 pressed");
            SetActiveCamera(witnessCam);
        }
        if (Input.GetKeyDown(KeyCode.Alpha5))
        {
            Debug.Log("Key 5 pressed");
            SetActiveCamera(defendantCam);
        }
    }

    public void SetActiveCamera(Camera cam)
    {
        if (cam == null || cam == activeCam) return;

        if (activeCam != null)
        {
            var oldListener = activeCam.GetComponent<AudioListener>();
            if (oldListener) oldListener.enabled = false;
            activeCam.gameObject.SetActive(false);
            activeCam.tag = "Untagged";
        }

        cam.gameObject.SetActive(true);

        var newListener = cam.GetComponent<AudioListener>();
        if (newListener) newListener.enabled = true;

        cam.tag = "MainCamera";

        activeCam = cam;

        Debug.Log("Switched to: " + cam.name);
    }
}
