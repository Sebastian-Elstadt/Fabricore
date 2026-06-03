FROM docker.io/library/rust:1.96-alpine AS build
WORKDIR /app

RUN apk add --no-cache clang lld musl-dev git protobuf-dev

COPY Cargo.toml Cargo.lock build.rs ./
COPY src ./src

RUN --mount=type=cache,target=/app/target/ \
    --mount=type=cache,target=/usr/local/cargo/git/db \
    --mount=type=cache,target=/usr/local/cargo/registry/ \
    cargo build --locked --release

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

COPY --from=build /app/target/release/machine /bin/
CMD ["/bin/machine"]