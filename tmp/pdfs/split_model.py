import ctypes
import pypdfium2 as pdf
import pypdfium2.raw as raw
from pypdf import PdfReader,PdfWriter
from reportlab.pdfgen import canvas
from reportlab.pdfbase import pdfmetrics
from reportlab.pdfbase.ttfonts import TTFont

p=pdf.PdfDocument('output/pdf/3D-printer.pdf')
for i,page in enumerate(p):
    removed=[]
    for o in list(page.get_objects()):
        if o.type!=1:continue
        v=[ctypes.c_float() for _ in range(4)]
        raw.FPDFPageObj_GetBounds(o,*[ctypes.byref(k) for k in v])
        l,b,r,t=[k.value for k in v]
        if (l>890 and t<40) or (i==2 and l<500 and b>275 and t<390):
            page.remove_obj(o);removed.append(o)
    page.gen_content()
    for o in removed:o.close()
p.save('tmp/pdfs/model-base.pdf');p.close()
pdfmetrics.registerFont(TTFont('ModelArial','C:/Windows/Fonts/arial.ttf'))
pdfmetrics.registerFont(TTFont('ModelBold','C:/Windows/Fonts/arialbd.ttf'))
c=canvas.Canvas('tmp/pdfs/model-new.pdf',pagesize=(960,540))
c.setFillColorRGB(1,253/255,247/255);c.rect(0,0,960,540,fill=1,stroke=0)
c.setFillColorRGB(40/255,122/255,140/255);c.rect(0,531,960,9,fill=1,stroke=0)
c.setFont('ModelBold',36.75);c.setFillColorRGB(21/255,87/255,108/255)
c.drawString(48,462.75,'Сначала создают модель')
c.setFont('ModelArial',21.75);c.setFillColorRGB(23/255,54/255,75/255)
for text,y in [('Сначала на компьютере создают',320),('модель предмета.',291.5),('Это образец для принтера.',235)]:
    c.drawString(48,y,text)
c.drawImage('tmp/pdfs/model-elephant.png',506,55,width=406,height=406)
c.save()
w=PdfWriter();r=PdfReader('tmp/pdfs/model-base.pdf')
for i,page in enumerate(r.pages):
    if i==2:w.add_page(PdfReader('tmp/pdfs/model-new.pdf').pages[0])
    w.add_page(page)
c=canvas.Canvas('tmp/pdfs/model-numbers.pdf',pagesize=(960,540))
for i in range(9):
    c.setFont('ModelArial',13.5);c.setFillColorRGB(86/255,113/255,124/255)
    c.drawRightString(933,18.75,f'{i+1} / 9');c.showPage()
c.save();r=PdfReader('tmp/pdfs/model-numbers.pdf')
for i,page in enumerate(w.pages):page.merge_page(r.pages[i])
w.write('tmp/pdfs/model-final.pdf')
r=pdf.PdfDocument('tmp/pdfs/model-final.pdf')
for i in (2,3):r[i].render().to_pil().save(f'tmp/pdfs/model-check-{i}.png')
r.close()
