# syntax=docker/dockerfile:1
FROM adoptopenjdk/openjdk12:jre-12.0.2_10-alpine
COPY build/install/orders app/
WORKDIR /app/bin
ENTRYPOINT ["./orders-server"]