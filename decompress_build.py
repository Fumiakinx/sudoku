"""
Unity WebGL の .br (Brotli圧縮) ファイルを解凍して、
GitHub Pages で使える非圧縮ファイルを生成するスクリプト
"""
import brotli
import os

# 解凍対象ファイルの定義: (入力ファイル, 出力ファイル)
build_dir = os.path.join(os.path.dirname(__file__), "WebGLBuilds", "Sudoku", "Build")

files_to_decompress = [
    ("Sudoku.framework.js.br", "Sudoku.framework.js"),
    ("Sudoku.data.br",         "Sudoku.data"),
    ("Sudoku.wasm.br",         "Sudoku.wasm"),
]

for input_name, output_name in files_to_decompress:
    input_path  = os.path.join(build_dir, input_name)
    output_path = os.path.join(build_dir, output_name)

    print(f"解凍中: {input_name} → {output_name}", end=" ... ")

    with open(input_path, "rb") as f:
        compressed_data = f.read()

    decompressed_data = brotli.decompress(compressed_data)

    with open(output_path, "wb") as f:
        f.write(decompressed_data)

    original_size    = len(compressed_data)  / (1024 * 1024)
    decompressed_size = len(decompressed_data) / (1024 * 1024)
    print(f"完了 ({original_size:.1f} MB → {decompressed_size:.1f} MB)")

print("\n全ファイルの解凍が完了しました！")
