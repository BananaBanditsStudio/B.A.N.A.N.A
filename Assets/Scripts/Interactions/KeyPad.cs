using UnityEngine;

public class KeyPad : Interactable
{
    [SerializeField]
    public GameObject door;
    private bool isOpen;
    
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    protected override void Interact()
    {
        isOpen = !isOpen;
        door.GetComponent<Animator>().SetBool("IsOpen", isOpen);
    }
}
