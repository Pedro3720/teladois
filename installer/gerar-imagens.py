"""Gera o ícone do Windows e as imagens do instalador a partir de docs/design/icone-1024.png.

Uso (na raiz do repositório): py -3 installer/gerar-imagens.py
Os arquivos gerados são versionados; rode de novo só se o ícone mudar. Precisa do Pillow (py -3 -m pip install pillow).
"""
from pathlib import Path

from PIL import Image, ImageDraw

ROOT = Path(__file__).resolve().parent.parent
SOURCE = ROOT / "docs" / "design" / "icone-1024.png"
ICO = ROOT / "windows" / "src" / "TelaDois.Tray" / "TelaDois.ico"
IMAGES = ROOT / "installer" / "imagens"
BLACK = (0, 0, 0, 255)
ICO_SIZES = [16, 20, 24, 32, 40, 48, 64, 128, 256]


def artwork() -> Image.Image:
    """O desenho (monitor + iPhone) recortado do fundo preto, com 4% de folga."""
    img = Image.open(SOURCE).convert("RGBA")
    box = img.convert("L").point(lambda v: 255 if v > 40 else 0).getbbox()
    pad = round(img.width * 0.04)
    return img.crop((box[0] - pad, box[1] - pad, box[2] + pad, box[3] + pad))


def fit(art: Image.Image, width: int, height: int) -> Image.Image:
    ratio = min(width / art.width, height / art.height)
    return art.resize((max(1, round(art.width * ratio)), max(1, round(art.height * ratio))), Image.LANCZOS)


def tile(art: Image.Image, size: int) -> Image.Image:
    """Desenho centralizado num quadrado preto de cantos arredondados, fora dele transparente."""
    canvas = Image.new("RGBA", (size, size), (0, 0, 0, 0))
    mask = Image.new("L", (size, size), 0)
    ImageDraw.Draw(mask).rounded_rectangle((0, 0, size - 1, size - 1), radius=round(size * 0.2), fill=255)
    canvas.paste(Image.new("RGBA", (size, size), BLACK), (0, 0), mask)
    # Nos tamanhos da bandeja (até 32 px) o desenho ocupa quase tudo para os traços não sumirem.
    inner = size - 2 * round(size * (0.04 if size <= 32 else 0.1))
    scaled = fit(art, inner, inner)
    canvas.alpha_composite(scaled, ((size - scaled.width) // 2, (size - scaled.height) // 2))
    return canvas


def wizard(art: Image.Image, width: int, height: int) -> Image.Image:
    """Faixa da esquerda do assistente: fundo preto e o desenho um pouco acima do meio."""
    canvas = Image.new("RGBA", (width, height), BLACK)
    scaled = fit(art, round(width * 0.76), height)
    canvas.alpha_composite(scaled, ((width - scaled.width) // 2, round(height * 0.42) - scaled.height // 2))
    return canvas


def main() -> None:
    art = artwork()
    tiles = [tile(art, size) for size in ICO_SIZES]
    tiles[-1].save(ICO, format="ICO", sizes=[(s, s) for s in ICO_SIZES], append_images=tiles[:-1])
    IMAGES.mkdir(parents=True, exist_ok=True)
    for scale in (100, 150, 200):
        # Arredonda meio para cima (round() do Python arredonda 82,5 para 82; o Inno Setup espera 83).
        def scaled(base: int) -> int:
            return (base * scale + 50) // 100

        wizard(art, scaled(164), scaled(314)).save(IMAGES / f"assistente-{scale}.png")
        tile(art, scaled(55)).save(IMAGES / f"icone-{scale}.png")
    print(f"Gerados: {ICO.relative_to(ROOT)} e {len(list(IMAGES.glob('*.png')))} imagens em {IMAGES.relative_to(ROOT)}")


if __name__ == "__main__":
    main()
