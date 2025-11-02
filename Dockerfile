# Use the official .NET SDK image for building
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src

# Copy project file and restore dependencies
COPY ["EventEase.csproj", "./"]
RUN dotnet restore "EventEase.csproj"

# Copy source code and build the application
COPY . .
RUN dotnet build "EventEase.csproj" -c Release -o /app/build

# Publish the application
FROM build AS publish
RUN dotnet publish "EventEase.csproj" -c Release -o /app/publish

# Use nginx to serve the static files
FROM nginx:alpine AS final
WORKDIR /usr/share/nginx/html

# Copy published files
COPY --from=publish /app/publish/wwwroot .

# Copy custom nginx configuration
COPY nginx.conf /etc/nginx/nginx.conf

# Expose port 80
EXPOSE 80

# Start nginx
CMD ["nginx", "-g", "daemon off;"]