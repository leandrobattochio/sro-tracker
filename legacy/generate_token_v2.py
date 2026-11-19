#!/usr/bin/env python3
import base64
import fcntl
import hashlib
import hmac
import os
from pathlib import Path

from correios_token import PUBKEY_2, PUBKEY_3, iso_timestamp
from correios_token import generate_token as encrypt_token

# Valores confirmados no APK instalado e no call-site Dart de /liberacao.
PACKAGE_NAME = "br.homelab.app"
BUILD_SIGNATURE = "my_signature"
VERSION = "my_version"
BUILD_NUMBER = "0.1"
PLATFORM = "iOS"
KEY_SELECTION = 1  # param_9=1: pubkey_3 e segredo HOTP correspondente.
COUNTER_PATH = Path(__file__).resolve().with_name("counter")

_HOTP_SECRETS = (
    "hidden1",
    "hidden2",
)
_PUBLIC_KEYS = (PUBKEY_2, PUBKEY_3)


def hotp_code(counter: int, key_selection: int = KEY_SELECTION) -> str:
    """HOTP de 8 digitos, HMAC-SHA256, contador big-endian de 8 bytes."""
    if not isinstance(counter, int) or not 0 <= counter <= 0x7FFFFFFF:
        raise ValueError("O contador deve ser um inteiro entre 0 e 2147483647.")
    if key_selection not in (0, 1):
        raise ValueError("key_selection deve ser 0 ou 1.")
    secret = base64.b32decode(_HOTP_SECRETS[key_selection])
    secret = (secret * ((32 + len(secret) - 1) // len(secret)))[:32]
    digest = hmac.new(secret, counter.to_bytes(8, "big"), hashlib.sha256).digest()
    offset = digest[-1] & 0x0F
    value = int.from_bytes(digest[offset:offset + 4], "big") & 0x7FFFFFFF
    return f"{value % 100000000:08d}"


def reserve_counter(path: Path = COUNTER_PATH) -> int:
    """Reserva um contador persistente; nao modifica o contador do aparelho."""
    with Path(path).open("a+", encoding="ascii") as state:
        fcntl.flock(state, fcntl.LOCK_EX)
        state.seek(0)
        saved = state.read().strip()
        counter = int(saved) if saved else 0
        hotp_code(counter)
        state.seek(0)
        state.truncate()
        state.write(f"{counter + 1}\n")
        state.flush()
        os.fsync(state.fileno())
        return counter


def build_plaintext(
    *, counter: int, package_name: str = PACKAGE_NAME,
    build_signature: str = BUILD_SIGNATURE, version: str = VERSION,
    build_number: str = BUILD_NUMBER, platform: str = PLATFORM,
    key_selection: int = KEY_SELECTION, ts: str | None = None,
) -> str:
    return ";".join((
        hotp_code(counter, key_selection), str(counter),
        iso_timestamp() if ts is None else ts, platform, package_name,
        build_signature, version, str(build_number),
    ))


def generate_token_with_plaintext(
    *, counter: int, key_selection: int = KEY_SELECTION,
    package_name: str = PACKAGE_NAME, build_signature: str = BUILD_SIGNATURE,
    version: str = VERSION, build_number: str = BUILD_NUMBER,
    platform: str = PLATFORM, ts: str | None = None, key=None,
) -> tuple[str, str]:
    """Retorna (requestToken, plaintext exato usado na criptografia)."""
    plaintext = build_plaintext(
        counter=counter, key_selection=key_selection, package_name=package_name,
        build_signature=build_signature, version=version, build_number=build_number,
        platform=platform, ts=ts,
    )
    token = encrypt_token(
        "", "", "", "", plaintext=plaintext,
        key=_PUBLIC_KEYS[key_selection] if key is None else key,
    )
    return token, plaintext


def main():
    counter = reserve_counter()
    token, plaintext = generate_token_with_plaintext(counter=counter)
    print(f"chave=pubkey_{KEY_SELECTION + 2} contador={counter}")
    print(f"plaintext: {plaintext}")
    print(f"len cipher: {len(base64.b64decode(token))} bytes")
    print(f"requestToken: {token}")


if __name__ == "__main__":
    main()
