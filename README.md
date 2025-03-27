# Crossword

To install as a serice copy crossword.serice to /etc/systemd/system/
$ sudo systemctl enable crossword.service
$ sudo systemctl start crossword.service

After new deployments:
$ sudo systemctl restart crossword.service