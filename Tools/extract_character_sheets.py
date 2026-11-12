"""Extract aligned transparent frames from the seven approved character master sheets.
Requires Pillow, NumPy and OpenCV; run manually when the masters change.
"""
import os, cv2, numpy as np
from PIL import Image
root=os.path.abspath(os.path.join(os.path.dirname(__file__), '..')); out=root+'/Assets/Art/PixelArt/Characters'
sheets=[('Bulka','Gameplay','Bulka_Gameplay_Sprites.png',5,[0,300,575,795,1024]),('Bulka','Bark','Bulka_Bark.png',4,[0,300,590,800,1024]),('Bulka','Victory','Bulka_Victory.png',6,[0,295,570,785,1024]),('Bulka','Lose','Bulka_Lose.png',5,[0,310,580,790,1024]),('OrangeCat','Gameplay','OrangeCat_Gameplay_Sprites.png',5,[0,295,570,775,1024]),('OrangeCat','CarryingSock','OrangeCat_CarryingSock.png',5,[0,295,570,775,1024]),('OrangeCat','Fear','OrangeCat_Fear.png',5,[0,295,570,775,1024])]
for char,kind,file,cols,ys in sheets:
 im=Image.open(f'{root}/ConceptArt/Characters/{char}/{file}').convert('RGB');w,h=im.size
 os.makedirs(f'{out}/{char}',exist_ok=True)
 for row in range(4):
  for col in range(cols):
   xbounds=([0,445,750,1055,1536] if char=='Bulka' and kind=='Bark' else [0,300,560,800,1045,1295,1536] if char=='Bulka' and kind=='Victory' else [0,335,635,935,1235,1536] if char=='Bulka' and kind=='Lose' else [0,355,645,935,1230,1536] if char=='OrangeCat' else [round(i*w/cols) for i in range(cols+1)])
   x0=xbounds[col];x1=xbounds[col+1];y0=ys[row];y1=ys[row+1]
   if char=='OrangeCat' and row==2: y0=545
   rgb=np.array(im.crop((x0,y0,x1,y1))); gray=cv2.cvtColor(rgb,cv2.COLOR_RGB2GRAY)
   edge=cv2.Canny(gray,35,85)
   edge=cv2.morphologyEx(edge,cv2.MORPH_CLOSE,np.ones((5,5),np.uint8))
   contours,_=cv2.findContours(edge,cv2.RETR_EXTERNAL,cv2.CHAIN_APPROX_SIMPLE)
   contours=[c for c in contours if cv2.contourArea(c)>250]
   mask=np.zeros(gray.shape,np.uint8)
   # The largest outlined island is the character; the small islands are detached shadow/marks.
   if not contours: raise RuntimeError(f'No contour {char} {kind} {row} {col}')
   main=max(contours,key=cv2.contourArea)
   cv2.drawContours(mask,[main],-1,255,-1)
   mask=cv2.morphologyEx(mask,cv2.MORPH_CLOSE,np.ones((5,5),np.uint8))
   yy,xx=np.where(mask>0); left=max(0,int(xx.min())-2); right=min(mask.shape[1],int(xx.max())+3);top=max(0,int(yy.min())-2);bottom=min(mask.shape[0],int(yy.max())+3)
   rgba=Image.fromarray(np.dstack((rgb,mask)),'RGBA').crop((left,top,right,bottom))
   canvas=Image.new('RGBA',(320,320),(0,0,0,0))
   if rgba.width>320 or rgba.height>320:raise RuntimeError(f'Oversize {char} {kind} {row} {col}: {rgba.size}')
   canvas.alpha_composite(rgba,((320-rgba.width)//2,306-rgba.height))
   canvas.save(f'{out}/{char}/{char}_{kind}_R{row+1}_F{col+1:02}.png')
 print(char,kind,cols*4)
