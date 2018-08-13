using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using IronPython;
using System.IO;

public class Graph
{
	// edge connections for cascading highlighting
	public Dictionary<int, List<int>> edgeDict;
	public Dictionary<int, List<int>> connectionDictIn;
	public Dictionary<int, List<int>> connectionDictOut;

	// mesh must be split into partitions since maximum # ov vertices is ~65000
	public List<List<Vector3>> pointPartitions;
	public List<List<Vector3[]>> edgePartitions;

	// gameobjects to hold graph
	public GameObject nodeObj;
	public GameObject edgeObj;
	public GameObject master;

	// how many generations to highlight
	public int highlightDepth = 1;

	// used to scale and reposition the graph once it's made
	private bool pointsFinished = false;
	private bool edgesFinished = false;

	// is graph constructed
	public List<Action> onComplete;
	// used for converting python 2D lists into lists of Vector3
	public static Vector3 ToVector3(IList<object> vec)
	{
		try
		{
			return new Vector3((int)vec[0], (int)vec[1], (int)vec[2]);
		}
		catch{
		}
		try
		{
			return new Vector3 ((float)((double)(vec [0])), (float)((double)(vec [1])), (float)((double)(vec [2])));
		}
		catch{
		}
		return new Vector3((float)vec[0], (float)vec[1], (float)vec[2]);
	}

	public Graph()
	{
		pointPartitions = new List<List<Vector3>> ();
		edgePartitions = new List<List<Vector3[]>> ();
		edgeDict = new Dictionary<int, List<int>> ();
		connectionDictIn = new Dictionary<int, List<int>> ();
		connectionDictOut = new Dictionary<int, List<int>> ();

		nodeObj = new GameObject (){name = "points"};
		edgeObj = new GameObject (){name = "edges" };
		master = new GameObject () {name = "graph" };
		nodeObj.transform.SetParent (master.transform);
		edgeObj.transform.SetParent (master.transform);

		onComplete = new List<Action> ();
	}

	// constuctor for Python
	public Graph(IList<object> ptsl, IList<object> edgesl) : this()
	{
		List<Vector3> pts = new List<Vector3> ();
		foreach (IList<object> pt in ptsl)
			pts.Add (ToVector3 (pt));

		List<Vector3[]> edges = new List<Vector3[]>();

		int j = 0;
		foreach (IList<object> ed in edgesl) 
		{
			int fro = (int)ed [0];
			int to = (int)ed [1];
			edges.Add (new Vector3[]{ pts [fro], pts [to] });

			if (!edgeDict.ContainsKey (fro))
				edgeDict.Add (fro, new List<int> ());
			if (!edgeDict.ContainsKey (to))
				edgeDict.Add (to, new List<int> ());
			edgeDict [fro].Add (2 * j);
			edgeDict [to].Add (2 * j + 1);

			if (!connectionDictOut.ContainsKey (fro))
				connectionDictOut.Add ((int)ed [0], new List<int> ());
			if (!connectionDictIn.ContainsKey (to))
				connectionDictIn.Add (to, new List<int> ());
			connectionDictOut [fro].Add (to);
			connectionDictIn [to].Add (fro);
			j++;
		}

		int nPartitions = Mathf.FloorToInt (pts.Count / 20000);
		int nEdgePartitions = Mathf.FloorToInt (edges.Count / 30000);
		for (int i = 0; i < nPartitions-1; i++) {
			pointPartitions.Add(pts.GetRange (i * 20000, 20000));

		}
		for (int i = 0; i < nEdgePartitions; i++) 
		{
			edgePartitions.Add(edges.GetRange(i*30000, 30000));
		}

		if(pts.Count % 20000 != 0)
			pointPartitions.Add(pts.GetRange ((nPartitions) * 20000, pts.Count - (nPartitions)*20000));
		if(edges.Count % 30000 != 0)
			edgePartitions.Add(edges.GetRange ((nEdgePartitions) * 30000, edges.Count - (nEdgePartitions)*30000));
		
		MeshGenerator.mg.StartCoroutine (MakePoints ());
		MeshGenerator.mg.StartCoroutine (MakeLines ());
		MeshGenerator.mg.StartCoroutine (ScaleGraph ());

		tmg.mtmg.onGraphClick.AddListener (delegate(GraphClickEventArgs arg0) {
			GraphClicked(arg0);
		});
	}

	// constructor for Unity (not up-to-date)
	public Graph(List<Vector3> pts, List<int[]> edgesl) : this()
	{
		List<Vector3[]> edges = new List<Vector3[]>();

		for (int i = 0; i < edgesl.Count; i++) 
		{
			edges.Add (new Vector3[]{ pts [edgesl [i] [0]], pts [edgesl [i] [1]] });
			if (!edgeDict.ContainsKey (edgesl [i] [0]))
				edgeDict.Add (edgesl [i] [0], new List<int> ());
			if (!edgeDict.ContainsKey (edgesl [i] [1]))
				edgeDict.Add (edgesl [i] [1], new List<int> ());
			edgeDict [edgesl [i] [0]].Add (2 * i);
			edgeDict [edgesl [i] [1]].Add (2 * i + 1);

			if (!connectionDictOut.ContainsKey (edgesl [i] [0]))
				connectionDictOut.Add (edgesl [i] [0], new List<int> ());
			if (!connectionDictIn.ContainsKey (edgesl [i] [1]))
				connectionDictIn.Add (edgesl [i] [1], new List<int> ());
			connectionDictOut [edgesl [i] [0]].Add (edgesl[i][1]);
			connectionDictIn [edgesl [i] [1]].Add (edgesl[i][0]);
		}

		int nPartitions = Mathf.FloorToInt (pts.Count / 20000);
		int nEdgePartitions = Mathf.FloorToInt (edges.Count / 30000);
		for (int i = 0; i < nPartitions-1; i++) {
			pointPartitions.Add(pts.GetRange (i * 20000, 20000));

		}
		for (int i = 0; i < nEdgePartitions; i++) 
		{
			edgePartitions.Add(edges.GetRange(i*30000, 30000));
		}
		pointPartitions.Add(pts.GetRange ((nPartitions) * 20000, pts.Count - (nPartitions)*20000));
		edgePartitions.Add(edges.GetRange ((nEdgePartitions) * 30000, edges.Count - (nEdgePartitions)*30000));
		MonoBehaviour.print (pointPartitions.Count);
		MonoBehaviour.print (edgePartitions.Count);
		MeshGenerator.mg.StartCoroutine (MakePoints ());
		MeshGenerator.mg.StartCoroutine (MakeLines ());

		tmg.mtmg.onGraphClick.AddListener (delegate(GraphClickEventArgs e) {
			GraphClicked(e);
		});
	}

	// converts [partition][triangle] coordinates to coordinates over full graph
	// and vice versa
	public static int[] SplitIndex(int idx, bool edge = false)
	{
		if (edge)
			return new int[]{ (idx - idx % 30000) / 30000, idx % 30000 };
		else
			return new int[]{ (idx - idx % 20000) / 20000, idx % 20000 };
	}

	// highlight with 1 coordinate
	public void Highlight(int idx, int depth=1)
	{
		int[] idxs = SplitIndex (idx);
		Highlight (idxs [0], idxs [1], depth);
	}

	// highlight, with 2 coordinates
	public void Highlight(int siblingIdx, int trigIdx, int depth=1)
	{
		int ptIdx = 20000 * siblingIdx + trigIdx;
		MeshFilter pmf = nodeObj.transform.GetChild(siblingIdx).gameObject.GetComponent<MeshFilter>();
		List<Vector3> ns = new List<Vector3> ();
		pmf.mesh.GetNormals (ns);
		for (int i = 0; i < 3; i++)
			ns [3 * trigIdx + i] = Vector3.up;
		pmf.mesh.SetNormals (ns);

		foreach (int eIdx in edgeDict[ptIdx]) {
			MonoBehaviour.print ("y");
			int[] subIdxs = SplitIndex (eIdx, true);

			MeshFilter emf = edgeObj.transform.GetChild (subIdxs [0]).gameObject.GetComponent<MeshFilter> ();
			List<Vector3> ens = new List<Vector3> ();
			emf.mesh.GetNormals (ens);
			MonoBehaviour.print (ens.Count + " " + eIdx);
			for (int i = 0; i < 1; i++)
				ens [eIdx + i] = Vector3.forward;
			emf.mesh.SetNormals (ens);
			foreach (Vector3 n in ens)
				MonoBehaviour.print (n);
		}

		if (depth > 1) {
			
			try
			{
				foreach (int childIdx in connectionDictOut[ptIdx])
					Highlight (childIdx, depth - 1);
			}
			catch{
			}
			try
			{
				foreach (int childIdx in connectionDictIn[ptIdx])
					Highlight (childIdx, depth - 1);
			}
			catch{
			}
		}
	}

	// scales and repositions graph to 15^3 unit box centered at origin
	IEnumerator ScaleGraph()
	{
		while (!(pointsFinished && edgesFinished))
			yield return new WaitForEndOfFrame ();
		Bounds b = MeshGenerator.GetMaxBounds (master);

		master.transform.localScale *= 15/b.size.magnitude;

		Bounds bb = MeshGenerator.GetMaxBounds (master);
		master.transform.position -= bb.center;

		DoActions ();
	}

	// make mesh for points
	IEnumerator MakePoints()
	{
		float t0 = 0, t1 = 0, t2 = 0;
		Vector3 avg = Vector3.zero;
		for (int i = 0; i < pointPartitions.Count; i++) 
		{
			GameObject go = new GameObject ();
			go.transform.SetParent (nodeObj.transform);
			go.name = "Mesh part " + i.ToString ();
			MeshFilter mf = go.AddComponent<MeshFilter> ();
			MeshRenderer mr = go.AddComponent<MeshRenderer> ();
			Mesh mesh = new Mesh ();

			Vector3[] verts = new Vector3[3*pointPartitions [i].Count];
			int[] trigs = new int[6 * pointPartitions [i].Count];
			Vector3[] norms = new Vector3[pointPartitions [i].Count];
			List<Vector2> uvs = new List<Vector2> ();

			for (int j = 0; j < pointPartitions [i].Count; j++) {
				if (j % 1000 == 0) {
					yield return i;
				}
				avg *= pointPartitions.Count * i + j;
				avg += pointPartitions [i] [j];
				avg /= (pointPartitions.Count * i + j + 1);
				MeshGenerator.SetTriag (pointPartitions [i] [j], verts, j*3);
				MeshGenerator.SetTriag_triangles(trigs, j*3, j*6);
				norms [j] = Vector3.up;
				uvs.Add (new Vector2(-.2f, 0f));
				uvs.Add (new Vector2 (.5f, 1.2f));
				uvs.Add (new Vector2(1.2f, 0f));
			}

			mesh.vertices = verts;

			mesh.triangles = trigs;
			mesh.uv = uvs.ToArray ();

			mr.material = MeshGenerator.mg.pmat;
			mr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
			mr.receiveShadows = false;
			mr.lightProbeUsage = UnityEngine.Rendering.LightProbeUsage.Off;
			mr.reflectionProbeUsage = UnityEngine.Rendering.ReflectionProbeUsage.Off;

			MeshCollider mc = go.AddComponent<MeshCollider> ();
			mc.sharedMesh = mesh;
			yield return 0;

			mesh.RecalculateNormals ();
			mesh.RecalculateBounds ();
			mf.mesh = mesh;
		}
		pointsFinished = true;
	}

	// make mesh for edges
	IEnumerator MakeLines()
	{
		for(int j = 0; j < edgePartitions.Count; j++){
			float t0 = 0, t1 = 0, t2 = 0;
			GameObject obj = new GameObject ();
			obj.transform.SetParent (edgeObj.transform);
			obj.name = "Line Mesh part " + j.ToString ();
			MeshFilter mf = obj.AddComponent<MeshFilter> ();
			MeshRenderer mr = obj.AddComponent<MeshRenderer> ();

			Mesh mesh = new Mesh ();
			mesh.subMeshCount = 1;

			List<int> indices = new List<int> ();
			List<Vector3> verts = new List<Vector3> ();

			for (int i = 0; i < edgePartitions[j].Count; i++) {
				verts.AddRange (edgePartitions[j] [i]);
				indices.Add (2 * i);
				indices.Add (2 * i + 1);
				if (i % 250 == 0) {
					yield return i;
				}
			}

			mesh.SetVertices (verts);
			mesh.SetIndices (indices.ToArray (), MeshTopology.Lines, 0);
			mesh.RecalculateNormals ();
			mr.material = MeshGenerator.mg.lmat;
			mf.mesh = mesh;

			mr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
			mr.receiveShadows = false;
			mr.lightProbeUsage = UnityEngine.Rendering.LightProbeUsage.Off;
			mr.reflectionProbeUsage = UnityEngine.Rendering.ReflectionProbeUsage.Off;
		}
		edgesFinished = true;
	}

	public void DoActions()
	{
		foreach (Action act in onComplete)
			act ();
	}

	// Handler for when graph is clicked
	// called by tmg script
	public void GraphClicked(GraphClickEventArgs e)
	{
		if (e.collider.gameObject == nodeObj || e.collider.gameObject.transform.IsChildOf (nodeObj.transform) )
			Highlight (e.index, highlightDepth);
	}

	public static void RotateAndRecord(IList<object> pyAz, IList<object> pyEl, string folder)
	{
		Vector3 azVec = ToVector3 (pyAz);
		Vector3 elVec = ToVector3 (pyEl);

		MeshGenerator.mg.StartCoroutine (RotateAndRecord (new float[]{ azVec.x, azVec.y, azVec.z }, new float[] {
			elVec.x ,
			elVec.y,
			elVec.z
		}, folder));
	}

	// rotate graph camera and save pictures for each rotation
	// argument arrays are expected in form: {start angle (deg), stop angle (deg), step (# of pictures to take)}
	// az: global y-axis rotation; el: local x-axis rotation
	public static IEnumerator RotateAndRecord(float[] az, float[] el, string folder="")
	{
		float az_angle = (az[1]-az[0])/az[2];
		float el_angle = (el [1] - el [0]) / el [2];

		Camera.main.gameObject.transform.RotateAround (Vector3.zero, Vector3.up, -1*Camera.main.gameObject.transform.eulerAngles.y);
		Camera.main.gameObject.transform.RotateAround (Vector3.zero, Camera.main.gameObject.transform.right, -1 * Camera.main.gameObject.transform.eulerAngles.x);

		Camera.main.gameObject.transform.RotateAround (Vector3.zero, Vector3.up, az [0]);
		Camera.main.gameObject.transform.RotateAround (Vector3.zero, Camera.main.gameObject.transform.right, el [0]);

		Vector3 initPos = Camera.main.gameObject.transform.position;
		Quaternion initRot = Camera.main.gameObject.transform.rotation;
		for (int i = 0, n=0; i <= el [2]; i++) 
		{
			for (int j = 0; j <= az [2]; j++, n++) {
				ScreenCapture.CaptureScreenshot (string.Format ("{0}{1}_az{2}el{3}.png", folder, n, az[0] + az_angle*j, el[0] + el_angle*i), 2);
				yield return new WaitForEndOfFrame ();
				if (j < az[2])
					Camera.main.gameObject.transform.RotateAround (Vector3.zero, Vector3.up, az_angle);
			}
			Camera.main.gameObject.transform.RotateAround (Vector3.zero, Vector3.up, -1 * (az [1]-az[0]));
			if(i < el[2])
				Camera.main.gameObject.transform.RotateAround (Vector3.zero, Camera.main.gameObject.transform.right, el_angle);
		}
	}
}
public class MeshGenerator : MonoBehaviour {

	public Material pmat;
	public static MeshGenerator mg;

	public Material lmat;

	void Start () 
	{
		mg = this;
		print (Environment.CurrentDirectory);
		Camera.main.gameObject.transform.RotateAround (Vector3.zero, Vector3.up, -1*Camera.main.gameObject.transform.eulerAngles.y);
		Camera.main.gameObject.transform.RotateAround (Vector3.zero, Camera.main.gameObject.transform.right, -1 * Camera.main.gameObject.transform.eulerAngles.x + 30);

		RunPython ("ipy.py", true);
	}

	// For debugging/ demonstrating rotation and picture taking
	/*
	void Update()
	{
		if (Input.GetKeyDown (KeyCode.Space)) {
			StartCoroutine(Graph.RotateAndRecord (new float[]{ 0, 360, 12 }, new float[]{ -60, 60, 4 }));
		}

		if (Input.GetKeyDown ("k"))
			Camera.main.gameObject.transform.RotateAround (Vector3.zero, Camera.main.gameObject.transform.right, -3.897f);
		if (Input.GetKeyDown ("o"))
			Camera.main.gameObject.transform.RotateAround (Vector3.zero, Vector3.up, -58.442f);
	}
	*/

	// executes python file or runs instructions in a string
	public void RunPython(string input, bool file)
	{
		var engine = IronPython.Hosting.Python.CreateEngine ();
		var unity = typeof(GameObject).Assembly;
		var gph = typeof(Graph).Assembly;
		engine.Runtime.LoadAssembly (unity);
		engine.Runtime.LoadAssembly (gph);
		var pth = engine.GetSearchPaths ();

		pth.Add(Environment.CurrentDirectory);
		pth.Add (Environment.CurrentDirectory + "/Lib");
		engine.SetSearchPaths (pth);
		var scope = engine.CreateScope ();
		scope.SetVariable ("camera", Camera.main.gameObject);

		if (file) 
		{
			var source = engine.CreateScriptSourceFromFile (input);
			source.Execute (scope);
		} 
		else 
		{
			var source = engine.CreateScriptSourceFromString (input);
			source.Execute (scope);
		}

		/*
		string ex = @"
import random
import numpy
op = str(random.randint(0,10))";
*/var froms = scope.GetVariable<Action> ("op");
		print (froms.GetType());
		print (froms);

	}

	// load stanford point cloud of small office; ~900000 points
	List<Vector3> LoadStanfordData(string filename = @"C:\Users\DaMarcus\Desktop\t\office_2.txt")
	{
		List<Vector3> points = new List<Vector3> ();
		float minx = Mathf.Infinity, maxx=Mathf.NegativeInfinity, miny=Mathf.Infinity, maxy=Mathf.NegativeInfinity, minz=Mathf.Infinity, maxz=Mathf.NegativeInfinity;
		int idx = 0;
		using (StreamReader sr = File.OpenText (filename)) 
		{
			while (sr.Peek () > -1) 
			{
				string[] line = sr.ReadLine ().Split(new char[]{' '});
				float x, y, z;
				if (!float.TryParse (line [0], out x) ||
				    !float.TryParse (line [1], out y) ||
				    !float.TryParse (line [2], out z))
					continue;
				else {
					points.Add (new Vector3 (x, y, z)*10);
					minx = Mathf.Min (minx, x);
					maxx = Math.Max (maxx, x);
					miny = Mathf.Min (miny, y);
					maxy = Math.Max (maxy, y);
					minz = Mathf.Min (minz, z);
					maxz = Math.Max (maxz, z);
					idx++;
				}
			}
		}
		return points;
	}

	// make a graph (point cloud) using Unity's particle system instead of a mesh
	public static void GraphParticles(List<Vector3> pts)
	{
		int N = pts.Count;
		ParticleSystem.Particle[] pparts = new ParticleSystem.Particle[N];

		for (int i = 0; i < N; i++) {
			pparts [i] = new ParticleSystem.Particle (){position=pts[i], startSize3D = new Vector3(.02f, .02f, .02f) };
		}
		GameObject go = new GameObject ();
		go.transform.position = Vector3.zero;
		ParticleSystem ps = go.AddComponent<ParticleSystem> ();
		ParticleSystemRenderer psr = go.GetComponent<ParticleSystemRenderer> ();

		ps.Stop ();
		ps.SetParticles (pparts, N);
	}

	// gets the maximum bounds of an object; used to scale graph
	public static Bounds GetMaxBounds(GameObject g) 
	{
		Bounds b = new Bounds();
		Renderer[] rs = g.GetComponentsInChildren<Renderer> ();
		for (int i = 0; i < rs.Length; i++) {
			if (i == 0)
				b = rs [i].bounds;
			else
				b.Encapsulate (rs [i].bounds);
		}

		return b;
	}

	// return small graph
	public static object[] SmallGraph()
	{
		List<Vector3> pts = new List<Vector3> ();
		for (int i = 0; i < 120; i++) {
			if (i < 30)
				pts.Add (2 * UnityEngine.Random.insideUnitSphere);
			else if (i < 80)
				pts.Add (5 * UnityEngine.Random.insideUnitSphere + 6.5f*Vector3.one);
			else
				pts.Add (3 * UnityEngine.Random.insideUnitSphere + 10*Vector3.one);
		}
		List<int[]> edges = new List<int[]> ();

		for (int i = 0; i < 30; i++) {
			for (int j = 0; j < 50; j++) {
				if (UnityEngine.Random.Range (0, 10) < 1)
					edges.Add (new int[]{ i, j + 30 });
			}
		}
		for (int i = 0; i < 50; i++) {
			for (int j = 0; j < 40; j++) {
				if (UnityEngine.Random.Range (0, 10) < 1)
					edges.Add (new int[]{ i + 30, j + 80 });
			}
		}
		return new object[]{ pts, edges };
	}

	// return small graph
	public static object[] SmallGraphS()
	{
		List<Vector3> pts = new List<Vector3> ();
		for (int i = 0; i < 120; i++) {
			if (i < 30)
				pts.Add (2 * UnityEngine.Random.insideUnitSphere);
			else if (i < 80)
				pts.Add (5 * UnityEngine.Random.insideUnitSphere + 6.5f*Vector3.one);
			else
				pts.Add (3 * UnityEngine.Random.insideUnitSphere + 10*Vector3.one);
		}
		List<int[]> edges = new List<int[]> ();

		for (int i = 0; i < 30; i++) {
			edges.Add (new int[]{ i, i + 30 });
		}
		return new object[]{ pts, edges };
	}

	// returns spiral
	public static List<Vector3> Spiral(int N)
	{
		float radius = .8f, theta=0f, dtheta = 7.5f / 180 * Mathf.PI;
		List<Vector3> pointsList = new List<Vector3> ();
		for (int i = 0; i < N; i++) {
			theta += dtheta;
			float x = UnityEngine.Random.value * 10;//radius * (float)Mathf.Cos (theta);
			float y = UnityEngine.Random.value * 10;//radius * (float)Mathf.Sin (theta);
			float z = UnityEngine.Random.value * 10;//1f * radius;
			float r = 10.1f - i * 0.002f;
			radius -= 0.25f;
			//parts [i] = new ParticleSystem.Particle (){position=UnityEngine.Random.insideUnitSphere*30, startSize3D = new Vector3(.02f, .02f, .02f) };
			pointsList.Add (new Vector3 (x, y, z));
			//index.Add (new Vector3 (x, y, z), i);
		}
		return pointsList;
	}

	// following methods take a point and populate input array with mesh data around the point
	public static void SetTriag(Vector3 point, Vector3[] arr, int startIndex, float r = .2f)
	{
		arr [startIndex] = new Vector3 (point.x - r, point.y - r, point.z);
		arr [startIndex + 1] = new Vector3 (point.x, point.y + r, point.z);
		arr [startIndex + 2] = new Vector3 (point.x + r, point.y - r, point.z);
	}

	public static void SetTrig(Vector3 point, Vector3[] arr, int startIndex, float r = .1f)
	{
		arr [startIndex] = new Vector3 (point.x - r, point.y - r, point.z + r);
		arr [startIndex + 1] = new Vector3 (point.x + r, point.y - r, point.z + r);
		arr [startIndex + 2] = new Vector3 (point.x, point.y - r, point.z - r);
		arr [startIndex + 3] = new Vector3 (point.x, point.y + r, point.z);
	}

	public static void SetTriag_triangles(int[] arr, int startIndex=0, int starttrig = 0)
	{
		arr [starttrig] = startIndex;
		arr [starttrig + 1] = startIndex + 1;
		arr [starttrig + 2] = startIndex + 2;
		arr [starttrig + 3] = startIndex + 2;
		arr [starttrig + 4] = startIndex + 1;
		arr [starttrig + 5] = startIndex;
	}

	public static void SetTrig_triangles(int[] arr, int startindex=0, int starttrig = 0)
	{
		arr [starttrig] = startindex;
		arr [starttrig + 1] = startindex + 3;
		arr [starttrig + 2] = startindex + 1;
		arr [starttrig + 3] =	startindex + 1;
		arr [starttrig + 4] = startindex + 3;
		arr [starttrig + 5] = startindex + 2;
		arr [starttrig + 6] = startindex + 2;
		arr [starttrig + 7] = startindex + 3;
		arr [starttrig + 8] = startindex;

	}
}
