# Dockerfile for Janitorr - Spring Boot Kotlin Application
# This Dockerfile builds the application from source and creates a production-ready image

# Build stage
FROM eclipse-temurin:23-jdk-noble AS builder

WORKDIR /workspace/app

# Copy Gradle wrapper and build files
COPY gradlew .
COPY gradle gradle
COPY build.gradle.kts .
COPY settings.gradle.kts .
COPY gradle.properties .
COPY buildSrc buildSrc

# Make gradlew executable
RUN chmod +x ./gradlew

# Download dependencies (this layer will be cached if dependencies don't change)
RUN ./gradlew dependencies --no-daemon

# Copy source code
COPY src src

# Build the application
RUN ./gradlew bootJar --no-daemon

# Runtime stage
FROM eclipse-temurin:23-jre-noble

LABEL org.opencontainers.image.title="Janitorr"
LABEL org.opencontainers.image.description="Cleans up your media library"
LABEL org.opencontainers.image.authors="Schaka <schaka@github.com>"
LABEL org.opencontainers.image.source="https://github.com/Schaka/janitorr"

# Create non-root user
RUN groupadd -r janitorr && useradd -r -g janitorr janitorr

# Create directories
RUN mkdir -p /config /workspace /logs && \
    chown -R janitorr:janitorr /config /workspace /logs

# Copy the built jar from builder stage
COPY --from=builder --chown=janitorr:janitorr /workspace/app/build/libs/*.jar /workspace/app.jar

# Switch to non-root user
USER janitorr

# Set working directory
WORKDIR /workspace

# Expose port
EXPOSE 8978

# Health check
HEALTHCHECK --interval=30s --timeout=5s --start-period=40s --retries=3 \
    CMD curl -f http://localhost:8978/health || exit 1

# JVM settings optimized for containers
ENV JAVA_OPTS="-Xms256m -Xmx1024m \
    -Dspring.config.additional-location=optional:file:/config/application.yaml,optional:file:/workspace/application.yaml,optional:file:/workspace/application.yml \
    -Dsun.jnu.encoding=UTF-8 \
    -Dfile.encoding=UTF-8 \
    -XX:+UseContainerSupport \
    -XX:MaxRAMPercentage=75.0"

# Run the application
CMD ["sh", "-c", "java ${JAVA_OPTS} -jar app.jar"]
