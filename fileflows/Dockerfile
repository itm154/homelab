ARG BASE_TAG=latest

# Stage 1: Build the patcher tool using the .NET SDK
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS patcher-builder
WORKDIR /src
COPY patcher/ /src/patcher/
RUN dotnet publish /src/patcher/patcher.csproj -c Release -o /app/patcher

# Stage 2: Final patched image
FROM revenz/fileflows:${BASE_TAG}

# Copy the compiled patcher tool into the image temporarily
COPY --from=patcher-builder /app/patcher /app/patcher

# Run the patcher on target DLLs using the container's .NET runtime
RUN dotnet /app/patcher/patcher.dll /app/Server/FileFlows.Services.dll && \
    if [ -f /app/Node/FileFlows.Services.dll ]; then dotnet /app/patcher/patcher.dll /app/Node/FileFlows.Services.dll; fi && \
    if [ -f /app/Agent/FileFlows.Services.dll ]; then dotnet /app/patcher/patcher.dll /app/Agent/FileFlows.Services.dll; fi

# Clean up the patcher tool to keep the image clean
RUN rm -rf /app/patcher
