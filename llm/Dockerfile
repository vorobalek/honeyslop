# syntax=docker/dockerfile:1

ARG LLAMA_CPP_VERSION=v1.0.0

FROM debian:trixie-slim AS build
ARG LLAMA_CPP_VERSION
ARG GGML_CPU_FLAGS="-DGGML_AVX=ON -DGGML_AVX2=ON -DGGML_FMA=ON -DGGML_F16C=ON -DGGML_BMI2=ON"
RUN apt-get update \
 && apt-get install -y --no-install-recommends build-essential cmake git ca-certificates \
 && rm -rf /var/lib/apt/lists/*
RUN git clone --depth 1 --branch "$LLAMA_CPP_VERSION" https://github.com/ggml-org/llama.cpp /src
RUN cmake -S /src -B /build \
        -DCMAKE_BUILD_TYPE=Release \
        -DBUILD_SHARED_LIBS=OFF \
        -DGGML_NATIVE=OFF \
        $GGML_CPU_FLAGS \
        -DGGML_OPENMP=OFF \
        -DLLAMA_OPENSSL=OFF \
        -DLLAMA_SUBPROCESS=OFF \
        -DLLAMA_BUILD_UI=OFF \
        -DLLAMA_USE_PREBUILT_UI=OFF \
        -DLLAMA_BUILD_APP=OFF \
        -DLLAMA_BUILD_TESTS=OFF \
        -DLLAMA_BUILD_EXAMPLES=OFF \
 && cmake --build /build --target llama-server -j"$(nproc)"

FROM gcr.io/distroless/cc-debian13:nonroot
COPY --from=build /build/bin/llama-server /app/llama-server
EXPOSE 8080
ENTRYPOINT ["/app/llama-server", "--model", "/models/model.gguf", "--alias", "honeyslop", "--host", "0.0.0.0", "--port", "8080", "--offline", "--no-webui", "--slots", "--metrics", "--reasoning", "off"]
