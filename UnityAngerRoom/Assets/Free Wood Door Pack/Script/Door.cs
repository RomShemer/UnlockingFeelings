using System.Collections;
using System.Collections.Generic;
using UnityEngine;
namespace DoorScript
{
	[RequireComponent(typeof(AudioSource))]


	public class Door : MonoBehaviour
	{
		public bool open;
		public float smooth = 1.0f;
		float DoorOpenAngle = 90.0f;
		float DoorCloseAngle = 0.0f;
		public GameObject fullPuzzleObject;
		public AudioSource asource;
		public AudioClip openDoor, closeDoor;
		// Use this for initialization
		public RoomManager roomManager;
		public BoxCollider boxCollider;
        void Start()
		{
			asource = GetComponent<AudioSource>();
			if (!fullPuzzleObject)
				fullPuzzleObject = GameObject.FindWithTag("FullPuzzle");

			if (roomManager == null)
			{
				roomManager = RoomManager.Instance;
            }

            boxCollider = GetComponent<BoxCollider>();


        }

            // Update is called once per frame
        void Update()
		{
			if (open)
			{
				Debug.Log("open = " + open + " | current rotation: " + transform.localEulerAngles);
				var target = Quaternion.Euler(0, DoorOpenAngle, 0);
				transform.localRotation = Quaternion.Slerp(transform.localRotation, target, Time.deltaTime * 5 * smooth);


            }
            else
			{
				var target1 = Quaternion.Euler(0, DoorCloseAngle, 0);
				transform.localRotation = Quaternion.Slerp(transform.localRotation, target1, Time.deltaTime * 5 * smooth);

            }
        }

		public void OpenDoor()
		{
			Debug.Log("entering open door");
            if (fullPuzzleObject) fullPuzzleObject.SetActive(false);
			if(boxCollider) boxCollider.isTrigger = true;
            open = !open;
			asource.clip = open ? openDoor : closeDoor;
			asource.Play();
            Debug.Log("done with open door");

		}
	}
}