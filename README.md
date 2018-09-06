# Visualizations
Two examples for visualizing point clouds and large graphs: one using Vispy in Python , and the other using Unity. To use the Unity example, first unzip the Lib.zip file as a folder named Lib. The Python file that the Unity example reads is called ipy.py (located in the Unity folder). No setup is required for the Vispy example other than having Vispy installed.

## Unity
This grapher works by creating a mesh and displaying it using the GraphShader shader. Opening the Unity project and pressing play will make the ipy.py Python script (located under Assets) execute. The current running example will create a small graph and use Graph.RotateAndRecord to rotate the camera around the graph and save pictures of the graph at each angle to the pics folder. To make and display a graph, run

``` Python
# points: a list of points in format [[x1,y1,z1],[x2,y2,z2],...]
# edges: a list of indices of connected points in format [[idx1,idx2],[idx3,idx4],...]
g = Graph(points, edges)
```
in Python, or
```C#
// points: List<Vector3>
# edges: indices of connected points as List<int[]>
Graph g = new Graph(points, edges)
```
in C#. The important files and classes are:

#### ipy.py
###### Location: Unity
This contains a few examples on how to use the Graph class from Python

**example2** : this example showcases interactive highlighting and cascading highlighting. The example contains 4 clusters all connected randomly.

**example3**: this example showcases loading point cloud data. The example loads data from Stanford BuildingParser and displays it. Note that the file (office2.txt) is not included, but any file in the same format will work

### Graph class
###### Location: Unity/Assets/Script/MeshGenerator.cs
Most importantly, this class contains members:

**highlightDepth**: how far down in edge connections to highlight. For instance, 1 will highlight just the point clicked, 2 will highlight the point clicked and its children, 3 will highlight the point clicked, children, and grandchildren, and so on

**RotateAndRecord**: rotates the camera by elevation (y-axis), azimuth (local x-axis) and input steps and saves pictures of the graph at each angle to the specified folder

### MeshGenerator class
###### Location: Unity/Assets/Scripts/MeshGenerator.cs
This class contains the methods that actually create the mesh for graphs. It also includes the method for running external Python files and examples for using graphs in C#. The Start function in MeshGenerator runs the program and example

### CameraMovement
###### Location: Unity/Assets/Scripts/CameraMovement.cs
For camera controls:

- w/s: up/down

- a/d: left/right

- q/e: rotate clockwise/counterclockwise

- r/f: zoom in/out

### ToTrig
###### Location: Unity/Assets/Scripts/ToTrig.cs
Most importantly contains

**MeshToPoints**: Takes a Mesh in Unity, adds random points according to the input density, and returns a point cloud as a list of Unity Vector3. The example uses this method on the church.obj 3D model to display the church using Unity's particle system (display function in MeshGenerator.cs)

### tmg
###### Location: Unity/Assets/Scripts/tmg.cs
Handles user clicks. Newly created graphs listen to the onGraphClicked event

### GraphShader
###### Location: Unity/Assets
The shader that points use. Most important for graph highlighting

## Vispy
This makes graphs by using the Canvas object which extends Vispy's SceneCanvas object. Unlike the Unity implementation, this uses point color to identify and highlight points that are clicked. For this reason, all graphs are multicolored. To make a graph, use

```Python
# points: a list of points in format [[x1,y1,z1],[x2,y2,z2],...]
# edges: a list of indices of connected points in format [[idx1,idx2],[idx3,idx4],...]
c = Canvas(points, edges)
app.run()
```

#### Canvas
This extends Vispy's SceneCanvas and most importantly includes:
**pos**: the input points, for example [[x,y,z]...]

**edges**: the edge indicies, for example [[0,1],...] denotes an edge from point index 0 to point index 1 and so on. Direction of edges also matters

**highlight** the highlighting options to use. More on the HighlightOpts class below

**color**: colors a point white at the given index

**rotate_and_picture**: Much like RotateAndRecord in the Unity implementation. Rotates the camera by elevation, azimuth, and input steps and saves pictures of the graph in each angle

**on_key_press**: key controls

- e: toggle edge display on/off

- r/f: increase/ decrease highlight.selectionDepth by 1

- z/m: rotate camera by 30 degrees on y-axis/ local x-axis

- c: call rotate_and_picture for 360 degrees in 6 steps on y-axis and 60 degrees in 3 steps on local x-axis

**example** creates 2 clusters of 1M points each and 500k edges connecting them

**example2**: creates 4 clusters of 105 points total and around 1000 edges

**example3**: loads data from Stanford BuildingParser and displays it. Note, the data file isn't included.

#### HighlightOpts
Contains display options for graphs:

**selectIncoming**: if True, highlights edges incoming to point selected

**selectOutgoing**: if True, highlihhts edges outgoing from point selected

**selectionDepth**: how far down in edge connections to highlight. For instance, 1 will highlight just the point clicked, 2 will highlight the point clicked and its children, 3 will highlight the point clicked, children, and grandchildren, and so on

**edge_toggle**: if True, displays edges
Note that a function in both examples use data from buildingparser.stanford.edu
