from PIL import Image, ImageDraw, ImageFilter
import os

W, H = 110, 110
bg_color = (5, 5, 5)
lit_color = (255, 60, 60)
unlit_color = (35, 10, 10)
glow_color = (255, 0, 0)

# Segment coordinates (hexagons)
margin_x = 25
margin_y = 15
thickness = 12
gap = 2

# Helper to define horizontal segment
def h_seg(x, y, w):
    return [
        (x + gap, y),
        (x + gap + thickness/2, y - thickness/2),
        (x + w - gap - thickness/2, y - thickness/2),
        (x + w - gap, y),
        (x + w - gap - thickness/2, y + thickness/2),
        (x + gap + thickness/2, y + thickness/2)
    ]

# Helper to define vertical segment
def v_seg(x, y, h):
    return [
        (x, y + gap),
        (x + thickness/2, y + gap + thickness/2),
        (x + thickness/2, y + h - gap - thickness/2),
        (x, y + h - gap),
        (x - thickness/2, y + h - gap - thickness/2),
        (x - thickness/2, y + gap + thickness/2)
    ]

w = W - 2 * margin_x
h = (H - 2 * margin_y) / 2

segs = {
    'A': h_seg(margin_x, margin_y, w),
    'B': v_seg(margin_x + w, margin_y, h),
    'C': v_seg(margin_x + w, margin_y + h, h),
    'D': h_seg(margin_x, margin_y + h * 2, w),
    'E': v_seg(margin_x, margin_y + h, h),
    'F': v_seg(margin_x, margin_y, h),
    'G': h_seg(margin_x, margin_y + h, w)
}

digits = {
    '0': 'ABCDEF', '1': 'BC', '2': 'ABGED', '3': 'ABGCD',
    '4': 'FGBC', '5': 'AFGCD', '6': 'AFEDCG', '7': 'ABC',
    '8': 'ABCDEFG', '9': 'ABCDFG', 'Blank': ''
}

out_dir = r'c:\Users\nakam\UnityProject\Sudoku\Assets\Textures\LED7Seg'
os.makedirs(out_dir, exist_ok=True)

for digit, active_segs in digits.items():
    img = Image.new('RGB', (W, H), bg_color)
    draw = ImageDraw.Draw(img)
    
    # Draw unlit
    for name, poly in segs.items():
        if name not in active_segs:
            draw.polygon(poly, fill=unlit_color)
            
    # Draw lit with glow
    glow_img = Image.new('RGB', (W, H), (0,0,0))
    glow_draw = ImageDraw.Draw(glow_img)
    for name, poly in segs.items():
        if name in active_segs:
            glow_draw.polygon(poly, fill=glow_color)
            
    glow_img = glow_img.filter(ImageFilter.GaussianBlur(radius=4))
    
    # Composite
    img = Image.blend(img, glow_img, alpha=0.7)
    draw = ImageDraw.Draw(img)
    for name, poly in segs.items():
        if name in active_segs:
            draw.polygon(poly, fill=lit_color)
            
    img.save(os.path.join(out_dir, f'{digit}.png'))
    print(f'Generated {digit}.png')
