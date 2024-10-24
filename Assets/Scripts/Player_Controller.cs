using UnityEngine;

public class Player_Controller : MonoBehaviour
{
    void Update()
    {
        transform.Translate(new Vector3(Input.GetAxis("Horizontal"), 0, Input.GetAxis("Vertical"))*Time.deltaTime * 4f);
    }
}
