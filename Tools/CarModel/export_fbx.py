"""Export the split coupe OBJ as binary FBX 7.4, with geometry and solid materials."""
from pathlib import Path
import struct
from collections import OrderedDict

ROOT=Path(__file__).resolve().parents[2]
SOURCE=ROOT/'Assets/LevelDesign/Models/Vehicles/Cars/GrandTouringCoupe'
OUT=ROOT/'output/car/GrandTouringCoupe.fbx'
def prop(v):
    if isinstance(v,bytes):return b'S'+struct.pack('<I',len(v))+v
    if isinstance(v,str):return prop(v.encode())
    if isinstance(v,float):return b'D'+struct.pack('<d',v)
    if isinstance(v,int):return b'L'+struct.pack('<q',v)
    kind,values=v
    raw=struct.pack('<'+{'d':'d','i':'i'}[kind]*len(values),*values)
    return kind.encode()+struct.pack('<III',len(values),0,len(raw))+raw
def N(name,*values,children=()):return (name,values,list(children))
def encode(node,offset):
    name,values,children=node;name=name.encode();data=b''.join(map(prop,values))
    cursor=offset+13+len(name)+len(data);tail=b''
    for child in children:
        encoded=encode(child,cursor);tail+=encoded;cursor+=len(encoded)
    if children:tail+=b'\0'*13;cursor+=13
    return struct.pack('<III',cursor,len(values),len(data))+bytes([len(name)])+name+data+tail
def P(name,kind,*values):return N('P',name,kind,'','A',*values)
verts=[];normals=[];groups=OrderedDict();group='Body';material='Paint'
for line in (SOURCE/'GrandTouringCoupe.obj').read_text().splitlines():
    a=line.split()
    if not a:continue
    if a[0]=='v':verts.append(tuple(map(float,a[1:])))
    elif a[0]=='vn':normals.append(tuple(map(float,a[1:])))
    elif a[0]=='g':group=a[1]
    elif a[0]=='usemtl':material=a[1]
    elif a[0]=='f':groups.setdefault(group,[]).append((material,[tuple(int(x)-1 for x in token.split('//')) for token in a[1:]]))
colors={};name=''
for line in (SOURCE/'GrandTouringCoupe.mtl').read_text().splitlines():
    a=line.split()
    if not a:continue
    if a[0]=='newmtl':name=a[1]
    elif a[0]=='Kd':colors[name]=tuple(map(float,a[1:]))
objects=[];connections=[];material_ids={name:2000+i for i,name in enumerate(colors)}
for name,color in colors.items():
    objects.append(N('Material',material_ids[name],name.encode()+b'\0\1Material','',children=[
        N('Version',102),N('ShadingModel','lambert'),N('MultiLayer',0),
        N('Properties70',children=[P('DiffuseColor','Color',*color),P('DiffuseFactor','Number',1.),
            P('SpecularFactor','Number',0.),P('TransparencyFactor','Number',.68 if name=='Glass' else 0.)])]))
root_id=1000
def model(ident,name):
    return N('Model',ident,name.encode()+b'\0\1Model','Null' if ident==root_id else 'Mesh',children=[
        N('Version',232),N('Properties70',children=[P('Lcl Translation','Lcl Translation',0.,0.,0.),
          P('Lcl Rotation','Lcl Rotation',0.,0.,0.),P('Lcl Scaling','Lcl Scaling',1.,1.,1.)]),N('Shading',1),N('Culling','CullingOff')])
objects.append(model(root_id,'GrandTouringCoupe'));connections.append(N('C','OO',root_id,0))
for i,(name,faces) in enumerate(groups.items()):
    mid=3000+i;gid=4000+i;local={};points=[];indices=[];ns=[];mats=[];used=list(dict.fromkeys(m for m,f in faces))
    for mat,face in faces:
        for j,(vi,ni) in enumerate(face):
            if vi not in local:local[vi]=len(points)//3;points.extend(verts[vi])
            index=local[vi];indices.append(-index-1 if j==len(face)-1 else index);ns.extend(normals[ni])
        mats.append(used.index(mat))
    objects.append(N('Geometry',gid,name.encode()+b'\0\1Geometry','Mesh',children=[N('GeometryVersion',124),
        N('Vertices',('d',points)),N('PolygonVertexIndex',('i',indices)),
        N('LayerElementNormal',0,children=[N('Version',101),N('Name',''),N('MappingInformationType','ByPolygonVertex'),N('ReferenceInformationType','Direct'),N('Normals',('d',ns))]),
        N('LayerElementMaterial',0,children=[N('Version',101),N('Name',''),N('MappingInformationType','ByPolygon'),N('ReferenceInformationType','IndexToDirect'),N('Materials',('i',mats))]),
        N('Layer',0,children=[N('Version',100),N('LayerElement',children=[N('Type','LayerElementNormal'),N('TypedIndex',0)]),N('LayerElement',children=[N('Type','LayerElementMaterial'),N('TypedIndex',0)])])]))
    objects.append(model(mid,name));connections.extend([N('C','OO',gid,mid),N('C','OO',mid,root_id)])
    connections.extend(N('C','OO',material_ids[m],mid) for m in used)
nodes=[N('FBXHeaderExtension',children=[N('FBXHeaderVersion',1003),N('FBXVersion',7400),N('EncryptionType',0),N('Creator','Memories mesh exporter')]),
    N('GlobalSettings',children=[N('Version',1000),N('Properties70',children=[P('UpAxis','int',1),P('UpAxisSign','int',1),P('FrontAxis','int',2),P('FrontAxisSign','int',-1),P('CoordAxis','int',0),P('CoordAxisSign','int',1),P('UnitScaleFactor','double',100.),P('OriginalUnitScaleFactor','double',100.)])]),
    N('Documents',children=[N('Count',1),N('Document',999,'Scene','Scene',children=[N('Properties70'),N('RootNode',0)])]),
    N('References'),N('Definitions',children=[N('Version',100),N('Count',len(objects)),*[N('ObjectType',kind,children=[N('Count',count)]) for kind,count in [('Model',len(groups)+1),('Geometry',len(groups)),('Material',len(colors))]]]),
    N('Objects',children=objects),N('Connections',children=connections),N('Takes',children=[N('Current','')])]
data=b'Kaydara FBX Binary  \0\x1a\0'+struct.pack('<I',7400)
for node in nodes:data+=encode(node,len(data))
data+=b'\0'*13
data+=bytes.fromhex('fabcab09d0c8d466b176fb831cf7267e')
data+=b'\0'*((-len(data))%16)+struct.pack('<I',7400)+b'\0'*120+bytes.fromhex('f85a8c6adef5d97eece90ce3758f290b')
OUT.write_bytes(data)
# Validate nested node offsets, property lengths and terminators from the binary output.
def check(offset):
    end,count,length=struct.unpack_from('<III',data,offset);n=data[offset+12]
    assert end>offset and end<=len(data)
    cursor=offset+13+n+length
    while cursor<end:
        if data[cursor:cursor+13]==b'\0'*13:cursor+=13;break
        cursor=check(cursor)
    assert cursor==end
    return end
cursor=27
while data[cursor:cursor+13]!=b'\0'*13:cursor=check(cursor)
print(OUT, len(groups),'parts;',sum(len(f) for f in groups.values()),'triangles;',len(data),'bytes; binary structure verified')

