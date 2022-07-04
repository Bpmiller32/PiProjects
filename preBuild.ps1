cd C:\Users\billy\Desktop\SweatBot\

dotnet build
dotnet publish -r linux-arm --no-self-contained /p:PublishSingleFile=true

# Debug
scp C:\Users\billy\Desktop\SweatBot\SweatBot\bin\Debug\net6.0\* pi@192.168.50.183:/home/pi/SweatBotDebug

# Publish
scp C:\Users\billy\Desktop\SweatBot\SweatBot\bin\Debug\net6.0\linux-arm\publish\appsettings.json pi@192.168.50.183:/home/pi/
scp C:\Users\billy\Desktop\SweatBot\SweatBot\bin\Debug\net6.0\linux-arm\publish\SweatBot pi@192.168.50.183:/home/pi/