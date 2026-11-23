import ctypes
from pathlib import Path
import pypdfium2 as pdf
import pypdfium2.raw as raw
from pypdf import PdfReader, PdfWriter
from reportlab.pdfgen import canvas
from reportlab.pdfbase import pdfmetrics
from reportlab.pdfbase.ttfonts import TTFont

root=Path('tmp/pdfs')
src='output/pdf/3D-printer.pdf'
p=pdf.PdfDocument(src)
for i,page in enumerate(p):
    removed=[]
    for obj in list(page.get_objects()):
        v=[ctypes.c_float() for _ in range(4)]
        raw.FPDFPageObj_GetBounds(obj,*[ctypes.byref(k) for k in v])
        l,b,r,t=[k.value for k in v]
        footer=obj.type==1 and l>890 and t<40
        houses=i==4 and ((obj.type==1 and l<500 and b>140 and t<210) or obj.type==3)
        if footer or houses:
            page.remove_obj(obj); removed.append(obj)
    page.gen_content()
    for obj in removed: obj.close()
p.save(root/'houses-base.pdf');p.close()
pdfmetrics.registerFont(TTFont('Arial','C:/Windows/Fonts/arial.ttf'))
pdfmetrics.registerFont(TTFont('ArialBold','C:/Windows/Fonts/arialbd.ttf'))
c=canvas.Canvas(str(root/'houses-new.pdf'),pagesize=(960,540))
c.setFillColorRGB(1,253/255,247/255);c.rect(0,0,960,540,stroke=0,fill=1)
c.setFillColorRGB(40/255,122/255,140/255);c.rect(0,531,960,9,stroke=0,fill=1)
c.setFillColorRGB(21/255,87/255,108/255);c.setFont('ArialBold',36.75)
c.drawString(48,462.75,'Дома из 3D-принтера')
def lines(texts,y,bold=False):
    c.setFont('ArialBold' if bold else 'Arial',21.75)
    c.setFillColorRGB(*( (22/255,119/255,139/255) if bold else (23/255,54/255,75/255)))
    for text in texts:
        assert pdfmetrics.stringWidth(text,'ArialBold' if bold else 'Arial',21.75)<430
        c.drawString(48,y,text); y-=28.5
lines(['Большие принтеры делают стены','домов из цементной смеси.'],365)
lines(['В нашей стране такие дома уже','построены в Сочи: в сёлах','Вардане-Верино, Молдовка','и Галицыно (Адлер).'],283)
lines(['В мире в напечатанных домах','уже живут люди!','Это уже реальность!'],145,True)
c.drawImage(str(root/'house-photo.png'),506,180,width=406,height=228.7)
c.save()
w=PdfWriter();base=PdfReader(root/'houses-base.pdf');new=PdfReader(root/'houses-new.pdf')
for i,page in enumerate(base.pages):
    if i==4:w.add_page(new.pages[0])
    w.add_page(page)
c=canvas.Canvas(str(root/'house-page-numbers.pdf'),pagesize=(960,540))
for i in range(9):
    c.setFont('Arial',13.5);c.setFillColorRGB(86/255,113/255,124/255)
    c.drawRightString(933,18.75,f'{i+1} / 9');c.showPage()
c.save()
numbers=PdfReader(root/'house-page-numbers.pdf')
for i,page in enumerate(w.pages):page.merge_page(numbers.pages[i])
w.write(root/'houses-final.pdf')
r=pdf.PdfDocument(root/'houses-final.pdf')
for i in (3,4,5):r[i].render().to_pil().save(root/f'houses-check-{i+1}.png')
r.close()
