sc create SmartHomeApi binPath= %~dp0SmartHomeApi.WebApi.exe
sc failure SmartHomeApi actions= restart/60000/restart/60000/""/60000 reset= 86400
sc start SmartHomeApi
sc config SmartHomeApi start=auto

pause