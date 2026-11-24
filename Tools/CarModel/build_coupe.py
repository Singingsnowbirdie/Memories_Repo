"""Reference-shaped static touring coupe. Metres; X right, Y up, Z forward."""
from pathlib import Path
import math, json, uuid
import numpy as np
from PIL import Image, ImageDraw

ROOT=Path(__file__).resolve().parents[2]
OUT=ROOT/'Assets/LevelDesign/Models/Vehicles/Cars/GrandTouringCoupe'
OUT.mkdir(parents=True,exist_ok=True)
V=[]; F=[]; GROUPS=[]
part="Body"
def group(name):
    global part
    part=name
COL={'Paint':(.64,.48,.30),'Chrome':(.72,.77,.80),'Rubber':(.035,.038,.041),'Dark':(.055,.06,.065),'Glass':(.17,.34,.38),'Leather':(.24,.025,.045),'Headlamp':(.72,.85,.88),'Amber':(1,.25,.015),'Red':(.65,.015,.025),'White':(.85,.85,.77)}
def face(points,mat):
    start=len(V);V.extend(points)
    for i in range(1,len(points)-1):
        a,b,c=start,start+i,start+i+1
        if np.linalg.norm(np.cross(np.array(V[b])-V[a],np.array(V[c])-V[a])) > 1e-10:
            F.append((a,b,c,mat));GROUPS.append(part)
def box(c,s,m):
    x,y,z=c;a,b,d=np.array(s)/2
    p=[(x+i*a,y+j*b,z+k*d) for i,j,k in [(-1,-1,-1),(1,-1,-1),(1,1,-1),(-1,1,-1),(-1,-1,1),(1,-1,1),(1,1,1),(-1,1,1)]]
    for ids in [(0,3,2,1),(4,5,6,7),(0,4,7,3),(1,2,6,5),(3,7,6,2),(0,1,5,4)]:face([p[i] for i in ids],m)
def tube(points,r,m,n=4):
    n=min(n,6)
    points=np.array(points,float);rings=[]
    for i,p in enumerate(points):
        t=points[min(i+1,len(points)-1)]-points[max(0,i-1)];t/=np.linalg.norm(t)
        u=np.cross(t,[0,1,0] if abs(t[1])<.9 else [1,0,0]);u/=np.linalg.norm(u);v=np.cross(t,u)
        rings.append([p+r*(u*math.cos(a)+v*math.sin(a)) for a in np.linspace(0,2*math.pi,n,endpoint=False)])
    for a,b in zip(rings,rings[1:]):
        for j in range(n):face([a[j],b[j],b[(j+1)%n],a[(j+1)%n]],m)
    face(rings[0][::-1],m);face(rings[-1],m)
def ellipse(c,rx,ry,axis,m,r=.01):
    x,y,z=c
    pts=[(x+rx*math.cos(a),y+ry*math.sin(a),z) if axis=='z' else (x,y+ry*math.sin(a),z+rx*math.cos(a)) for a in np.linspace(0,2*math.pi,17)]
    tube(pts,r,m)
def disc(c,r,axis,m):
    x,y,z=c
    face([(x+r*math.cos(a),y+r*math.sin(a),z) if axis=='z' else (x,y+r*math.sin(a),z+r*math.cos(a)) for a in np.linspace(0,2*math.pi,(16 if r>.06 else 6),endpoint=False)],m)
def panel(p,m,trim=False):
    face(p,m)
    if trim:tube(p+[p[0]],.014,'Chrome')

# Rounded long body, high straight beltline and actual cut-out wheel arches.
zs=np.unique(np.concatenate([np.array([-2.45,-2.2,-1.3,0,1.35,1.5,2.15,2.25,2.45]), *[a+.425*np.cos(np.linspace(0,math.pi,13)) for a in [-1.48,1.42]]]))
def width(z):return np.interp(z,[-2.45,-2.2,-1.3,0,1.5,2.25,2.45],[.79,.87,.88,.865,.88,.84,.77])
def top(z):return np.interp(z,[-2.45,-1.3,0,1.35,2.15,2.45],[.96,1.035,1.04,1.04,1.0,.88])
for sign in [-1,1]:
    group("Body_Left" if sign>0 else "Body_Right")
    for z0,z1 in zip(zs,zs[1:]):
        def ring(z):
            low=.31
            for axle in [-1.48,1.42]:
                dz=z-axle
                if abs(dz)<.425:low=max(low,.36+math.sqrt(.425**2-dz**2))
            h=top(z);w=width(z)
            return [(sign*(w-.035),low,z),(sign*w,max(low,h-.20),z),(sign*(w-.025),h-.04,z),(sign*(w-.10),h,z)]
        a,b=ring(z0),ring(z1)
        for j in range(3):
            points=[a[j],b[j],b[j+1],a[j+1]]
            face(points if sign < 0 else points[::-1],'Paint')
    group("SideTrim_Left" if sign>0 else "SideTrim_Right")
    for axle in [-1.48,1.42]:
        pts=[(sign*(width(axle+.425*math.cos(a))+.004),.36+.425*math.sin(a),axle+.425*math.cos(a)) for a in np.linspace(0,math.pi,13)]
        tube(pts,.012,'Chrome')
    tube([(sign*(width(z)+.005),top(z)-.065,z) for z in [-2.45,-2.2,-1.3,0,1.35,1.5,2.15,2.25,2.45]],.012,'Chrome')
    tube([(sign*.87,.325,z) for z in np.linspace(-1.03,.97,2)],.017,'Chrome')
# Hood and trunk crowned cross-sections, closed rigid surfaces.
for lo,hi in [(-2.45,-.92),(.68,2.45)]:
    group("Trunk" if lo<0 else "Hood")
    for a,b in zip(np.unique(np.r_[lo,hi,[z for z in [-2.2,-1.3,0,1.35,1.5,2.15,2.25] if lo<z<hi]]),np.unique(np.r_[lo,hi,[z for z in [-2.2,-1.3,0,1.35,1.5,2.15,2.25] if lo<z<hi]])[1:]):
        for u,v in zip(np.linspace(-1,1,9),np.linspace(-1,1,9)[1:]):
            def pt(t,z):return (t*(width(z)-.10),top(z)+.035*(1-t*t),z)
            face([pt(u,b),pt(v,b),pt(v,a),pt(u,a)],'Paint')
group("Underbody")
box((0,.34,0),(1.50,.12,4.5),'Dark')
group("EndPanels")
for z in [-2.44,2.35]:box((0,.65,z),(1.56,.57,.035),'Paint')
# Close the crowned trunk edge against the top rear edge of the rear panel.
group("Trunk")
rear_lip_start=len(F)
for u,v in zip(np.linspace(-1,1,9),np.linspace(-1,1,9)[1:]):
    def deck_edge(t):return (t*(width(-2.45)-.10),top(-2.45)+.035*(1-t*t),-2.45)
    def panel_edge(t):return (t*(width(-2.45)-.10),.65+.57/2,-2.44-.035/2)
    face([deck_edge(u),deck_edge(v),panel_edge(v),panel_edge(u)],'Paint')
rear_lip_end=len(F)

# Cabin: low hardtop, thin A pillars, broad C pillars, transparent-look glazing.
group("Roof")
roof_front=.40;roof_back=-.72
for a,b in zip(np.linspace(roof_back,roof_front,2),np.linspace(roof_back,roof_front,2)[1:]):
    for u,v in zip(np.linspace(-1,1,9),np.linspace(-1,1,9)[1:]):
        def rp(t,z):return (.70*t,1.43+.055*(1-t*t),z)
        face([rp(u,b),rp(v,b),rp(v,a),rp(u,a)],'Paint')
group("Windshield")
panel([(-.76,1.075,.78),(.76,1.075,.78),(.69,1.43,.40),(-.69,1.43,.40)],'Glass',True)
group("RearWindow")
panel([(-.76,1.07,-1.06),(-.69,1.43,-.72),(.69,1.43,-.72),(.76,1.07,-1.06)],'Glass',True)
for s in [-1,1]:
    group("Cabin_Left" if s>0 else "Cabin_Right")
    def p(y,z):return (s*np.interp(y,[1.04,1.43],[.795,.70]),y,z)
    def side_panel(points,material):
        panel(points if s < 0 else points[::-1],material,True)
    # Bridge the exact windshield and side-window edges with an opaque A pillar.
    pillar=[(s*.76,1.075,.78),(s*.69,1.43,.40),p(1.42,.39),p(1.07,.74)]
    face(pillar if s < 0 else pillar[::-1],'Chrome')
    side_panel([p(1.07,.74),p(1.42,.39),p(1.42,-.12),p(1.07,-.12)],'Glass')
    side_panel([p(1.07,-.15),p(1.42,-.15),p(1.42,-.48),p(1.07,-.73)],'Glass')
    # Opaque divider joins the exact adjacent side-glass boundaries.
    divider=[p(1.07,-.12),p(1.42,-.12),p(1.42,-.15),p(1.07,-.15)]
    face(divider if s < 0 else divider[::-1],'Chrome')
    side_panel([p(1.07,-.75),p(1.43,-.49),p(1.43,-.73),p(1.07,-1.08)],'Paint')
    # Ribs follow both sloping edges, inset from the perimeter trim.
    # Lift them off the pillar so the painted surface cannot cut through them.
    for y in np.linspace(1.098,1.19,6):
        t=(y-1.07)/.36
        front=-.75+.26*t-.026
        rear=-1.08+.35*t+.026
        a=np.array(p(y,rear));b=np.array(p(y,front))
        a[0]+=s*.012;b[0]+=s*.012
        tube([a,b],.004,'Chrome',4)
    group("DoorTrim_Left" if s>0 else "DoorTrim_Right")
    # Closed door seam, handle, fuel flap, mirror.
    tube([(s*.875,.99,.68),(s*.88,.42,.68),(s*.88,.37,-.65),(s*.88,.98,-.65)],.0035,'Dark',6)
    tube([(s*.895,.90,-.48),(s*.92,.90,-.32)],.014,'Chrome')
    tube([(s*.88,.97,.56),(s*1.00,1.035,.51)],.014,'Chrome')
    ellipse((s*1.02,1.08,.51),.07,.075,'z','Chrome',.015)
    disc((s*1.02,1.08,.512),.065,'z','Chrome')
    tube([(s*.88,.77,-1.94),(s*.88,.93,-1.94),(s*.88,.93,-1.72),(s*.88,.77,-1.72),(s*.88,.77,-1.94)],.003,'Dark',6)
    for i,m in enumerate(['Red','White','Glass']):box((s*.889,.91,.22-i*.035),(.012,.03,.032),m)
group("Interior")
# Interior visible through glazing; no engine or underhood components.
box((0,.56,-.24),(1.4,.12,1.55),'Dark')
for x in [-.37,.37]:
    box((x,.67,-.02),(.62,.15,.56),'Leather')
    box((x,.91,-.30),(.62,.49,.13),'Leather')
    for dx in np.linspace(-.27,.27,5):tube([(x+dx,.70,.23),(x+dx,.75,-.23),(x+dx,1.14,-.24)],.007,'Leather',6)
box((0,.77,-.81),(1.35,.15,.36),'Leather');box((0,1.0,-.93),(1.35,.39,.12),'Leather')
box((0,.96,.59),(1.4,.14,.22),'Dark')
# Unity's OBJ handedness conversion mirrors X: positive source X is vehicle left.
group("SteeringWheel")
steering_x=.37
ellipse((steering_x,1.02,.34),.16,.16,'z','Dark',.015)
for a in [0,2.1,4.2]:tube([(steering_x,1.02,.34),(steering_x+.15*math.cos(a),1.02+.15*math.sin(a),.34)],.008,'Chrome')
# Wheels with rounded tire profile and five recessed slots.
for s in [-1,1]:
 for z in [-1.48,1.42]:
    group("Wheel_"+("Left" if s>0 else "Right")+("_Front" if z>0 else "_Rear"))
    profile=[(.73,.23),(.73,.32),(.76,.355),(.82,.367),(.94,.355),(.96,.32),(.96,.23)]
    for (x0,r0),(x1,r1) in zip(profile,profile[1:]):
        for a,b in zip(np.linspace(0,2*math.pi,17),np.linspace(0,2*math.pi,17)[1:]):
            face([(s*x0,.365+r0*math.sin(a),z+r0*math.cos(a)),(s*x1,.365+r1*math.sin(a),z+r1*math.cos(a)),(s*x1,.365+r1*math.sin(b),z+r1*math.cos(b)),(s*x0,.365+r0*math.sin(b),z+r0*math.cos(b))],'Rubber')
    disc((s*.965,.365,z),.235,'x','Chrome');ellipse((s*.971,.365,z),.232,.232,'x','Chrome',.014)
    for a in np.linspace(0,2*math.pi,5,endpoint=False):
        disc((s*.979,.365+.158*math.sin(a),z+.158*math.cos(a)),.049,'x','Dark')
        disc((s*.985,.365+.068*math.sin(a),z+.068*math.cos(a)),.012,'x','Chrome')
    disc((s*.989,.365,z),.047,'x','Chrome')
# Chrome wraparound bumpers, central tall grille, separate low side grilles.
for z in [-2.48,2.48]:
    group("RearBumper" if z<0 else "FrontBumper")
    tube([(-.85,.48,z*.95),(-.88,.48,z),(-.65,.48,z*1.015),(0,.48,z*1.025),(.65,.48,z*1.015),(.88,.48,z),(.85,.48,z*.95)],.066,'Chrome',12)
# Front grille rises into hood, following reference's tapered shield-like outline.
group("Grille")
outline=[(-.32,.50,2.50),(-.41,.93,2.43),(-.35,1.035,2.40),(0,1.06,2.40),(.35,1.035,2.40),(.41,.93,2.43),(.32,.50,2.50)]
panel(outline,'Dark');tube(outline+[outline[0]],.025,'Chrome',10)
for y in np.linspace(.54,1.0,12):
    w=np.interp(y,[.54,.93,1.0],[.32,.39,.35]);z=np.interp(y,[.5,1.06],[2.505,2.41])
    tube([(-w,y,z),(w,y,z)],.004,'Chrome',4)
for x in np.linspace(-.30,.30,7):tube([(x,.53,2.507),(x,1.015,2.414)],.003,'Chrome',4)
for s in [-1,1]:
    group("LightsAndTrim_Left" if s>0 else "LightsAndTrim_Right")
    disc((s*.68,.91,2.50),.133,'z','Headlamp');ellipse((s*.68,.91,2.505),.14,.14,'z','Chrome',.018)
    for x in np.linspace(-.10,.10,5):
        h=math.sqrt(.12**2-x*x);tube([(s*.68+x,.91-h,2.507),(s*.68+x,.91+h,2.507)],.002,'White',4)
    box((s*.61,.65,2.46),(.40,.14,.025),'Dark')
    for y in np.linspace(.59,.71,7):tube([(s*.41,y,2.48),(s*.81,y,2.48)],.004,'Chrome',4)
    disc((s*.76,.65,2.496),.046,'z','Amber');ellipse((s*.76,.65,2.50),.048,.048,'z','Chrome',.006)
    # rear vertical red lenses, white reversing segment, restrained fins.
    box((s*.76,.79,-2.46),(.13,.27,.035),'Chrome');box((s*.76,.82,-2.483),(.105,.19,.018),'Red');box((s*.76,.695,-2.484),(.105,.045,.018),'White')
    # Follow the actual rear shoulder, including its downward taper.
    # Half-embed the trim instead of suspending a straight bar over the deck.
    tube([(s*(width(z)-.10),top(z)-.003,z)
          for z in np.linspace(-1.15,-2.38,5)],.007,'Chrome',4)
    tube([(s*.50,.25,-2.25),(s*.50,.25,-2.53)],.036,'Dark',12)
    tube([(s*.56,1.08,.70),(s*.30,1.095,.73)],.006,'Chrome',6)
group("Badges")
for i,m in enumerate(['Red','White','Glass']):box((.16+i*.045,.585,2.515),(.043,.038,.012),m)
box((0,.45,-2.57),(.30,.12,.012),'Dark')
group("Antenna")
tube([(.46,1.075,-1.15),(.46,1.68,-1.20)],.003,'Chrome',6)

group("Roof")
# Close roof header seams.
for z in [roof_back,roof_front]:
    for u,v in zip(np.linspace(-1,1,9),np.linspace(-1,1,9)[1:]):
        # Rear header meets the glass at its exact width and faces out of the cabin.
        lower_width=.69 if z == roof_back else .70
        header=[(lower_width*u,1.43,z),(lower_width*v,1.43,z),
                (.70*v,1.43+.055*(1-v*v),z),(.70*u,1.43+.055*(1-u*u),z)]
        face(header[::-1] if z == roof_back else header,'Paint')
# Single static mesh, material submeshes; no animation or mechanical internals.
with (OUT/'GrandTouringCoupe.obj').open('w') as f:
    f.write('mtllib GrandTouringCoupe.mtl\n')
    for p in V:f.write('v %.6f %.6f %.6f\n'%tuple(p))
    for a,b,c,mat in F:
        n=np.cross(np.array(V[b])-V[a],np.array(V[c])-V[a]);length=np.linalg.norm(n)
        n=n/length if length>1e-10 else np.array([0.,1.,0.])
        f.write('vn %.7f %.7f %.7f\n'%tuple(n))
    for part_name in dict.fromkeys(GROUPS):
        f.write('g '+part_name+'\ns off\n')
        for m in COL:
            selected=[(ni,a,b,c) for ni,(a,b,c,mat) in enumerate(F,1) if mat==m and GROUPS[ni-1]==part_name]
            if not selected:continue
            f.write('usemtl '+m+'\n')
            for ni,a,b,c in selected:f.write(f'f {a+1}//{ni} {b+1}//{ni} {c+1}//{ni}\n')
with (OUT/'GrandTouringCoupe.mtl').open('w') as f:
    for m,c in COL.items():f.write(f'newmtl {m}\nKd {c[0]} {c[1]} {c[2]}\nKs 0.4 0.4 0.4\nNs 100\nd 1\n\n')
# Geometry preview uses the exported mesh itself, no generated concept image.
def render(name,eye):
    verts=np.array(V);target=np.array([0,.75,0]);eye=np.array(eye);forward=target-eye;forward/=np.linalg.norm(forward);right=np.cross(forward,[0,1,0]);right/=np.linalg.norm(right);up=np.cross(right,forward)
    rel=verts-target;xy=np.column_stack((rel@right,rel@up));depth=rel@forward
    W,H=1400,850;scale=225;xy=xy*scale+[W/2,H*.52];xy[:,1]=H-xy[:,1]
    pixels=np.full((H,W,3),(224,229,233),dtype=np.uint8);zb=np.full((H,W),np.inf)
    light=np.array([-.3,1,.6]);light/=np.linalg.norm(light)
    for a,b,c,m in sorted(F,key=lambda f:float(depth[list(f[:3])].mean()),reverse=True):
        ps=verts[[a,b,c]];n=np.cross(ps[1]-ps[0],ps[2]-ps[0]);l=np.linalg.norm(n)
        if l<1e-9:continue
        shade=.58+.42*abs(float(n@light/l));color=tuple(int(255*min(1,v*shade)) for v in COL[m])
        tri=xy[[a,b,c]];xmin,ymin=np.maximum(np.floor(tri.min(axis=0)).astype(int),[0,0]);xmax,ymax=np.minimum(np.ceil(tri.max(axis=0)).astype(int),[W-1,H-1])
        if xmax<xmin or ymax<ymin:continue
        xx,yy=np.meshgrid(np.arange(xmin,xmax+1)+.5,np.arange(ymin,ymax+1)+.5)
        (x0,y0),(x1,y1),(x2,y2)=tri;den=(y1-y2)*(x0-x2)+(x2-x1)*(y0-y2)
        if abs(den)<1e-8:continue
        u=((y1-y2)*(xx-x2)+(x2-x1)*(yy-y2))/den;v=((y2-y0)*(xx-x2)+(x0-x2)*(yy-y2))/den;w=1-u-v
        d=u*depth[a]+v*depth[b]+w*depth[c];sub=zb[ymin:ymax+1,xmin:xmax+1];mask=(u>=0)&(v>=0)&(w>=0)&(d<sub)
        sub[mask]=d[mask];pixels[ymin:ymax+1,xmin:xmax+1][mask]=color
    Image.fromarray(pixels).save(ROOT/'output/car'/name)
render('front.png',(6,3,8));render('rear.png',(-6,3,-8));render('side.png',(9,1.8,0))
print(json.dumps({'vertices':len(V),'triangles':len(F),'bounds':np.ptp(np.array(V),axis=0).tolist()}))
