using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.Events;

// EventArgs for graph click
public class GraphClickEventArgs
{
	public Collider collider; // the collider that was clikced
	public int index; // the index of the triangle on the collider that was clicked

	public GraphClickEventArgs(Collider col, int i)
	{
		collider = col;
		index = i;
	}
}

// Event for graph click
public class GraphClickEvent : UnityEvent<GraphClickEventArgs>
{
	
}

public class tmg : MonoBehaviour 
{
	public static tmg mtmg;
	public GraphClickEvent onGraphClick = new GraphClickEvent();

	void Awake()
	{
		mtmg = this;
	}

	void Update()
	{
		if (Input.GetMouseButtonDown (0)) {
			RaycastHit hit;
			Ray ray = Camera.main.ScreenPointToRay (Input.mousePosition);

			// If collider was clicked
			if (Physics.Raycast (ray, out hit)) {
				int sibidx = hit.collider.gameObject.transform.GetSiblingIndex ();
				int trigdex = (hit.triangleIndex - hit.triangleIndex % 2) / 2;
				// Invoke graph click event
				onGraphClick.Invoke(new GraphClickEventArgs(hit.collider, 20000*sibidx + trigdex));
			}
		}
	}

}
