FROM node:20-alpine AS build

WORKDIR /app
ARG VITE_API_URL
ENV VITE_API_URL=$VITE_API_URL
COPY package.json package-lock.json ./
RUN npm ci

COPY . .
RUN npm run build

FROM nginx:alpine
ENV PORT=8080
COPY nginx.conf /etc/nginx/templates/default.conf.template
COPY --from=build /app/out /usr/share/nginx/html

EXPOSE 8080
CMD ["nginx", "-g", "daemon off;"]
