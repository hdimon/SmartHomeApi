sc stop SmartHomeApi
timeout /t 10 /nobreak > NUL
sc delete SmartHomeApi

pause