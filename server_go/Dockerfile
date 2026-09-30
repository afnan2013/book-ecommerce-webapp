# Build on the runner's native platform and cross-compile, so an arm64 image
# builds fast on an amd64 CI runner without CPU emulation.
FROM --platform=$BUILDPLATFORM golang:1.27.1 AS build

ARG TARGETOS
ARG TARGETARCH

WORKDIR /src

COPY go.mod go.sum ./
RUN go mod download

COPY cmd ./cmd
COPY internal ./internal

RUN CGO_ENABLED=0 GOOS=$TARGETOS GOARCH=$TARGETARCH \
    go build -trimpath -ldflags="-s -w" -o /out/api ./cmd/api && \
    CGO_ENABLED=0 GOOS=$TARGETOS GOARCH=$TARGETARCH \
    go build -trimpath -ldflags="-s -w" -o /out/migrate ./cmd/migrate

FROM gcr.io/distroless/static-debian13:nonroot

WORKDIR /app

COPY --from=build /out/api /app/api
COPY --from=build /out/migrate /app/migrate
COPY migrations /app/migrations

EXPOSE 8080

# CMD rather than ENTRYPOINT: ECS run-task can override the command but not the
# entrypoint, which is how the pipeline runs ["/app/migrate", "up"] from this image.
CMD ["/app/api"]
