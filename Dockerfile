# Unity Headless Server Docker Image
FROM ubuntu:22.04

ARG BUILD_PATH=build/server
ARG GAME_SERVER_IP=0.0.0.0
ARG GAME_SERVER_PORT=7777

# Install required dependencies for Unity server
RUN apt-get update && apt-get install -y \
    ca-certificates \
    libc6 \
    libgcc-s1 \
    libgssapi-krb5-2 \
    libstdc++6 \
    zlib1g \
    libpulse0 \
    && rm -rf /var/lib/apt/lists/*

# Create non-root user for security
RUN useradd -m -d /home/gameserver -s /bin/bash gameserver

# Set working directory
WORKDIR /app

# Copy the server build
COPY ${BUILD_PATH}/ ./

# Make the server executable
RUN chmod +x ./GameServer && \
    chown -R gameserver:gameserver /app

# Switch to non-root user
USER gameserver

# Expose default Unity Transport port
EXPOSE 7777/udp

# Health check (optional - adjust based on your game's needs)
HEALTHCHECK --interval=30s --timeout=10s --start-period=10s --retries=3 \
    CMD pgrep -x GameServer || exit 1

# Set environment variables for headless mode
ENV DISPLAY=:0
ENV GAME_SERVER_IP=${GAME_SERVER_IP}
ENV GAME_SERVER_PORT=${GAME_SERVER_PORT}

# Run the server
ENTRYPOINT ["./GameServer", "-batchmode", "-nographics"]
CMD ["-logFile", "/dev/stdout"]
