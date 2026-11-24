"""Static low-poly long-hood American tractor, metres, Y up / Z forward."""
from pathlib import Path
import math
import numpy as np

ROOT=Path(__file__).resolve().parents[2]
OUT=ROOT/'Assets/LevelDesign/Models/Vehicles/Trucks/LongHoodTruck'
V=[];F=[];objects=[]
COL={'Ivory':(.80,.83,.80),'Blue':(.025,.075,.26),'Metal':(.52,.58,.60),
     'Rubber':(.025,.03,.033),'Chassis':(.055,.065,.07),'Glass':(.075,.16,.19),
     'Lamp':(.80,.87,.82),'Amber':(.95,.30,.025),'Red':(.55,.025,.018)}
part=''
def group(name):
    global part
    part=name
def face(points,mat,center=None):
    points=np.array(points,float)
    if center is not None and np.dot(np.cross(points[1]-points[0],points[2]-points[0]),points.mean(axis=0)-center)<0:
        points=points[::-1]
    start=len(V);V.extend(points)
    for i in range(1,len(points)-1):
        if np.linalg.norm(np.cross(points[i]-points[0],points[i+1]-points[0]))>1e-10:
            F.append((start,start+i,start+i+1,mat,part))
def box(c,size,mat):
    c=np.array(c);d=np.array(size)/2
    p=[c+d*np.array(v) for v in [(-1,-1,-1),(1,-1,-1),(1,1,-1),(-1,1,-1),(-1,-1,1),(1,-1,1),(1,1,1),(-1,1,1)]]
    for ids in [(0,3,2,1),(4,5,6,7),(0,4,7,3),(1,2,6,5),(3,7,6,2),(0,1,5,4)]:face([p[i] for i in ids],mat,c)
def prism(profile,halfwidth,mat):
    # Convex YZ profile extruded across X; closed caps and sides.
    c=np.array([0,*np.mean(profile,axis=0)])
    rings=[[(s*halfwidth,y,z) for y,z in profile] for s in [-1,1]]
    for ring in rings:face(ring,mat,c)
    for i in range(len(profile)):
        j=(i+1)%len(profile)
        face([rings[0][i],rings[1][i],rings[1][j],rings[0][j]],mat,c)
def cylinder(a,b,r,mat,n=12):
    a=np.array(a,float);b=np.array(b,float);t=b-a;t/=np.linalg.norm(t)
    u=np.cross(t,[0,1,0] if abs(t[1])<.9 else [1,0,0]);u/=np.linalg.norm(u);v=np.cross(t,u)
    rings=[[p+r*(u*math.cos(ang)+v*math.sin(ang)) for ang in np.linspace(0,math.tau,n,endpoint=False)] for p in [a,b]]
    c=(a+b)/2
    for ring in rings:face(ring,mat,c)
    for i in range(n):j=(i+1)%n;face([rings[0][i],rings[1][i],rings[1][j],rings[0][j]],mat,c)
def side_patch(s,points,mat,x=1.071):
    face([(s*x,y,z) for y,z in points],mat,np.array([0,2,0]))

group('Frame')
for x in [-.52,.52]:box((x,.77,-.4),(.18,.3,7.7),'Chassis')
for z in [-3.9,-2.7,-1.4,0,1.4,2.6]:box((0,.77,z),(1.2,.16,.18),'Chassis')
group('Hood')
prism([(1.05,1.16),(1.05,3.54),(1.93,3.54),(2.12,1.16)],.79,'Ivory')
box((0,1.98,2.35),(.035,.025,2.25),'Metal')
group('Cab')
prism([(1.0,-.05),(1.0,1.24),(2.18,1.24),(3.22,.91),(3.30,-.05)],1.07,'Ivory')
group('Sleeper')
prism([(1.0,-1.65),(1.0,-.05),(3.30,-.05),(3.30,-1.45),(3.10,-1.65)],1.09,'Ivory')
box((0,3.32,-.65),(2.13,.06,1.65),'Ivory')

# Opaque glazing sits on a closed shell; no missing faces beneath window borders.
group('Windshield')
def front_z(y):return 1.24-(y-2.18)*(.33/1.04)+.012
for lo,hi in [(-.96,-.045),(.045,.96)]:
    face([(lo,2.39,front_z(2.39)),(hi,2.39,front_z(2.39)),(hi,3.10,front_z(3.10)),(lo,3.10,front_z(3.10))],'Glass',[0,2,0])
    cylinder((lo+.13,2.43,front_z(2.43)+.012),(hi-.10,2.57,front_z(2.57)+.012),.013,'Chassis',6)
box((0,3.22,1.0),(2.24,.075,.32),'Ivory')
for s in [-1,1]:
    group('Door_'+str(s))
    side_patch(s,[(2.36,.08),(2.36,1.13),(3.12,.88),(3.12,.08)],'Chassis',1.078)
    side_patch(s,[(2.41,.14),(2.41,1.06),(3.06,.84),(3.06,.14)],'Glass',1.082)
    for a,b in [((1.1,.02),(3.15,.02)),((1.1,.02),(1.1,1.15)),((1.1,1.15),(2.2,1.15))]:
        cylinder((s*1.078,*a),(s*1.078,*b),.009,'Chassis',4)
    box((s*1.09,1.93,.18),(.035,.07,.20),'Metal')
    group('BlueLivery_'+str(s))
    side_patch(s,[(1.95,-1.58),(1.95,1.18),(2.08,1.18),(2.08,-1.58)],'Blue',1.105)
    side_patch(s,[(2.14,-1.58),(2.14,1.18),(2.30,1.18),(2.30,-1.58)],'Blue',1.105)
    side_patch(s,[(2.30,-1.58),(2.30,-1.30),(3.08,-1.58)],'Blue',1.105)
    side_patch(s,[(2.34,-1.22),(2.34,-1.10),(3.08,-1.48),(3.08,-1.59)],'Blue',1.108)
    side_patch(s,[(1.56,1.26),(1.56,2.94),(1.85,3.12),(2.00,1.26)],'Blue',.796)
    group('Mirror_'+str(s))
    # Mount on solid door sheet above/below glazing, then stand off from the glass.
    for anchor_y,mirror_y in [(2.29,2.38),(3.20,2.96)]:
        box((s*1.078,anchor_y,.45),(.025,.07,.10),'Metal')
        cylinder((s*1.09,anchor_y,.45),(s*1.23,anchor_y,.45),.018,'Metal',6)
        cylinder((s*1.23,anchor_y,.45),(s*1.40,mirror_y,.58),.018,'Metal',6)
    box((s*1.40,2.67,.58),(.11,.65,.24),'Metal')
    box((s*1.40,2.67,.447),(.085,.58,.018),'Glass')
    group('Exhaust_'+str(s))
    cylinder((s*1.19,1.05,-.17),(s*1.19,3.14,-.17),.11,'Metal',12)
    cylinder((s*1.19,3.14,-.17),(s*1.19,4.20,-.17),.062,'Metal',10)
    cylinder((s*1.19,4.201,-.17),(s*1.19,4.208,-.17),.047,'Chassis',10)
    for y in [1.35,2.5,2.92]:cylinder((s*1.19,y-.025,-.17),(s*1.19,y+.025,-.17),.12,'Metal',12)
    group('FuelTank_'+str(s))
    cylinder((s*.98,.72,-1.46),(s*.98,.72,-.20),.32,'Metal',16)
    for z in [-1.25,-.40]:cylinder((s*.98,.72,z-.035),(s*.98,.72,z+.035),.333,'Chassis',16)
    group('Steps_'+str(s))
    for y,z in [(.46,.42),(.75,.36)]:box((s*1.10,y,z),(.42,.09,.68),'Metal')
    box((s*.99,.98,-1.98),(.50,.08,.65),'Metal')

# Tires retain actual volume, with simple low-poly hubs instead of tiny mesh details.
for axle,z in enumerate([2.35,-2.50,-3.78]):
    group('Axle_'+str(axle));cylinder((-1.13,.56,z),(1.13,.56,z),.10,'Chassis',10)
    for s in [-1,1]:
        centers=[s*1.02] if axle==0 else [s*.86,s*1.18]
        for wi,x in enumerate(centers):
            group('Wheel_%d_%d_%d'%(axle,s,wi))
            cylinder((x-.155,.56,z),(x+.155,.56,z),.55,'Rubber',20)
        x=s*(1.19 if axle==0 else 1.345)
        cylinder((x-s*.02,.56,z),(x+s*.035,.56,z),.335,'Metal',16)
        cylinder((x+s*.038,.56,z),(x+s*.055,.56,z),.255,'Chassis',16)
        cylinder((x+s*.058,.56,z),(x+s*.09,.56,z),.205,'Metal',12)
        cylinder((x+s*.09,.56,z),(x+s*.16,.56,z),.11,'Metal',10)
        for angle in np.linspace(0,math.tau,6,endpoint=False):
            y=.56+.27*math.sin(angle);zz=z+.27*math.cos(angle)
            cylinder((x+s*.036,y,zz),(x+s*.04,y,zz),.034,'Chassis',6)
for s in [-1,1]:
    group('FrontFender_'+str(s))
    angles=np.linspace(-.08,math.pi+.08,17)
    # Closed annular arch segments, centered on the front axle.
    for a,b in zip(angles,angles[1:]):
        profile=[(.56+r*math.sin(t),2.35+r*math.cos(t)) for r,t in [(.60,a),(.69,a),(.69,b),(.60,b)]]
        start=len(V);prism(profile,.30,'Ivory')
        for i in range(start,len(V)):V[i][0]+=s*1.02
    group('RearFender_'+str(s))
    box((s*1.04,1.18,-3.14),(.78,.09,2.40),'Ivory')
    box((s*1.04,.63,-4.31),(.70,1.05,.045),'Rubber')

group('Grille')
box((0,1.48,3.57),(1.47,1.34,.10),'Metal')
box((0,1.48,3.63),(1.30,1.19,.035),'Chassis')
for y in np.linspace(.92,2.04,20):box((0,y,3.66),(1.29,.023,.025),'Metal')
for x in [-.69,.69]:box((x,1.48,3.65),(.055,1.35,.06),'Metal')
box((0,2.14,3.61),(.40,.07,.04),'Metal')
group('FrontBumper')
box((0,.64,3.76),(2.61,.29,.23),'Metal')
for s in [-1,1]:
    group('Headlight_'+str(s))
    box((s*1.03,1.18,3.19),(.48,.27,.23),'Ivory')
    box((s*1.03,1.18,3.32),(.40,.18,.04),'Chassis')
    for x in [s*.92,s*1.14]:box((x,1.18,3.35),(.17,.13,.025),'Lamp')
    box((s*1.12,1.44,3.07),(.045,.16,.08),'Amber')
    group('TailLight_'+str(s));box((s*.86,.81,-4.35),(.24,.13,.06),'Red')
group('RoofMarkers')
for x in [-.86,-.43,0,.43,.86]:
    box((x,3.34,.72),(.13,.07,.20),'Metal');box((x,3.39,.77),(.09,.07,.12),'Amber')
group('FifthWheel')
box((0,1.07,-2.80),(1.15,.18,1.3),'Chassis')
cylinder((0,1.13,-2.8),(0,1.22,-2.8),.63,'Chassis',16)
box((0,1.225,-3.13),(.14,.018,.64),'Rubber')
group('RearCrossmember');box((0,.72,-4.25),(1.65,.22,.20),'Chassis')

OUT.mkdir(parents=True,exist_ok=True)
with (OUT/'LongHoodTruck.obj').open('w') as f:
    f.write('mtllib LongHoodTruck.mtl\n')
    for v in V:f.write('v %.6f %.6f %.6f\n'%tuple(v))
    for a,b,c,m,g in F:
        n=np.cross(V[b]-V[a],V[c]-V[a]);n/=np.linalg.norm(n)
        f.write('vn %.7f %.7f %.7f\n'%tuple(n))
    last=None
    for i,(a,b,c,m,g) in enumerate(F,1):
        if (g,m)!=last:f.write('g '+g+'\nusemtl '+m+'\ns off\n');last=(g,m)
        f.write(f'f {a+1}//{i} {b+1}//{i} {c+1}//{i}\n')
with (OUT/'LongHoodTruck.mtl').open('w') as f:
    for m,c in COL.items():f.write(f'newmtl {m}\nKd {c[0]} {c[1]} {c[2]}\nKs 0 0 0\nNs 0\nd 1\n\n')
assert all(np.isfinite(v).all() for v in V)
assert len(F)<14000
print('Truck:',len(F),'triangles;',len(set(f[4] for f in F)),'parts; bounds',np.ptp(V,axis=0))

# Software geometry preview; Unity materials are created by the editor importer.
source=(ROOT/'Tools/CarModel/build_coupe.py').read_text()
render_source=source[source.index('def render('):source.index("render('front.png'")]
render_source=render_source.replace("ROOT/'output/car'","ROOT/'output/truck'")
render_source=render_source.replace("scale=225", "scale=105").replace("[0,.75,0]", "[0,1.8,0]")
from PIL import Image
F=[f[:4] for f in F]
exec(render_source)
render('front.png',(10,5,12));render('rear.png',(-10,5,-12))

