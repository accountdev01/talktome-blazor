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

ENTRYPOINT ["dotnet", "TalkToMe.dll"]