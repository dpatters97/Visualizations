#import clr
#clr.LoadAssemblyFromFile('UnityEngine.dll')
#clr.AddReference('UnityEngine')
#clr.AddReference('Assembly-CSharp')
#import UnityEngine
#from UnityEngine import *
import random
import Graph
import UnityEngine
from UnityEngine import *

def rand_list(shape, scale=1, offset=0):
	ret = []
	if type(scale) != list:
		scale = [scale for _ in range(shape[1])]
	if type(offset) != list:
		offset = [offset for _ in range(shape[1])]
	for i in shape[0]:
		ret.append([random.random()*scale[j]+offset[j] for j in range(shape[1])])
	return ret

def example2():
	# create pos: (10,3), (40, 3)*1.5+2.5, (25,3)*.8+5, (30,3)+[3,0,-4]
	# append edges w/ groups (1,2); (2,3); (3,4); (2,4)
	pos1 = rand_list((10,3))
	pos2 = rand_list((40,3), 1.5, 2.5)
	pos3 = rand_list((25, 3), .8, 5)
	pos4 = rand_list((30, 3), 1, [3,0,-4])

	edges = []
	for i in range(10):
		for j in range(40):
			nl = random.randint(0,2)
			if nl < 1:
				n = random.randint(0,10)
				if n < 7:
					edges.append([i, j+10])
	for i in range(40):
		for j in range(25):
			nl = random.randint(0,3)
			if nl < 1:
				n = random.randint(0,10)
				if n < 6:
					edges.append([i+10, j+50])
	for i in range(25):
		for j in range(30):
			nl = random.randint(0,3)
			if nl < 1:
				n = random.randint(0,10)
				if n < 7:
					edges.append([i+50, j+75])
	for i in range(40):
		for j in range(25):
			nl = random.randint(0,5)
			if nl < 1:
				n = random.randint(0,10)
				if n < 7:
					edges.append([i+10, j+75])
	g = Graph(post, edges)

def get_stanford_data(filename='office_2.txt'):
	dat = []
	with open(filename) as f:
		for line in f:
			dat.append([float(num) for num in line.split()[0:3]])
	return dat

def example3():
	pts = get_stanford_data()
	g = Graph(pts, [])

def rot_and_rec(gph, az ,el, folder):
        def act():
                gph.RotateAndRecord(az, el, folder)
        return act
points = [[i, i%3, i] for i in range(10)]
edges = [[i, i+1] for i in range(9)]
g = Graph(points, edges)

# example: use RotateAndRecord directly (does not wait for graph to finish being made)
#g.RotateAndRecord([0,360,10], [0,70,2], 'pics/')

# example: add functions to onComplete (a list offunctions to execute once the graph is finished)
g.onComplete.Add(rot_and_rec(g, [0,360,12], [-60,60,4], 'pics/'))

# example: control graph properties
#g.highlightDepth = 3

# example: control camera directly (see Unity Scripting API)
#camera.transform.position = Vector3.zero

op = example3
#example3()
