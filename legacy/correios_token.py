#!/usr/bin/env python3

import base64
from datetime import datetime
from Crypto.PublicKey import RSA
from Crypto.Cipher import PKCS1_OAEP
from Crypto.Hash import SHA256, SHA1
from Crypto.Signature.pss import MGF1

# Chaves publicas RSA-2048 embutidas na libjwt (p9 seleciona)
PUBKEY_0 = """HIDDEN"""

PUBKEY_1 = """HIDDEN"""

PUBKEY_2 = """HIDDEN"""

PUBKEY_3 = """HIDDEN"""


def iso_timestamp(now: datetime | None = None) -> str:
    """YYYY-MM-DDTHH:MM:SS.NNNNNNNNN±HH:00  (strftime %Y-%m-%dT%H:%M:%S + '.' + %09lld ns + %c%02d:00)."""
    now = now or datetime.now().astimezone()
    base = now.strftime("%Y-%m-%dT%H:%M:%S")
    nanos = f"{now.microsecond * 1000:09d}"
    off = now.utcoffset()
    total_min = int(off.total_seconds() // 60) if off else 0
    sign = "+" if total_min >= 0 else "-"
    hh = min(abs(total_min) // 60, 99)
    return f"{base}.{nanos}{sign}{hh:02d}:00"


def build_plaintext(board, bootloader, brand, device, p3="null", platform="android",
                    ts=None, field0="null") -> str:
    return f"{field0};{p3};{ts or iso_timestamp()};{board};{bootloader};{brand};{device};{platform}"


def generate_token(board, bootloader, brand, device, p3="null", key=PUBKEY_2,
                   mgf1_hash=SHA256, plaintext=None) -> str:
    if plaintext is None:
        plaintext = build_plaintext(board, bootloader, brand, device, p3=p3)
    k = RSA.import_key(key)
    mgf = lambda x, y: MGF1(x, y, mgf1_hash)
    cipher = PKCS1_OAEP.new(k, hashAlgo=SHA256, mgfunc=mgf)
    ct = cipher.encrypt(plaintext.encode("utf-8"))   # 256 bytes
    return base64.b64encode(ct).decode("ascii")


if __name__ == "__main__":
    tok = generate_token(board="sdm660", bootloader="unknown",
                         brand="xiaomi", device="lavender", p3="null", key=PUBKEY_2)
    pt = build_plaintext("sdm660", "unknown", "xiaomi", "lavender")
    print("plaintext :", pt)
    print("len cipher:", len(base64.b64decode(tok)), "bytes")
    print("requestToken:", tok)
