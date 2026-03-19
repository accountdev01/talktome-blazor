# Stage 1: Build & Publish
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS publish
WORKDIR /src

# Copy เฉพาะไฟล์โปรเจกต์เพื่อทำ Restore (ช่วยให้ Build เร็วขึ้นเพราะใช้ Layer Cache)
COPY ["TalkToMe/TalkToMe.csproj", "TalkToMe/"]
RUN dotnet restore "TalkToMe/TalkToMe.csproj"

# Copy ไฟล์ที่เหลือทั้งหมด
COPY . .

# ย้ายไปที่โฟลเดอร์โปรเจกต์
WORKDIR "/src/TalkToMe"

RUN dotnet publish "TalkToMe.csproj" -c Release -o /app/publish /p:UseAppHost=false

# Stage 2: Final Runtime
FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS final
WORKDIR /app

# จัดการเรื่อง Permission สำหรับ Log Folder
USER root
# สร้างโฟลเดอร์ logs
RUN mkdir -p /app/logs && chown -R app:app /app/logs

USER app

# Copy ไฟล์ที่ publish มาจาก Stage แรก
COPY --from=publish /app/publish .

# --- Stage 1: Base (กำหนดภาพรวมที่ใช้ร่วมกัน) ---
FROM mcr.microsoft.com/dotnet/sdk:9.0-alpine AS build
WORKDIR /src

# --- Stage 2: Restore (Layer Caching) ---
COPY ["TalkToMe/TalkToMe.csproj", "TalkToMe/"]
COPY ["TalkToMe.Client/TalkToMe.Client.csproj", "TalkToMe.Client/"]

# ใช้ --mount=type=cache เพื่อเก็บ NuGet cache ไว้ในเครื่อง build
RUN --mount=type=cache,id=nuget,target=/root/.nuget/packages \
    dotnet restore "TalkToMe/TalkToMe.csproj"

# --- Stage 3: Publish ---
COPY . .
WORKDIR "/src/TalkToMe"

RUN dotnet publish "TalkToMe.csproj"
	-c Release \
    -o /app/publish \
    /p:UseAppHost=false \
    /p:StaticWebAssetsEnabled=true

# --- Stage 4: Final (Runtime) ---
FROM mcr.microsoft.com/dotnet/aspnet:9.0-alpine AS final
WORKDIR /app

# 1. Environment & Network
ENV ASPNETCORE_URLS=http://+:8080 \
    DOTNET_RUNNING_IN_CONTAINER=true \
    DOTNET_SYSTEM_GLOBALIZATION_INVARIANT=false
	
# 2. Dependencies (สำหรับ Alpine ต้องลง icu-libs เพื่อให้ภาษาไทย/Localization ทำงานได้)
RUN apk add --no-cache icu-libs
EXPOSE 8080

RUN apk add --no-cache curl

# 3. Security (Non-root)
RUN mkdir -p /app/logs && chown -R app:app /app/logs

# 4. Copy artifacts
COPY --from=build /app/publish .

# ใช้ User 'app' เสมอ (Security Best Practice)
USER app

# 5. Healthcheck
HEALTHCHECK --interval=30s --timeout=3s --retries=3 \
  CMD curl -f http://localhost:8080/health || exit 1

ENTRYPOINT ["dotnet", "TalkToMe.dll"]