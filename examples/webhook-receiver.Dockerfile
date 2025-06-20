# Dockerfile for the example webhook receiver
FROM python:3.11-slim

WORKDIR /app

# Install required Python packages
RUN pip install --no-cache-dir flask requests

# Copy the webhook receiver script
COPY webhook_receiver.py /app/webhook_receiver.py

# Create logs directory
RUN mkdir -p /app/logs

# Expose the port
EXPOSE 5000

# Set environment variables with defaults
ENV WEBHOOK_SECRET=your-webhook-secret
ENV DISCORD_WEBHOOK_URL=""
ENV FLASK_ENV=production

# Run the webhook receiver
CMD ["python", "webhook_receiver.py"]
