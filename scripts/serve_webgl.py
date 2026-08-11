#!/usr/bin/env python3
"""Serve the local WebGL build without allowing stale Unity build artifacts."""

import argparse
from http.server import SimpleHTTPRequestHandler, ThreadingHTTPServer
from pathlib import Path


class NoCacheHandler(SimpleHTTPRequestHandler):
    def end_headers(self) -> None:
        self.send_header("Cache-Control", "no-store, no-cache, must-revalidate, max-age=0")
        self.send_header("Pragma", "no-cache")
        self.send_header("Expires", "0")
        super().end_headers()


def main() -> None:
    parser = argparse.ArgumentParser()
    parser.add_argument("--host", default="127.0.0.1")
    parser.add_argument("--port", type=int, default=8000)
    parser.add_argument(
        "--directory",
        type=Path,
        default=Path(__file__).resolve().parents[1] / "local-build" / "webgl",
    )
    args = parser.parse_args()

    handler = lambda *handler_args, **kwargs: NoCacheHandler(  # noqa: E731
        *handler_args, directory=str(args.directory.resolve()), **kwargs
    )
    server = ThreadingHTTPServer((args.host, args.port), handler)
    print(f"Serving cache-safe WebGL at http://{args.host}:{args.port}/")
    server.serve_forever()


if __name__ == "__main__":
    main()
