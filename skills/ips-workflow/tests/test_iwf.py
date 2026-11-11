import contextlib
import gzip
import importlib.util
import io
import json
import os
from pathlib import Path
import subprocess
import tempfile
import unittest
from unittest.mock import patch
import zlib


SUITE = Path(__file__).resolve().parents[3]
SPEC = importlib.util.spec_from_file_location(
    "iwf", SUITE / "skills/ips-workflow/scripts/iwf.py"
)
iwf = importlib.util.module_from_spec(SPEC)
SPEC.loader.exec_module(iwf)

XML = b'<?xml version="1.0"?>\r\n<SimpleBriefcase><Version>1</Version></SimpleBriefcase>\r\n'


class IwfTests(unittest.TestCase):
    def setUp(self):
        test_root = SUITE / "output" / "business-processes"
        test_root.mkdir(parents=True, exist_ok=True)
        self.temp = tempfile.TemporaryDirectory(dir=test_root, prefix="iwf-test-")
        self.addCleanup(self.temp.cleanup)
        self.base = Path(self.temp.name)
        self.workspace = self.base / "workspace"
        self.workspace.mkdir()
        self.inputs = self.base / "inputs"
        self.inputs.mkdir()
        workspace_patch = patch.object(iwf, "WORKSPACE", self.workspace)
        workspace_patch.start()
        self.addCleanup(workspace_patch.stop)

    def source(self, data, name="sample.iwf"):
        path = self.inputs / name
        path.write_bytes(data)
        return path

    def assert_rejected(self, data, **kwargs):
        source = self.source(data)
        with self.assertRaises(iwf.ConversionError):
            iwf.decode_iwf(source, **kwargs)
        self.assertEqual(source.read_bytes(), data)
        self.assertEqual(list(self.workspace.rglob("*.xml")), [])

    def test_reference_sample_is_byte_identical(self):
        source = Path(__file__).parent / "fixtures/SATE-Soglasovanie-TD.iwf"
        reference = SUITE / "manuals/business-processes/САТЕ_Согласование_ТД_old.xml"
        original = source.read_bytes()
        result = iwf.decode_iwf(source)
        self.assertEqual(Path(result["output_path"]).read_bytes(), reference.read_bytes())
        self.assertEqual(source.read_bytes(), original)
        self.assertEqual(result["input_bytes"], len(original))
        self.assertEqual(result["output_bytes"], 165134)
        self.assertEqual(result["root_element"], "SimpleBriefcase")
        self.assertEqual(result["validation"], "passed")
        self.assertTrue(result["stream_complete"])
        self.assertFalse(result["trailing_data"])

    def test_keeps_encoding_bom_whitespace_and_line_endings(self):
        xml = '<?xml version="1.0" encoding="utf-16"?>\r\n<SimpleBriefcase>ТД &amp; КД</SimpleBriefcase>\r\n'.encode("utf-16")
        result = iwf.decode_iwf(self.source(zlib.compress(xml), "Имя с пробелами.iwf"))
        self.assertEqual(Path(result["output_path"]).read_bytes(), xml)
        self.assertEqual(result["root_element"], "SimpleBriefcase")

    def test_accepts_zlib_compression_levels_without_header_guessing(self):
        for level in (0, 1, 6, 9):
            with self.subTest(level=level):
                result = iwf.decode_iwf(self.source(zlib.compress(XML, level)), f"level-{level}.xml")
                self.assertEqual(Path(result["output_path"]).read_bytes(), XML)

    def test_rejects_corrupted_checksum(self):
        data = zlib.compress(XML)
        self.assert_rejected(data[:-1] + bytes([data[-1] ^ 255]))

    def test_rejects_truncated_stream_even_when_xml_is_complete(self):
        data = zlib.compress(XML)
        for cut in (1, 4, 8):
            with self.subTest(cut=cut):
                self.assert_rejected(data[:-cut])

    def test_rejects_trailing_bytes_and_concatenated_streams(self):
        for tail in (b"\x00", b"trailer", zlib.compress(XML)):
            with self.subTest(tail=tail):
                self.assert_rejected(zlib.compress(XML) + tail)

    def test_rejects_trailing_bytes_in_next_read_chunk(self):
        with patch.object(iwf, "CHUNK_BYTES", len(zlib.compress(XML))):
            self.assert_rejected(zlib.compress(XML) + b"\x00")

    def test_does_not_scan_offsets_or_fall_back_to_other_formats(self):
        raw = zlib.compressobj(wbits=-zlib.MAX_WBITS)
        formats = (b"", XML, b"prefix" + zlib.compress(XML), gzip.compress(XML), raw.compress(XML) + raw.flush())
        for data in formats:
            with self.subTest(header=data[:8]):
                self.assert_rejected(data)

    def test_rejects_invalid_xml(self):
        for xml in (b"", b"<r>", b"<r/><r/>", b"<r>&unknown;</r>"):
            with self.subTest(xml=xml):
                self.assert_rejected(zlib.compress(xml))

    def test_rejects_unknown_and_unsupported_xml_encodings(self):
        for encoding in ("no-such-encoding", "utf-7"):
            xml = f'<?xml version="1.0" encoding="{encoding}"?><r/>'.encode("ascii")
            for operation in ("decode", "encode"):
                with self.subTest(encoding=encoding, operation=operation):
                    if operation == "decode":
                        self.assert_rejected(zlib.compress(xml))
                    else:
                        with self.assertRaises(iwf.ConversionError):
                            iwf.encode_iwf(self.source(xml, "input.xml"))
        self.assertEqual(list(self.workspace.iterdir()), [])

    def test_rejects_dtd_and_entities_in_utf8_and_utf16(self):
        documents = (
            '<!DOCTYPE r SYSTEM "http://127.0.0.1:1/external.dtd"><r/>',
            '<!DOCTYPE r [<!ENTITY external SYSTEM "file:///nonexistent-secret">]><r>&external;</r>',
            '<!DOCTYPE r [<!ENTITY a "expansion">]><r>&a;</r>',
            '<!DOCTYPE r [<!ENTITY % p SYSTEM "http://127.0.0.1:1/p.dtd">%p;]><r/>',
        )
        for xml in documents:
            for encoding in ("utf-8", "utf-16"):
                with self.subTest(xml=xml, encoding=encoding):
                    self.assert_rejected(zlib.compress(xml.encode(encoding)))

    def test_does_not_process_xinclude_or_stylesheets(self):
        xml = b'<?xml-stylesheet href="http://127.0.0.1:1/style.xsl"?><r xmlns:xi="http://www.w3.org/2001/XInclude"><xi:include href="file:///nonexistent-secret"/></r>'
        result = iwf.decode_iwf(self.source(zlib.compress(xml)))
        self.assertEqual(Path(result["output_path"]).read_bytes(), xml)

    def test_accepts_exact_limit_and_rejects_one_byte_over(self):
        result = iwf.decode_iwf(self.source(zlib.compress(XML)), max_bytes=len(XML))
        self.assertEqual(Path(result["output_path"]).read_bytes(), XML)
        with self.assertRaisesRegex(iwf.ConversionError, "лимит"):
            iwf.decode_iwf(self.source(zlib.compress(XML)), "over.xml", max_bytes=len(XML) - 1)
        self.assertFalse((self.workspace / "over.xml").exists())

    def test_enforces_default_twenty_mib_limit(self):
        self.assertEqual(iwf.MAX_XML_BYTES, 20 * 1024 * 1024)
        self.assert_rejected(zlib.compress(b"<r>" + b" " * iwf.MAX_XML_BYTES + b"</r>"))

    def test_rejects_invalid_or_increased_limits(self):
        for limit in (0, -1, iwf.MAX_XML_BYTES + 1):
            with self.subTest(limit=limit):
                self.assert_rejected(zlib.compress(XML), max_bytes=limit)

    def test_accepts_multichunk_stream(self):
        with patch.object(iwf, "CHUNK_BYTES", 7):
            result = iwf.decode_iwf(self.source(zlib.compress(XML)))
        self.assertEqual(Path(result["output_path"]).read_bytes(), XML)
        self.assertEqual(result["input_bytes"], len(zlib.compress(XML)))

    def test_rejects_output_outside_workspace(self):
        for output in ("../escape.xml", self.base / "escape.xml"):
            with self.subTest(output=output):
                self.assert_rejected(zlib.compress(XML), output=output)
        self.assertFalse((self.base / "escape.xml").exists())

    def test_rejects_output_through_directory_link(self):
        outside = self.base / "outside"
        outside.mkdir()
        link = self.workspace / "linked"
        if os.name == "nt":
            completed = subprocess.run(["cmd", "/c", "mklink", "/J", str(link), str(outside)], capture_output=True)
            self.assertEqual(completed.returncode, 0, completed.stderr)
            self.addCleanup(os.rmdir, link)
        else:
            link.symlink_to(outside, target_is_directory=True)
            self.addCleanup(link.unlink)
        self.assert_rejected(zlib.compress(XML), output="linked/escape.xml")
        self.assertEqual(list(outside.iterdir()), [])

    def test_does_not_overwrite_existing_output(self):
        target = self.workspace / "existing.xml"
        target.write_bytes(b"user work")
        with self.assertRaises(iwf.ConversionError):
            iwf.decode_iwf(self.source(zlib.compress(XML)), target)
        self.assertEqual(target.read_bytes(), b"user work")

    def test_never_overwrites_input(self):
        source = self.workspace / "source.iwf"
        data = zlib.compress(XML)
        source.write_bytes(data)
        with self.assertRaises(iwf.ConversionError):
            iwf.decode_iwf(source, source)
        self.assertEqual(source.read_bytes(), data)

    def test_rejects_alternate_output_streams(self):
        cases = (
            (iwf.decode_iwf, "source.iwf", zlib.compress(XML), ".xml"),
            (iwf.encode_iwf, "source.xml", XML, ".iwf"),
        )
        for convert, name, data, suffix in cases:
            source = self.workspace / name
            source.write_bytes(data)
            for output in (f"{name}:result{suffix}", f"directory:stream/result{suffix}"):
                with self.subTest(operation=convert.__name__, output=output):
                    with self.assertRaisesRegex(iwf.ConversionError, "поток"):
                        convert(source, output)
                    self.assertFalse((self.workspace / output).exists())
                    self.assertEqual(source.read_bytes(), data)

    def test_removes_incomplete_output_on_write_error(self):
        source = self.source(zlib.compress(XML))
        real_open = Path.open

        def failing_open(path, *args, **kwargs):
            if args and args[0] == "xb":
                real_open(path, "xb").close()
                handle = unittest.mock.MagicMock()
                handle.__enter__.return_value.write.side_effect = OSError("disk full")
                return handle
            return real_open(path, *args, **kwargs)

        with patch.object(Path, "open", failing_open):
            with self.assertRaises(iwf.ConversionError):
                iwf.decode_iwf(source)
        self.assertEqual(list(self.workspace.rglob("*.xml")), [])

    def test_encode_decode_roundtrip_keeps_xml_bytes(self):
        source = self.source(XML, "edited.xml")
        packed = iwf.encode_iwf(source)
        unpacked = iwf.decode_iwf(Path(packed["output_path"]))
        self.assertEqual(Path(unpacked["output_path"]).read_bytes(), XML)
        self.assertEqual(source.read_bytes(), XML)
        self.assertEqual(packed["input_bytes"], len(XML))
        self.assertEqual(packed["root_element"], "SimpleBriefcase")
        self.assertEqual(packed["validation"], "passed")

    def test_encode_rejects_unsafe_or_oversize_xml(self):
        for xml, limit in ((b"<r>", 100), (b'<!DOCTYPE r SYSTEM "file:///secret"><r/>', 100), (XML, len(XML) - 1)):
            with self.subTest(xml=xml):
                with self.assertRaises(iwf.ConversionError):
                    iwf.encode_iwf(self.source(xml, "input.xml"), max_bytes=limit)
        self.assertEqual(list(self.workspace.rglob("*.iwf")), [])

    def test_cli_batch_reports_each_file_and_returns_failure(self):
        self.source(zlib.compress(XML), "good.iwf")
        self.source(b"broken", "bad.iwf")
        self.source(b"ignored", "notes.txt")
        stdout = io.StringIO()
        with contextlib.redirect_stdout(stdout):
            code = iwf.main(["decode", str(self.inputs)])
        report = json.loads(stdout.getvalue())
        self.assertEqual(code, 1)
        self.assertEqual(report["succeeded"], 1)
        self.assertEqual(report["failed"], 1)
        self.assertEqual(len(report["results"]), 2)

    def test_cli_batch_continues_after_unsupported_xml_encodings(self):
        documents = (
            XML,
            b'<?xml version="1.0" encoding="no-such-encoding"?><r/>',
            b'<?xml version="1.0" encoding="utf-7"?><r/>',
            XML,
        )
        for index, xml in enumerate(documents):
            self.source(zlib.compress(xml), f"{index}.iwf")
        stdout = io.StringIO()
        with contextlib.redirect_stdout(stdout):
            code = iwf.main(["decode", str(self.inputs)])
        report = json.loads(stdout.getvalue())
        self.assertEqual(code, 1)
        self.assertEqual(report["succeeded"], 2)
        self.assertEqual(report["failed"], 2)
        self.assertEqual(
            [result["validation"] for result in report["results"]],
            ["passed", "failed", "failed", "passed"],
        )
        for index in (0, 3):
            self.assertEqual((self.workspace / f"{index}.xml").read_bytes(), XML)
        for index in (1, 2):
            self.assertFalse((self.workspace / f"{index}.xml").exists())

    def test_cli_single_encode_and_decode(self):
        source = self.source(XML, "source.xml")
        for operation, path in (("encode", source), ("decode", self.workspace / "source.iwf")):
            stdout = io.StringIO()
            with contextlib.redirect_stdout(stdout):
                code = iwf.main([operation, str(path)])
            report = json.loads(stdout.getvalue())
            self.assertEqual(code, 0)
            self.assertEqual(report["succeeded"], 1)
        self.assertEqual((self.workspace / "source.xml").read_bytes(), XML)

    def test_cli_reports_missing_input_without_traceback(self):
        stdout = io.StringIO()
        with contextlib.redirect_stdout(stdout):
            code = iwf.main(["decode", str(self.inputs / "missing.iwf")])
        report = json.loads(stdout.getvalue())
        self.assertEqual(code, 1)
        self.assertEqual(report["failed"], 1)
        self.assertEqual(report["results"][0]["validation"], "failed")


if __name__ == "__main__":
    unittest.main()
