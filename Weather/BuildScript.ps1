dotnet build
dotnet publish -r linux-arm --no-self-contained /p:PublishSingleFile=true

# Deploy to Pi
scp ./SweatBot/bin/Debug/net7.0/linux-arm/publish/appsettings.json billy@10.254.254.7:/home/billy
scp ./SweatBot/bin/Debug/net7.0/linux-arm/publish/SweatBot billy@10.254.254.7:/home/billy