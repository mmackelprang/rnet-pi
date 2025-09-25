# Use official Node.js runtime as base image
FROM node:18-alpine

# Set working directory in container
WORKDIR /app

# Copy package files
COPY package*.json ./

# Install dependencies
RUN npm ci --only=production

# Copy application source
COPY src/ ./src/
COPY config.json ./
COPY sources.json ./
COPY zones.json ./

# Create non-root user for security
RUN addgroup -g 1001 -S nodejs
RUN adduser -S rnet -u 1001
RUN chown -R rnet:nodejs /app
USER rnet

# Expose port
EXPOSE 3000

# Health check
HEALTHCHECK --interval=30s --timeout=3s --start-period=5s --retries=3 \
  CMD node -e "require('http').get('http://localhost:3000', (res) => process.exit(res.statusCode === 200 ? 0 : 1))" || exit 1

# Start the application
CMD ["node", "src/app.js"]