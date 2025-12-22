# dyncompressor

**dyncompressor** is a custom Windows compression tool built as an experimental, research-driven project exploring dynamic compression strategies, chunk-based processing, and binary-level analysis.

This project was designed as both a technical exploration and a portfolio-grade system demonstrating real-world software engineering tradeoffs.

---

## Key Features

- Custom archive format: **`.dcz`**
- Dynamic file chunking based on file size
- Per-chunk compression algorithm selection
- Parallel compression pipeline
- Safe handling of executables and already-compressed files
- Automatic fallback to STORE when compression is not beneficial
- Symmetric compression / decompression
- Versioned archive format
- WinForms graphical interface

---

## Core Ideas & Motivation

### 1. Treat files as **pure binary**
Instead of relying only on file extensions, the system analyzes raw byte data:
- entropy
- repetition
- structure
- compressibility

This allows more informed decisions than traditional static approaches.

---

### 2. Dynamic chunking
Large files are split into dynamically-sized chunks to:
- improve compression efficiency
- reduce memory usage
- enable parallel processing
- allow per-chunk optimization

---

### 3. Dynamic compression selection
Each chunk is analyzed independently and compressed using the most appropriate algorithm available:
- LZMA
- BZip2
- Brotli
- GZip
- Deflate
- Lossless image compression
- STORE (no compression)

If compression does not reduce size, raw storage is used automatically.

---

## Architecture Overview

Main components:

- **UltraModeManager**
  - Producer / consumer pipeline
  - Chunk scheduling
  - Parallel processing
  - Archive writing

- **ChunkAnalyzer**
  - Entropy calculation
  - Data pattern detection
  - Image detection

- **ChunkCompressorPicker**
  - Chooses best algorithm per chunk
  - Applies safety rules for executables

- **Compression Algorithms**
  - Unified interface (`ICompressionAlgorithm`)
  - Modular and extensible

- **WinForms UI**
  - Drag & drop support
  - Progress reporting
  - Time estimation
  - Clean layout and custom icon

---

## Archive Format (`.dcz`)

- Versioned binary format
- Stores:
  - relative file paths
  - chunk metadata
  - compression method per chunk
  - preprocessing flags (future-proofed)
- Supports restoration of empty directories

---

## Why this project exists

This project is **not intended to outperform mature tools like WinRAR or 7-Zip**.

Instead, it demonstrates:
- system design
- algorithmic reasoning
- performance tradeoffs
- robustness
- evolution of ideas over time

It was built to explore *how* compression systems make decisions, not just *what* they compress with.

---

## Status

- ✔ Compression & decompression stable
- ✔ UI functional and polished
- ✔ Architecture finalized
- ✔ Project frozen for portfolio use

Future improvements are possible but not required for its intended purpose.

---

## Requirements

- Windows
- .NET (WinForms)
- Visual Studio (recommended)

---

## License

This project is provided for educational and portfolio purposes.
