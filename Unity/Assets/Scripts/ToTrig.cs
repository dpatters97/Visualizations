using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Annotation
{
	public static Dictionary<string, Color> labelMap;
	public string label;
	public Color color
	{
		get { return labelMap [this.label]; }
	}
	
}

public class ToTrig : MonoBehaviour 
{
	public GameObject go;
	public MeshFilter tmf;
	// Use this for initialization
	void Start () {
		//print (go.GetComponent<MeshFilter> ().mesh.triangles.Length);
		/*List<Vector3> translated = new List<Vector3>();
		foreach (Vector3 v in tmf.mesh.vertices) {
			print (v);
			translated.Add(v);
		}
		Matrix4x4 orientationm = GetOrientationMatrix (tmf.gameObject.transform.right, tmf.gameObject.transform.up, tmf.gameObject.transform.forward);
		foreach (Vector3 v in translated) {
			Matrix4x4 rm = Matrix4x4.identity;
			Quaternion q = Quaternion.Euler (go.transform.root.eulerAngles);
			rm.SetTRS (go.transform.position, q, go.transform.lossyScale);
			Vector3 center = go.GetComponent<MeshRenderer> ().bounds.center;
			print (v + " " + rm.MultiplyPoint3x4(v));
		}*/
		//List<Vector3> pointsd1 = ObjectToPoints (go, 1);
		//print (pointsd1.Count);
		MeshGenerator.GraphParticles (ObjectToPoints (go, 45));
		go.SetActive (false);
		//Bounds b = new Bounds ();

	}
	
	// Update is called once per frame
	void Update () {
		
	}

	public static List<Vector3> ObjectToPoints(GameObject obj, float density)
	{
		MeshFilter[] meshFilters = obj.GetComponentsInChildren<MeshFilter> ();

		List<Vector3> pointsMesh = new List<Vector3> ();
		foreach (MeshFilter mf in meshFilters)
			pointsMesh.AddRange (MeshToPoints (mf.mesh, density, mf.gameObject.transform));
		return pointsMesh;
	}

	// annotate
	/*
	public static List<Vector3>[] ObjectToAPoints(GameObject obj, float density)
	{
		MeshFilter[] meshFilters = obj.GetComponentsInChildren<MeshFilter> ();

		List<Vector3> pointsMesh = new List<Vector3> ();
		List<Vector3> colors = new List<Vector3> ();
		foreach (MeshFilter mf in meshFilters) 
		{
			List<Vector3> points = MeshToPoints (mf.mesh, density, mf.gameObject.transform);

			pointsMesh.AddRange (MeshToPoints (mf.mesh, density, mf.gameObject.transform));
		}
		return pointsMesh;
	}
*/
	public static List<Vector3> MeshToPoints(Mesh mesh, float density, Transform tf)
	{
		List<Vector3> points = new List<Vector3> ();
		//print (tf.position);
		//print (tf.rotation.eulerAngles);
		Matrix4x4 trs = Matrix4x4.identity;
		trs.SetTRS (tf.position, tf.rotation, tf.lossyScale);
		for (int i = 0; i < mesh.triangles.Length; i += 3) 
		{
			Vector3[] triangle = {
				trs.MultiplyPoint3x4(mesh.vertices [mesh.triangles [i]]), 
				trs.MultiplyPoint3x4 (mesh.vertices [mesh.triangles [i + 1]]),
				trs.MultiplyPoint3x4  (mesh.vertices [mesh.triangles [i + 2]])
			};

			/*foreach (Vector3 v3 in triangle)
				print ("**" + v3);*/

			points.AddRange (AddPointsToTrig (triangle, density));
		}

		return points;
	}

	public static List<Vector3> AddPointsToTrig(Vector3[] triangle, float density)
	{
		// find equation of plane triangle is in
		Vector3 P = triangle [0];
		Vector3 Q = triangle [1];
		Vector3 R = triangle [2];

		Vector3 PQ = Q - P;
		Vector3 PR = R - P;

		Vector3 cross = Vector3.Cross (PQ, PR);

		Plane p = new Plane (P, Q, R);
		// populate triangle on XZ plane
		Vector2 A = new Vector2(triangle[0].x, triangle[0].z);
		Vector2 B = new Vector2(triangle[1].x, triangle[1].z);
		Vector2 C = new Vector2(triangle[2].x, triangle[2].z);

		int n = Mathf.CeilToInt (cross.magnitude / 2 * density);
		List<Vector3> newPoints = new List<Vector3> ();
		for (int i = 0; i < n; i++) 
		{
			float u = UnityEngine.Random.value;
			float v = UnityEngine.Random.value;

			float rt2 = Mathf.Sqrt (2);
			Vector2 point = (1 - Mathf.Sqrt (u)) * A + (Mathf.Sqrt (u) * (1 - v)) * B + Mathf.Sqrt (u) * v * C;
			Ray ray = new Ray (new Vector3 (point.x, 0f, point.y), Vector3.up);
			float dist = 0f;
			p.Raycast (ray, out dist);
			Vector3 trigPoint = new Vector3 (point.x, dist/*Mathf.Abs((a * point [0] + c * point [1] + d) / Mathf.Sqrt (a * a + b * b + c * c))*/, point.y);
			newPoints.Add (trigPoint);
		}
		return newPoints;
	}
}
