# Unity Headless Server Docker Image
FROM ubuntu:22.04

ARG BUILD_PATH=build/StandaloneLinux64
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

# Debug: show what was copied
RUN echo "=== Files in /app ===" && \
    ls -la /app/ && \
    echo "=== Looking for executables ===" && \
    find /app -type f -executable 2>/dev/null || true

# Make all executables runnable and set ownership
RUN chmod +x ./*.x86_64 2>/dev/null || chmod +x ./$(ls | grep -v _Data | head -1) 2>/dev/null || true && \
    chown -R gameserver:gameserver /app

# Switch to non-root user
USER gameserver

# Expose default Unity Transport port
EXPOSE 7777/udp

# Health check (optional - adjust based on your game's needs)
HEALTHCHECK --interval=30s --timeout=10s --start-period=10s --retries=3 \
    CMD pgrep -f ".x86_64" || exit 1

# Set environment variables for headless mode
ENV DISPLAY=:0
ENV GAME_SERVER_IP=${GAME_SERVER_IP}
ENV GAME_SERVER_PORT=${GAME_SERVER_PORT}

# Run the server - find executable (either .x86_64 or the main binary)
ENTRYPOINT ["/bin/bash", "-c", "SERVER=$(find /app -maxdepth 1 -type f -executable ! -name '*.so' | head -1) && echo \"Starting: $SERVER\" && exec \"$SERVER\" -batchmode -nographics \"$@\"", "--"]
CMD ["-logFile", "/dev/stdout"]
