"""Independent PDF checks/rendering with PyMuPDF; development tool only."""
import json
import pathlib
import sys
import unicodedata
import pymupdf

root = pathlib.Path(sys.argv[1]) if len(sys.argv) > 1 else pathlib.Path(__file__).resolve().parent.parent / "app/verificacao/referencias-pdf"
reports = []
for name in ("unicode.pdf", "selected.pdf", "pagination.pdf"):
    document = pymupdf.open(root / name)
    text = "".join(page.get_text() for page in document)
    normalized = unicodedata.normalize("NFC", text)
    assert document.page_count > 0
    if name == "unicode.pdf":
        assert "Projeto elétrico Ω 北 😀" in normalized, repr(normalized)
        assert "Descrição: árvore, tolerância ≤ 5%, corrente 12,345 A." in normalized, repr(normalized)
    if name == "selected.pdf":
        assert "C1 — Iluminação" in normalized
        assert "C2 — Tomadas" not in normalized
        assert "1 de 2 circuitos selecionados" in normalized
        assert "P=3,600 kW" in normalized
    if name == "pagination.pdf":
        assert document.page_count > 3
    checked_spans = 0
    embedded_fonts = set()
    for index, page in enumerate(document):
        assert f"OpenEletric - {index + 1}" in page.get_text()
        for block in page.get_text("dict")["blocks"]:
            for line in block.get("lines", []):
                for span in line["spans"]:
                    x0, y0, x1, y1 = span["bbox"]
                    assert x0 >= 34 and x1 <= page.rect.width - 34, (name, index, span)
                    assert y0 >= 34 and y1 <= page.rect.height - 18, (name, index, span)
                    checked_spans += 1
        for font in page.get_fonts(full=True):
            if font[0]:
                data = document.extract_font(font[0])
                assert len(data[3]) > 0, (name, "font not embedded", font)
                embedded_fonts.add(font[0])
    for index in sorted({0, document.page_count - 1}):
        document[index].get_pixmap(matrix=pymupdf.Matrix(1.5, 1.5)).save(root / f"{name}.page-{index + 1}.png")
    (root / f"{name}.txt").write_text(text, encoding="utf-8")
    reports.append({"file": name, "pages": document.page_count, "text_chars": len(text), "checked_spans": checked_spans, "embedded_fonts": len(embedded_fonts), "result": "PASS"})

(root / "independent-reader.json").write_text(json.dumps({"engine": "PyMuPDF " + pymupdf.__version__, "cases": reports}, indent=2) + "\n", encoding="utf-8")
print(json.dumps(reports, indent=2))
