"""Local IPS IWF ↔ XML, Python 3.10+; based on the supplied iwf_unpack.py."""

import argparse
import hashlib
import json
from pathlib import Path
import sys
from xml.parsers import expat
import zlib


WORKSPACE = Path(__file__).resolve().parents[3] / "output" / "business-processes"
MAX_XML_BYTES = 20 * 1024 * 1024
CHUNK_BYTES = 64 * 1024


class ConversionError(ValueError):
    """An unsupported, unsafe or incomplete conversion."""


def _check_limit(max_bytes: int) -> None:
    if not 0 < max_bytes <= MAX_XML_BYTES:
        raise ConversionError(f"Допустимый лимит XML: от 1 до {MAX_XML_BYTES} байт (20 МиБ).")


def validate_xml(data: bytes | bytearray) -> str:
    """Check well-formedness without a DOM, DTD, entities or resource loaders."""
    parser = expat.ParserCreate(namespace_separator="}")
    root = ""

    def start_element(name, attributes):
        nonlocal root
        if not root:
            root = "{" + name if "}" in name else name

    def reject_declaration(*args):
        raise ConversionError("Небезопасный XML: DTD и объявления сущностей запрещены.")

    parser.StartElementHandler = start_element
    parser.StartDoctypeDeclHandler = reject_declaration
    parser.EntityDeclHandler = reject_declaration
    parser.ExternalEntityRefHandler = reject_declaration
    parser.SetParamEntityParsing(expat.XML_PARAM_ENTITY_PARSING_NEVER)
    try:
        parser.Parse(data, True)
    except (expat.ExpatError, LookupError, ValueError) as error:
        raise ConversionError(f"Некорректный XML: {error}") from error
    return root


def _save(data: bytes | bytearray, source: Path, output, suffix: str) -> Path:
    target = Path(output) if output is not None else Path(source.stem + suffix)
    if not target.is_absolute():
        target = WORKSPACE / target
    target = target.resolve()
    if any(":" in part for part in target.parts[1:]):
        raise ConversionError("Именованные потоки NTFS в выходном пути запрещены.")
    # Resolve links in the target, but keep the authorised workspace boundary fixed.
    if not target.is_relative_to(WORKSPACE):
        raise ConversionError(f"Выходной файл разрешён только внутри {WORKSPACE}.")
    if target == source.resolve():
        raise ConversionError("Исходный файл нельзя перезаписывать.")
    if target.suffix.lower() != suffix:
        raise ConversionError(f"Выходной файл должен иметь расширение {suffix}.")

    created = False
    try:
        target.parent.mkdir(parents=True, exist_ok=True)
        with target.open("xb") as stream:
            created = True
            if stream.write(data) != len(data):
                raise OSError("неполная запись файла")
    except BaseException as error:
        if created:
            target.unlink(missing_ok=True)
        if isinstance(error, OSError):
            raise ConversionError(f"Не удалось сохранить {target}: {error}") from error
        raise
    return target


def _result(source, target, input_bytes, data, root):
    return {
        "input_path": str(source.resolve()),
        "output_path": str(target),
        "input_bytes": input_bytes,
        "output_bytes": len(data),
        "root_element": root,
        "validation": "passed",
        "output_sha256": hashlib.sha256(data).hexdigest(),
    }


def decode_iwf(source: Path, output=None, *, max_bytes: int = MAX_XML_BYTES) -> dict:
    _check_limit(max_bytes)
    source = Path(source)
    decompressor = zlib.decompressobj(zlib.MAX_WBITS)
    data = bytearray()
    input_bytes = 0
    try:
        with source.open("rb") as stream:
            while chunk := stream.read(CHUNK_BYTES):
                input_bytes += len(chunk)
                data.extend(decompressor.decompress(chunk, max_bytes - len(data) + 1))
                if len(data) > max_bytes or decompressor.unconsumed_tail:
                    raise ConversionError(f"Превышен лимит распакованного XML: {max_bytes} байт.")
                if decompressor.eof:
                    if decompressor.unused_data or stream.read(1):
                        raise ConversionError("Неподдерживаемая структура IWF: данные после конца zlib-потока.")
                    break
    except zlib.error as error:
        raise ConversionError(
            f"Неподдерживаемый или повреждённый IWF: ожидается zlib-поток от смещения 0 ({error})."
        ) from error
    if not decompressor.eof:
        raise ConversionError("Усечённый или пустой IWF: zlib-поток не завершён.")

    root = validate_xml(data)
    target = _save(data, source, output, ".xml")
    result = _result(source, target, input_bytes, data, root)
    result.update(stream_complete=True, trailing_data=False)
    return result


def encode_iwf(source: Path, output=None, *, max_bytes: int = MAX_XML_BYTES) -> dict:
    _check_limit(max_bytes)
    source = Path(source)
    with source.open("rb") as stream:
        xml = stream.read(max_bytes + 1)
    if len(xml) > max_bytes:
        raise ConversionError(f"Превышен лимит XML: {max_bytes} байт.")
    root = validate_xml(xml)
    data = zlib.compress(xml)
    target = _save(data, source, output, ".iwf")
    result = _result(source, target, len(xml), data, root)
    result["xml_sha256"] = hashlib.sha256(xml).hexdigest()
    return result


def main(argv=None) -> int:
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("operation", choices=("decode", "encode"))
    parser.add_argument("source", type=Path, help="IWF / каталог IWF для decode; XML для encode")
    parser.add_argument("--output", type=Path, help="имя или путь внутри output/business-processes")
    parser.add_argument("--max-bytes", type=int, default=MAX_XML_BYTES, help="лимит XML, не более 20 МиБ")
    args = parser.parse_args(argv)
    results = []
    try:
        if args.source.is_dir():
            if args.operation != "decode" or args.output is not None:
                raise ConversionError("Каталог разрешён только для decode без --output.")
            sources = sorted(p for p in args.source.iterdir() if p.is_file() and p.suffix.lower() == ".iwf")
            if not sources:
                raise ConversionError("В каталоге нет IWF-файлов.")
        else:
            sources = [args.source]
        convert = decode_iwf if args.operation == "decode" else encode_iwf
        for source in sources:
            try:
                results.append(convert(source, args.output, max_bytes=args.max_bytes))
            except (ConversionError, OSError) as error:
                results.append({"input_path": str(source), "validation": "failed", "error": str(error)})
    except (ConversionError, OSError) as error:
        results.append({"input_path": str(args.source), "validation": "failed", "error": str(error)})
    failed = sum(result["validation"] == "failed" for result in results)
    print(json.dumps({"results": results, "succeeded": len(results) - failed, "failed": failed}, ensure_ascii=False, indent=2))
    return 1 if failed else 0


if __name__ == "__main__":
    sys.exit(main())
