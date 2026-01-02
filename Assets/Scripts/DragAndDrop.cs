using System.Numerics;
using NUnit.Framework;
using TMPro;
using Unity.VisualScripting;
using UnityEditor;
using UnityEditor.Search;
using UnityEngine;
using UnityEngine.InputSystem;

public class DragAndDrop : MonoBehaviour
{
    private GameObject drgObj;
    private UnityEngine.Vector3 centerPoint;
    [SerializeField] private float distance, objSpeed;
    [SerializeField] private UnityEngine.Vector3 offset;
    [SerializeField] private GameObject holdPoint;
    [SerializeField] bool isHandFull;
    [SerializeField] private TextMeshProUGUI interactText;
    [SerializeField] private UnityEngine.GameObject[] draggables;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        Drag();
        Onayla();
    }
    
    private void Drag()
    {
        centerPoint = new UnityEngine.Vector3(Screen.width / 2, Screen.height / 2, 0);

        Ray ray = Camera.main.ScreenPointToRay(centerPoint);

        if (Physics.Raycast(ray, out RaycastHit hit, distance) && hit.transform.gameObject.GetComponent<Draggable>() != null)
        {
            if (Input.GetMouseButtonDown(0) && !isHandFull)
            {
                isHandFull = true;
                drgObj = hit.transform.gameObject;
                drgObj.GetComponent<Draggable>().isDragging = true;
                drgObj.GetComponent<Rigidbody>().useGravity = false;

                Debug.Log("El Doldu Obje: " + drgObj.name);
                
            }
        }
        if (isHandFull && Input.GetMouseButton(0))
        {
            UnityEngine.Vector3 objYon = holdPoint.transform.position - drgObj.transform.position;
            drgObj.GetComponent<Rigidbody>().linearVelocity = (objYon * objSpeed);
            drgObj.GetComponent<Rigidbody>().angularVelocity = UnityEngine.Vector3.zero;
        }
        else if(isHandFull && !Input.GetMouseButton(0))
        {
            isHandFull = false;
            drgObj.GetComponent<Rigidbody>().useGravity = true;
            drgObj.GetComponent<Draggable>().isDragging = false;
            drgObj = null;
        }
        


        if (Input.GetMouseButtonDown(0) && hit.transform.gameObject != null  && hit.transform.gameObject.GetComponent<Draggable>() != null)
        {
            Debug.Log("Taşındı");
            
            //drgObj.transform.position = this.gameObject.transform.position + ;
            
        }
        
    }

    private void Onayla()
    {   
        Ray ray = Camera.main.ScreenPointToRay(centerPoint);
        if(Physics.Raycast(ray, out RaycastHit hit, distance) && hit.transform.tag == "Kasa" && hit.transform.GetComponent<Scanner>().toplam != 0)
        {
            interactText.text = "Onaylamak için 'E' tuşuna basın";
            if (Input.GetKeyDown(KeyCode.E))
            {
               draggables = GameObject.FindGameObjectsWithTag("Tasinabilir");
               for(int i = 0; i < draggables.Length; i++)
                {
                    if (draggables[i].GetComponent<Draggable>().isScanned)
                    {
                        Destroy(draggables[i]);
                    }
                }
                hit.transform.GetComponent<Scanner>().toplam = 0;
                hit.transform.GetComponent<Scanner>().ScannerReset();
            }
        }else
            interactText.text = "";

    }
}
