dotnet publish
scp -r ./bin/Release/net8.0/publish/* pi@192.168.1.5:/home/pi/crossword/
ssh pi@192.168.1.5 "sudo systemctl restart crossword.service"
pause