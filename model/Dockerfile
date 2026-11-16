# syntax=docker/dockerfile:1

FROM scratch
ARG MODEL_URL=https://huggingface.co/unsloth/Qwen3-4B-Instruct-2507-GGUF/resolve/a06e946bb6b655725eafa393f4a9745d460374c9/Qwen3-4B-Instruct-2507-Q4_K_M.gguf
ARG MODEL_SHA256=3605803b982cb64aead44f6c1b2ae36e3acdb41d8e46c8a94c6533bc4c67e597
ADD --chmod=0444 --checksum=sha256:${MODEL_SHA256} ${MODEL_URL} /model.gguf
