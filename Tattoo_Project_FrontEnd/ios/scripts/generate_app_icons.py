from pathlib import Path

from PIL import Image


ROOT = Path(__file__).resolve().parents[1]
PUBLIC = ROOT / "public"
MASTER_ICON = PUBLIC / "inkroute-app-icon.png"
BACKGROUND = "#09090d"
RESAMPLING = Image.Resampling.LANCZOS


def load_master_icon() -> Image.Image:
    with Image.open(MASTER_ICON) as source:
        icon = source.convert("RGB")

    side = min(icon.size)
    left = (icon.width - side) // 2
    top = (icon.height - side) // 2
    return icon.crop((left, top, left + side, top + side))


MASTER = load_master_icon()


def create_icon(size: int, safe_padding: bool = False) -> Image.Image:
    if not safe_padding:
        return MASTER.resize((size, size), RESAMPLING)

    canvas = Image.new("RGB", (size, size), BACKGROUND)
    artwork_size = round(size * 0.80)
    artwork = MASTER.resize((artwork_size, artwork_size), RESAMPLING)
    offset = (size - artwork_size) // 2
    canvas.paste(artwork, (offset, offset))
    return canvas


def save_icon(image: Image.Image, path: Path) -> None:
    path.parent.mkdir(parents=True, exist_ok=True)
    image.save(path, optimize=True)


save_icon(create_icon(32), PUBLIC / "favicon-32x32.png")
save_icon(create_icon(48), PUBLIC / "favicon-48x48.png")
save_icon(create_icon(180), PUBLIC / "apple-touch-icon.png")
save_icon(create_icon(192), PUBLIC / "pwa-192x192.png")
save_icon(create_icon(512), PUBLIC / "pwa-512x512.png")
save_icon(
    create_icon(512, safe_padding=True),
    PUBLIC / "pwa-maskable-512x512.png",
)


def create_splash(width: int, height: int) -> Image.Image:
    splash = Image.new("RGB", (width, height), BACKGROUND)
    logo_size = min(width, height) * 34 // 100
    logo = create_icon(logo_size)
    splash.paste(logo, ((width - logo_size) // 2, (height - logo_size) // 2))
    return splash


android_res = ROOT / "android" / "app" / "src" / "main" / "res"
if android_res.exists():
    launcher_sizes = {
        "mipmap-mdpi": 48,
        "mipmap-hdpi": 72,
        "mipmap-xhdpi": 96,
        "mipmap-xxhdpi": 144,
        "mipmap-xxxhdpi": 192,
    }
    foreground_sizes = {
        "mipmap-mdpi": 108,
        "mipmap-hdpi": 162,
        "mipmap-xhdpi": 216,
        "mipmap-xxhdpi": 324,
        "mipmap-xxxhdpi": 432,
    }

    for folder, size in launcher_sizes.items():
        destination = android_res / folder
        save_icon(create_icon(size), destination / "ic_launcher.png")
        save_icon(create_icon(size), destination / "ic_launcher_round.png")

    for folder, size in foreground_sizes.items():
        save_icon(
            create_icon(size, safe_padding=True),
            android_res / folder / "ic_launcher_foreground.png",
        )

    for splash_path in android_res.glob("drawable*/splash.png"):
        with Image.open(splash_path) as current_splash:
            splash_size = current_splash.size
        save_icon(create_splash(*splash_size), splash_path)


ios_assets = ROOT / "ios" / "App" / "App" / "Assets.xcassets"
if ios_assets.exists():
    save_icon(
        create_icon(1024),
        ios_assets / "AppIcon.appiconset" / "AppIcon-512@2x.png",
    )

    for splash_path in (ios_assets / "Splash.imageset").glob("*.png"):
        with Image.open(splash_path) as current_splash:
            splash_size = current_splash.size
        save_icon(create_splash(*splash_size), splash_path)
