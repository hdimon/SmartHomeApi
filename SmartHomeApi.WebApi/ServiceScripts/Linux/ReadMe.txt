Install:
1. Put smarthomeapi.service into "/etc/systemd/system" directory.
2. Open terminal and execute "sudo systemctl daemon-reload".
3. Execute "sudo systemctl start smarthomeapi".
4. Execute "sudo systemctl enable smarthomeapi" if you want a service to be launched at system startup.

Uninstall:
1. Open terminal and execute "sudo systemctl stop smarthomeapi" to stop service.
2. Open another terminal and execute "sudo nautilus".
3. Delete smarthomeapi.service file from "/etc/systemd/system".
4. Execute "sudo systemctl daemon-reload".