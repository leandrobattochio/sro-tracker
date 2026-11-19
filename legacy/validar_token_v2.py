#!/usr/bin/env python3

import base64
import time
from urllib.parse import urljoin

import requests
from Crypto.Cipher import AES, PKCS1_OAEP
from Crypto.Hash import SHA256
from Crypto.PublicKey import RSA
from Crypto.Random import get_random_bytes

from correios_token import PUBKEY_1
from generate_token_v2 import generate_token_with_plaintext, reserve_counter, KEY_SELECTION

URL = "https://localhost:9898/v4/app-validation/liberacao"
VERSION_URL = urljoin(URL, "/v1/versao-atual")
FUNCIONALIDADES_URL = urljoin(URL, "/v1/funcionalidades")
RASTREIO_URL = urljoin(URL, "/v1/sro-rastro/NN459724226BR")


HEADERS_BASE = {
    "Content-Type": "application/json; charset=utf-8",
    "User-Agent": "Dart/3.13 (dart:io)",
}

def build_app_token_check(jwt: str) -> str:
    """encrypt_data(mode=1): base64(RSA(key) || IV || ciphertext || tag)."""
    agora_segundos = time.time_ns() // 1_000_000
    plaintext = f"{agora_segundos};{jwt}".encode("utf-8")
    aes_key = get_random_bytes(32)
    iv = get_random_bytes(12)
    encrypted_key = PKCS1_OAEP.new(
        RSA.import_key(PUBKEY_1), hashAlgo=SHA256, label=b"",
    ).encrypt(aes_key)
    cipher = AES.new(aes_key, AES.MODE_GCM, nonce=iv, mac_len=16)
    ciphertext, tag = cipher.encrypt_and_digest(plaintext)
    return base64.b64encode(encrypted_key + iv + ciphertext + tag).decode("ascii")


def post(client: requests.Session, body: dict, extra_headers: dict):
    headers = {**HEADERS_BASE, **extra_headers}
    return client.post(
        URL,
        json=body,
        headers=headers,
        timeout=20,
        allow_redirects=False,
    )

def main():
    counter = reserve_counter()
    token, plaintext = generate_token_with_plaintext(counter=counter)
    print("=" * 70)
    print(f"chave=pubkey_{KEY_SELECTION + 2} contador={counter}")
    print(f"plaintext: {plaintext}")
    try:
        with requests.Session() as client:
            response = post(client, {"requestToken": token}, {})
            print(f"POST /v4/app-validation/liberacao HTTP {response.status_code}: {response.text}")
            response.raise_for_status()
            if not 200 <= response.status_code < 300:
                raise ValueError("Liberação não retornou sucesso; GET cancelado.")

            payload = response.json()
            jwt = payload.get("token") if isinstance(payload, dict) else None
            if not isinstance(jwt, str) or not jwt.strip():
                raise ValueError("Liberação não retornou um JWT em 'token'; GET cancelado.")
            cookie = client.cookies.get("LBprdint2")
            if not cookie:
                raise ValueError("Liberação não retornou o cookie LBprdint2; GET cancelado.")

            header = build_app_token_check(jwt)
            response = client.get(
                VERSION_URL,
                params={"plataforma": "ANDROID"},
                headers={
                    **HEADERS_BASE,
                    "Cookie": f"LBprdint2={cookie}",
                    "app-checker": header,
                },
                timeout=20,
                allow_redirects=False,
            )
            print(f"GET /v1/versao-atual?plataforma=ANDROID HTTP {response.status_code}: {response.text}")
            response.raise_for_status()


            # Funcionalidades
            header = build_app_token_check(jwt)
            response = client.get(
                FUNCIONALIDADES_URL,
                headers={
                    **HEADERS_BASE,
                    "Cookie": f"LBprdint2={cookie}",
                    "app-checker": header,
                },
                timeout=20,
                allow_redirects=False
            )
            #print(f"GET /v1/funcionalidades {response.status_code}: {response.text}")
            response.raise_for_status()

            #Rastreio
            header = build_app_token_check(jwt)
            response = client.get(
                RASTREIO_URL,
                headers = {
                    **HEADERS_BASE,
                    "Cookie": f"LBprdint2={cookie}",
                    "app-checker": header,
                },
                timeout=20,
                allow_redirects=False
            )
            print(f"GET /v1/sro-rastro {response.status_code}: {response.text}")
            response.raise_for_status()


    except (requests.RequestException, ValueError) as exc:
        print(f"ERRO: {exc}")

if __name__ == "__main__":
    main()
