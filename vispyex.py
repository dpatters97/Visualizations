# pylint: disable=no-member
""" scatter using MarkersVisual """

import numpy as np
import sys
import math
import os

from vispy import app, visuals, scene, gloo
from vispy.gloo import *
from vispy.gloo.util import _screenshot
import faiss
import random
import matplotlib.pyplot as plt

# selection vars
# selected idxs, selectIncoming, selectOutgoing, selectionDepth
class HighlightOpts:
    def __init__(self):
        self.selectIncoming = False
        self.selectOutgoing = True
        self.selectionDepth = 1
        self.selectedIdxs = set()
        self.revert = False
        self.edge_toggle = True
        self.arrow_toggle = True

# main object used for graphs
class Canvas(scene.SceneCanvas):
    # init takes points [[x,y,z],...] and edge indices [[0,1],[5,12],...]
    def __init__(self, pos, edges):
        scene.SceneCanvas.__init__(self, keys='interactive', show=True)
        Scatter3D2 = scene.visuals.create_visual_node(visuals.MarkersVisual)
        
        # first unfreeze
        self.unfreeze()
        self.pos = pos # the input points
        self.view = self.central_widget.add_view()
        self.edges = edges
        self.color_list = [] #!
        self.cti = {} # color to index dictionary: (key, value) = (rgba color, index of point)
        self.vte_i = {} # vertex to edge, incoming: (key, value) = (destination point index, source point index)
        self.vte_o = {} # vertex to edge, outgoing: (key, value) = (source point index, destination point index)
        self.arrow_map = {} 
        self.arrow_colors = [] # what colors arrowVisual should be (for highlighting)
        self.index = faiss.IndexFlatL2(4) # use FAISS to retrieve index of point color clicked
        self.highlight = HighlightOpts()

        N = pos.shape[0]
        
        idx = 0
        count = 0
        # Rmember to make color scheme variable; currently enough for >=2M points
        res = 4 # space between each valid color
        for b in range(230, 1, -1):
            if idx >= N:
                break
            for r in range(b - 184, b+184):
                if idx >= N:
                    break
                if r < 0 or r > 255:
                    continue
                for g in range(b - 200, b+200):
                    if idx >= N:
                        break
                    if count % res != 0 or g < 0 or g > 255:
                        count+=1
                        continue
                    if b < 0 or g < 0 or r < 0:
                        print('o no')
                    self.color_list.append((r, g, b, 255))
                    self.cti[idx] = (r,g,b,255)
                    idx += 1
                    count += 1
        
        self.index.add(np.array(self.color_list).astype('float32')) # set up index
        self.vis = Scatter3D2(parent=self.view.scene)
        self.vis.set_gl_state('translucent', blend=True, depth_test=True)
        self.vis.set_data(pos, symbol='o', size=5, face_color=np.array(self.color_list)*(1/255.0),
                    edge_width=0.5, edge_color='white') # visualize points


        self.arrows = [] # arrows in ArrowVisual
        idx = 0
        for edge in self.edges: # populate vte_i, vte_o, and edges
            #print(edge)
            if edge[0] in self.vte_o:
                self.vte_o[edge[0]] |= {edge[1]}
            else:
                self.vte_o[edge[0]] = {edge[1]}

            if edge[1] in self.vte_i:
                self.vte_i[edge[1]] |= {edge[0]}
            else:
                self.vte_i[edge[1]] = {edge[0]}
            self.arrow_map[(edge[0], edge[1])] = idx
            idx += 1
            self.arrows.append(np.hstack((pos[edge[0]], (pos[edge[1]] + pos[edge[0]])*.5)))
        self.arrows = np.array(self.arrows)

        # create arrow visual
        ArrowConn = scene.visuals.create_visual_node(visuals.ArrowVisual)
        self.arrows_vis = ArrowConn(parent=self.view.scene)
        self.arrows_vis.set_gl_state('translucent', blend=True, depth_test=True)
        self.linecol = np.repeat([[.4,.4,.4,.4]], N, axis=0)
        self.arrows_vis.set_data(pos, connect=edges, arrows=self.arrows, color=self.linecol)
        self.arrow_colors = np.repeat([[.4,.4,.4,.4]],self.arrows.shape[0], axis=0)
        self.arrows_vis.arrow_color = self.arrow_colors

        # camera setup
        self.view.camera = 'turntable'
        self.view.camera.fov = 45
        self.view.camera.distance = 500
        
        self.freeze()

    # handles highlighting on mouse press
    def on_mouse_press(self, event):
        
        pixs = gloo.read_pixels() # get pixels on screen
        
        # mouse click coordinates
        x = event.pos[0]
        y = event.pos[1]
        
        target = []
        found = False
        for u in range(x-2, x+2): # try to find a fully-opaque pixel around click point
            if found:
                break
            for v in range(y - 2, y+2):
                if found:
                    break
                
                if pixs[v][u][3] == 255 and tuple(pixs[v][u]) != (0,0,0,255):
                    target = pixs[v][u]
                    found = True
                    print(v,u)

        if len(target) > 0: # get the nearest neighbor to the clicked color
            nearest = self.index.search(np.array([target]).astype('float32'), 1)
            nn = nearest[1][0][0]
            
            self.color(nn, revert = self.highlight.revert, depth=self.highlight.selectionDepth) # highlight 

    # example: handling different key presses
    def on_key_press(self, event):
        #print(dir(event))
        if event.key == 'e': # toggle edges on/off
            self.highlight.edge_toggle = not self.highlight.edge_toggle

            self.update_arrow_colors()
        elif event.key == 'a': # toggle arrows on/ off (doesn't work)
            self.highlight.arrow_toggle = not self.highlight.arrow_toggle
            self.update_arrow_colors()
        elif event.key == 'r': # increase selection depth by 1
            self.highlight.selectionDepth += 1
            print('selection depth:', self.highlight.selectionDepth)
        elif event.key == 'f': # decrease selection depth by 1
            self.highlight.selectionDepth = 1 if self.highlight.selectionDepth == 1 else self.highlight.selectionDepth - 1
            print('selection depth:', self.highlight.selectionDepth)
        elif event.key == 'z': # rotate camera 30 degrees on y axis
            self.view.camera.azimuth += 30
        elif event.key == 'm': # rotate camera 30 degrees on local x axis
            self.view.camera.elevation += 30
            print('roll')
        elif event.key == 'c':
            self.rotate_and_picture((0, 360, 6), el=(0,60,3),folder='tpics')
            
            
    # just shorthand for self.vis.set_data(...)
    def update_vertex_colors(self):
        self.vis.set_data(self.pos, symbol='o', size=8, face_color=np.array(self.color_list) * (1/255.0),
                    edge_width=0.5, edge_color='blue')
    # shorthand for updating arrow colors
    def update_arrow_colors(self):
        if self.highlight.edge_toggle:
            if self.highlight.arrow_toggle:
                self.arrows_vis.set_data(self.pos, connect=self.edges,arrows=self.arrows, color=self.linecol)
                self.arrows_vis.arrow_color = self.arrow_colors
            else:
                self.arrows_vis.set_data(self.pos, connect=self.edges,color=self.linecol)
        elif self.highlight.arrow_toggle:
            self.arrows_vis.set_data(self.pos, connect=np.array([[]]),arrows=self.arrows, color=self.linecol)
            self.arrows_vis.arrow_color = self.arrow_colors
        else:
            self.arrows_vis.set_data(self.pos, connect=np.array([[]]), color=self.linecol)
            
    # colors a particular vertex (and edge); not recursive    
    def color_particular(self, vert_idx, revert=False):
        arr_col = [1,1,1,1]
        vert_col = [255,255,255,255]
        if revert:
            arr_col = [.4,.4,.4,.4]
            vert_col = self.cti[vert_idx]

        self.color_list[vert_idx] = vert_col
        self.linecol[vert_idx] = arr_col

        if self.highlight.selectIncoming and vert_idx in self.vte_i:
            for other in self.vte_i[vert_idx]:
                if (other, vert_idx) in self.arrow_map:
                    self.arrow_colors[self.arrow_map[(other, vert_idx)]] = arr_col

        if self.highlight.selectOutgoing and vert_idx in self.vte_o:
            for other in self.vte_o[vert_idx]:
                if (vert_idx, other) in self.arrow_map:
                    self.arrow_colors[self.arrow_map[(vert_idx, other)]] = arr_col

    # cascading highlighting for graph
    def color(self, vert_idx, revert=False,depth = 1):
        if depth == 0:
            self.update_vertex_colors()
            self.update_arrow_colors()
            return
        if revert:
            self.color_particular(vert_idx, True)
        else:
            self.color_particular(vert_idx, revert=False)
            if self.highlight.selectIncoming and vert_idx in self.vte_i:
                for child in self.vte_i[vert_idx]:
                    self.color(child, revert=revert, depth=depth - 1)
            if self.highlight.selectOutgoing and vert_idx in self.vte_o:
                for child  in self.vte_o[vert_idx]:
                    self.color(child, revert=revert, depth=depth-1)
            self.update_vertex_colors()
            self.update_arrow_colors()
                
    # rotates camera and saves picture of view at each step
    def rotate_and_picture(self, az, el=(0,0,0), folder=''):

        el_angle = (el[1] - el[0])//(el[2] if el[2] != 0 else 1)
        az_angle = (az[1] - az[0])//(az[2] if az[2] != 0 else 1)

        i = 0
        for j in range(el[2] + 1):
            self.view.camera.elevation = el[0] + j * el_angle
            self.view.camera.azimuth = az[0]
            for k in range(az[2] + 1):
                self.view.camera.azimuth = az[0] + k * az_angle
                self.update()

                pixs = self.render()
                plt.imsave(fname = os.path.join(folder, '%i_az%iel%i.png' % (i, self.view.camera.azimuth, self.view.camera.elevation)), arr=pixs, format='png')
                
                i += 1

# Examples
# 2 clusters, 2M points, 500K edges
def example():
    pos = np.random.normal(size=(1000000, 3), scale=0.2)
    pos2 = np.random.normal(size=(1000000, 3), scale=0.3) + 1.5
    pos = np.vstack((pos, pos2))

    edges = np.random.randint(1999999, size=(500000, 2))
    canvas = Canvas(pos, edges)

# 4 clusters, 105 points, ~1000 edges
def example2():
    pos = np.random.normal(size = (10, 3))
    pos2 = np.random.normal(size=(40, 3))*1.5 + 2.5
    pos3 = np.random.normal(size = (25, 3))*.8 + 5
    pos4 = np.random.normal(size=(30,3)) + np.repeat([[3,0,-4]], 30, axis=0)
    post = np.vstack((np.vstack((np.vstack((pos, pos2)), pos3)), pos4))
    edges = []
    for i in range(10):
        for j in range(40):
            n1 = random.randint(0,2)
            if n1 < 1:
                n = random.randint(0,10)
                if n < 7:
                    edges.append([i,j + 10])
                else:
                    edges.append([j + 10,i])

    for i in range(40):
        for j in range(25):
            n1 = random.randint(0,3)
            if n1 < 1:
                n = random.randint(0,10)
                if n < 6:
                    edges.append([i+10,j + 50])
                else:
                    edges.append([j + 50,i+10])
    for i in range(25):
        for j in range(30):
            n1 = random.randint(0,3)
            if n1 < 1:
                n = random.randint(0,10)
                if n < 7:
                    edges.append([i+50,j + 75])
                else:
                    edges.append([j + 75,i+50])
    for i in range(40):
        for j in range(25):
            n1 = random.randint(0,5)
            if n1 < 1:
                n = random.randint(0,10)
                if n < 7:
                    edges.append([i+10,j + 75])
                else:
                    edges.append([j + 75,i+10])
    canvas = Canvas(post, np.array(edges))

# helper to load points from stanford data file
def get_standford_data(filename='/home/dpatters/Downloads/office_2/office_2.txt'):
    dat = []
    with open(filename) as f:
        for line in f:
            dat.append([float(num) for num in line.split()[0:3]])
    return dat
# load standford data of small office; ~900000 points
def example3():
    pos = np.array(get_standford_data())
    edges = np.array([])
    canvas = Canvas(pos,edges)



if __name__ == '__main__':
    example2()
    print("click on a point to highlight it\npress 'a' to toggle arrows, 'e' to toggle edges\npress 'r' to increase highlighting depth, 'f' to decrease")
    if sys.flags.interactive != 1:
        app.run()
        