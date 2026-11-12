"""Draw the small UI sprites by hand on integer pixel grids (requires Pillow)."""
from pathlib import Path
from PIL import Image, ImageDraw

ROOT = Path(__file__).resolve().parents[1] / "Assets/Art/PixelArt"
DARK = (48, 34, 31, 255)
PANEL = (69, 46, 39, 250)
GOLD = (205, 153, 95, 255)
LIGHT = (244, 213, 159, 255)
SHADE = (103, 65, 47, 255)


def panel(path, width, height):
    im = Image.new("RGBA", (width, height), (0, 0, 0, 0))
    d = ImageDraw.Draw(im)
    d.rectangle((1, 2, width - 2, height - 1), fill=DARK)
    d.rectangle((3, 0, width - 4, height - 4), fill=GOLD)
    d.rectangle((5, 2, width - 6, height - 6), fill=PANEL)
    d.line((7, 4, width - 8, 4), fill=SHADE, width=1)
    d.line((7, height - 8, width - 8, height - 8), fill=SHADE, width=1)
    for x in (7, width - 9):
        for y in (6, height - 10):
            d.rectangle((x, y, x + 1, y + 1), fill=LIGHT)
    # Tiny corner stitches tie the frame to the apartment's textiles.
    for x in (13, width - 18):
        d.line((x, 3, x + 3, 3), fill=LIGHT, width=1)
    im.save(path)


panel(ROOT / "HudCard.png", 144, 64)
panel(ROOT / "FinishCard.png", 192, 72)

marker = Image.new("RGBA", (32, 32), (0, 0, 0, 0))
d = ImageDraw.Draw(marker)
ink = (122, 77, 49, 205)
soft = (217, 161, 99, 190)
d.ellipse((8, 15, 24, 27), fill=ink)
d.ellipse((11, 18, 21, 24), fill=soft)
for box in ((5, 9, 10, 15), (11, 5, 16, 12), (18, 6, 23, 13), (24, 11, 29, 17)):
    d.ellipse(box, fill=ink)
    d.point(((box[0] + box[2]) // 2, (box[1] + box[3]) // 2), fill=soft)
marker.save(ROOT / "TrapZoneMarker.png")

chip = Image.new("RGBA", (88, 20), (0, 0, 0, 0))
d = ImageDraw.Draw(chip)
d.rectangle((2, 3, 85, 18), fill=(57, 40, 35, 220))
d.rectangle((0, 0, 87, 15), fill=SHADE)
d.rectangle((2, 2, 85, 13), fill=LIGHT)
d.rectangle((4, 4, 83, 11), fill=(239, 201, 142, 255))
d.rectangle((7, 6, 9, 8), fill=GOLD)
d.rectangle((78, 6, 80, 8), fill=GOLD)
chip.save(ROOT / "TrapPromptChip.png")
