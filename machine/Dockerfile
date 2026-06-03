FROM docker.io/library/rust:1.96-alpine AS build
WORKDIR /src/machine

RUN apk add --no-cache clang lld musl-dev git protobuf-dev

# proto/ lives at the repo root, shared with the web project; build.rs reads ../proto
COPY proto /src/proto
COPY machine/Cargo.toml machine/Cargo.lock machine/build.rs ./
COPY machine/src ./src

RUN --mount=type=cache,target=/src/machine/target/ \
    --mount=type=cache,target=/usr/local/cargo/git/db \
    --mount=type=cache,target=/usr/local/cargo/registry/ \
    cargo build --locked --release && \
    cp ./target/release/machine /bin/machine

FROM docker.io/library/alpine:3.18 AS final

RUN adduser \
    --disabled-password \
    --gecos "" \
    --home "/nonexistent" \
    --shell "/sbin/nologin" \
    --no-create-home \
    --uid "10001" \
    appuser

USER appuser

COPY --from=build /bin/machine /bin/
CMD ["/bin/machine"]
