using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;

public class InputController : MonoBehaviour
{
    InputAction CameraPos;
    InputAction Cameralook;
    InputAction Clicking;


    public Transform CameraFollowed;

    Camera mainCamera;

    [SerializeField] float cameraFollowedSpeed;

    void Awake()
    {
        Cameralook = InputSystem.actions.FindAction("Look");
        CameraPos = InputSystem.actions.FindAction("Position");
        Clicking = InputSystem.actions.FindAction("Interact");

        mainCamera = Camera.main;
        
    }

    
    void Update()
    {
        if (Cameralook.IsPressed())
        {
            CameraFollowed.position += new Vector3(Cameralook.ReadValue<Vector2>().x, Cameralook.ReadValue<Vector2>().y, 0) * cameraFollowedSpeed * Time.deltaTime;
        }

    }


    //Click on something to select a country
    void ClickedOn()
    {
        Ray ray = mainCamera.ScreenPointToRay(CameraPos.ReadValue<Vector2>());
        RaycastHit2D hit = Physics2D.GetRayIntersection(ray);
        if (hit.collider != null && hit.transform.gameObject.tag == "Country" && !IsPointerOverUIElement())
        {
            if (GameManager.Instance.Startgame)
            {
                GameManager.Instance.CountryPanelSet(GameManager.Instance.Countries.Find(_ => _.Name == hit.collider.name));
            }
            else
            {
                GameManager.Instance.Countries.Find(_ => _.Name == hit.collider.name).IsCaptured = true;
                GameManager.Instance.Startgame = true;
            }
            
        }
    }
    
    //-------------------------Checking if Pointer on UI or not--------------------------
    public bool IsPointerOverUIElement()
    {
        return IsPointerOverUIElement(GetEventSystemRaycastResults());
    }
	bool IsPointerOverUIElement(List<RaycastResult> eventSystemRaysastResults)
	{
		for (int index = 0; index < eventSystemRaysastResults.Count; index++)
		{
			RaycastResult curRaysastResult = eventSystemRaysastResults[index];
			if (curRaysastResult.gameObject.layer == 5)
				return true;
		}
		return false;
	}
	List<RaycastResult> GetEventSystemRaycastResults()
	{
		PointerEventData eventData = new PointerEventData(EventSystem.current);
		eventData.position = CameraPos.ReadValue<Vector2>();
		List<RaycastResult> raysastResults = new List<RaycastResult>();
		EventSystem.current.RaycastAll(eventData, raysastResults);
		return raysastResults;
	}
    //-----------------------------------------------------------------------------------


    private void OnDisable()
    {
        if (Clicking != null)
            Clicking.Disable();

    }

    void OnEnable()
    {
        Clicking.started += _ => ClickedOn();
        Clicking.Enable();
    }
}