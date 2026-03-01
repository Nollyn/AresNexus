FROM python:3.12-slim

# Task 5: Hardened Python container (Non-root)
RUN groupadd -r appuser && useradd -r -g appuser appuser
WORKDIR /app

ENV PYTHONDONTWRITEBYTECODE=1 \
    PYTHONUNBUFFERED=1

RUN pip install --no-cache-dir fastapi uvicorn requests

COPY apps/compliance-engine/main.py ./
RUN chown -R appuser:appuser /app

USER appuser
EXPOSE 8080

CMD ["uvicorn", "main:app", "--host", "0.0.0.0", "--port", "8080"]
