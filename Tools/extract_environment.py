"""Deterministic cutouts from the approved AssetSets sheets.

Run with Pillow, NumPy and OpenCV installed. Coordinates are source pixels.
Only alpha masking, cropping and nearest-neighbour reduction are performed.
"""
from pathlib import Path
import cv2
import numpy as np
from PIL import Image

ROOT = Path(__file__).resolve().parents[1]
SHEETS = ROOT / "ConceptArt" / "AssetSets"
OUT = ROOT / "Assets" / "Art" / "PixelArt" / "Environment"

# sheet, category, object, source bounding box, longest exported edge in pixels
ITEMS = [
    ("Bedroom_Furniture_Set_01.png", "Bedroom", "Bed", (65, 75, 465, 925), 88),
    ("Bedroom_Furniture_Set_01.png", "Bedroom", "Wardrobe", (505, 58, 1025, 570), 72),
    ("Bedroom_Furniture_Set_01.png", "Bedroom", "BedsideTableLamp", (1120, 110, 1415, 470), 44),
    ("Bedroom_Furniture_Set_01.png", "Bedroom", "LowBookShelf", (500, 615, 1045, 950), 76),
    ("Bedroom_Furniture_Set_01.png", "Bedroom", "BedroomRug", (1105, 495, 1480, 945), 70),
    ("LivingRoom_Furniture.png", "LivingRoom", "Sofa", (45, 155, 600, 500), 82),
    ("LivingRoom_Furniture.png", "LivingRoom", "CoffeeTable", (600, 250, 950, 500), 58),
    ("LivingRoom_Furniture.png", "LivingRoom", "TVConsole", (960, 195, 1515, 490), 82),
    ("LivingRoom_Furniture.png", "LivingRoom", "Sideboard", (30, 545, 600, 855), 82),
    ("LivingRoom_Furniture.png", "LivingRoom", "Armchair", (605, 550, 865, 865), 50),
    ("LivingRoom_Furniture.png", "LivingRoom", "LivingRoomRug", (860, 515, 1510, 930), 112),
    ("Kitchen_Assets.png", "Kitchen", "Refrigerator", (25, 35, 300, 545), 66),
    ("Kitchen_Assets.png", "Kitchen", "StoveOven", (300, 145, 555, 525), 58),
    ("Kitchen_Assets.png", "Kitchen", "SinkCabinet", (570, 120, 955, 535), 65),
    ("Kitchen_Assets.png", "Kitchen", "CounterModule", (960, 70, 1510, 530), 88),
    ("Kitchen_Assets.png", "Kitchen", "DiningTableChairs", (15, 545, 625, 935), 96),
    ("Kitchen_Assets.png", "Kitchen", "KitchenRug", (635, 600, 980, 885), 56),
    ("Kitchen_Assets.png", "Kitchen", "CoffeeCounter", (990, 540, 1515, 940), 76),
    ("Bathroom_Assets.png", "Bathroom", "Bathtub", (20, 45, 335, 590), 78),
    ("Bathroom_Assets.png", "Bathroom", "SinkCabinet", (345, 120, 710, 570), 67),
    ("Bathroom_Assets.png", "Bathroom", "Toilet", (740, 175, 985, 565), 55),
    ("Bathroom_Assets.png", "Bathroom", "NarrowCabinet", (995, 45, 1195, 560), 70),
    ("Bathroom_Assets.png", "Bathroom", "Shower", (1195, 45, 1520, 570), 76),
    ("Bathroom_Assets.png", "Bathroom", "BathroomRug", (15, 595, 470, 920), 72),
    ("Bathroom_Assets.png", "Bathroom", "LaundryBasket", (500, 575, 700, 815), 42),
    ("Bathroom_Assets.png", "Bathroom", "AccessoryShelf", (1020, 575, 1370, 825), 56),
    ("Decor_Assets.png", "Decor", "MonsteraPlant", (15, 15, 210, 275), 48),
    ("Decor_Assets.png", "Decor", "TrailingPlant", (200, 20, 400, 300), 48),
    ("Decor_Assets.png", "Decor", "TableLamp", (20, 250, 165, 435), 35),
    ("Decor_Assets.png", "Decor", "Books", (1300, 290, 1510, 425), 40),
    ("Decor_Assets.png", "Decor", "FramedLandscape", (690, 270, 840, 425), 32),
    ("Decor_Assets.png", "Decor", "GreenSlippers", (180, 730, 350, 850), 32),
    ("Decor_Assets.png", "Decor", "SleepingCat", (500, 715, 755, 885), 54),
    ("Gameplay_Assets.png", "Gameplay", "SockBasketLow", (30, 95, 365, 380), 60),
    ("Gameplay_Assets.png", "Gameplay", "SockBasketMedium", (370, 95, 700, 380), 60),
    ("Gameplay_Assets.png", "Gameplay", "SockBasketFull", (690, 60, 1120, 380), 66),
    ("Gameplay_Assets.png", "Gameplay", "SockCreamStripe", (25, 405, 140, 565), 32),
    ("Gameplay_Assets.png", "Gameplay", "SockGreen", (135, 405, 240, 565), 32),
    ("Gameplay_Assets.png", "Gameplay", "SockLeaf", (245, 400, 365, 565), 32),
    ("Gameplay_Assets.png", "Gameplay", "SockOrangeDots", (365, 400, 470, 565), 32),
    ("Gameplay_Assets.png", "Gameplay", "SockGreenCheck", (570, 400, 685, 565), 32),
    ("Gameplay_Assets.png", "Gameplay", "SockHeart", (675, 400, 785, 565), 32),
    ("Gameplay_Assets.png", "Gameplay", "SlowTrapArmed", (55, 775, 370, 1015), 56),
    ("Gameplay_Assets.png", "Gameplay", "SlowTrapTriggered", (525, 775, 865, 1015), 56),
    ("Gameplay_Assets.png", "Gameplay", "SlowTrapUsed", (900, 765, 1210, 1015), 56),
]


def extract(sheet: Image.Image, bounds, max_edge):
    roi = np.asarray(sheet.crop(bounds).convert("RGB"))
    h, w = roi.shape[:2]
    bgr = cv2.cvtColor(roi, cv2.COLOR_RGB2BGR)
    mask = np.zeros((h, w), np.uint8)
    inset = max(4, min(w, h) // 45)
    cv2.grabCut(bgr, mask, (inset, inset, w - 2 * inset, h - 2 * inset), None, None, 8, cv2.GC_INIT_WITH_RECT)
    alpha = np.where((mask == cv2.GC_FGD) | (mask == cv2.GC_PR_FGD), 255, 0).astype(np.uint8)
    # Keep the object's principal silhouette, dropping stray background islands.
    count, labels, stats, _ = cv2.connectedComponentsWithStats(alpha)
    if count > 1:
        largest = 1 + np.argmax(stats[1:, cv2.CC_STAT_AREA])
        alpha = np.where(labels == largest, 255, 0).astype(np.uint8)
    image = Image.fromarray(np.dstack((roi, alpha)))
    bbox = image.getbbox()
    if bbox is None:
        raise RuntimeError(f"Empty cutout: {bounds}")
    image = image.crop(bbox)
    if bounds in ((1105, 495, 1480, 945), (45, 155, 600, 500)):
        image = image.transpose(Image.Transpose.ROTATE_90)
    scale = min(1.0, max_edge / max(image.size))
    if scale < 1:
        image = image.resize((max(1, round(image.width * scale)), max(1, round(image.height * scale))), Image.Resampling.NEAREST)
    return image


def main():
    sheets = {}
    for sheet_name, category, name, bounds, max_edge in ITEMS:
        if sheet_name not in sheets:
            sheets[sheet_name] = Image.open(SHEETS / sheet_name).convert("RGB")
        image = extract(sheets[sheet_name], bounds, max_edge)
        folder = OUT / category
        folder.mkdir(parents=True, exist_ok=True)
        image.save(folder / (name + ".png"), optimize=True)
        print(category, name, image.size)


if __name__ == "__main__":
    main()
