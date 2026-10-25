from pathlib import Path
from PIL import Image
import random,struct
output=Path(__file__).with_name("GifFixtures")
output.mkdir(exist_ok=True)
palette=[n for i in range(256) for n in ((i*37)%256,(i*97)%256,(i*13)%256)]
def indexed(w,h,values):
    im=Image.new("P",(w,h));im.putpalette(palette);im.putdata(values);return im
rng=random.Random(29727)
# Variable-width LZW dictionaries, reset at 4096 codes, and interlaced passes.
random_image=indexed(257,129,[rng.randrange(256) for _ in range(257*129)])
random_image.save(output/"interlaced-dictionary.gif",interlace=True,optimize=False)
indexed(128,96,[3]*(128*96)).save(output/"lzw-repeat.gif",optimize=False)
# Cropped frame rectangles, local color tables and transparency with disposal 2/3.
frames=[]
for i in range(4):
    pixels=[0]*(24*24)
    for y in range(4+i*2,12+i*2):
        for x in range(3+i*3,10+i*3):pixels[y*24+x]=i+1
    frames.append(indexed(24,24,pixels))
for disposal in ([1,2,3,1],[1,1,1,1]):
    frames[0].save(output/("disposal-"+str(disposal[1])+".gif"),save_all=True,append_images=frames[1:],duration=[80,120,40,160],transparency=0,loop=0,disposal=disposal,optimize=False)
# Different palette in each frame, intentionally opaque.
local=[]
for i in range(3):
    im=indexed(19,17,[(x+y+i)%7 for y in range(17) for x in range(19)])
    im.putpalette([n for j in range(256) for n in ((j*53+i*40)%256,(j*17+i*60)%256,(j*71+i*80)%256)])
    local.append(im)
local[0].save(output/"local-palette.gif",save_all=True,append_images=local[1:],duration=[30,70,110],loop=0,optimize=False,disposal=1)
for path in output.glob("*.gif"):
    with Image.open(path) as im, path.with_suffix(".rgba").open("wb") as expected:
        expected.write(struct.pack("<iii",im.width,im.height,im.n_frames))
        for i in range(im.n_frames):
            im.seek(i);expected.write(struct.pack("<i",max(20,im.info.get("duration",100))));expected.write(im.convert("RGBA").tobytes())
    print(path.name, path.stat().st_size)
