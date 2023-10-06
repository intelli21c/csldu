# csldu
CSharp LuaDNS Updater

## example config file (csldu.json)
```
{
  "id": "jdoe@example.com",
  "token": "123456789abcdefg",
  "url": "example.com"
}
```
place this under ProgramData\CSLDU\ or /usr/local/etc/ or at least executable directory.

## example crontab entry
```
0 * * * * /home/jdoe/bin/csldu/csldu
```
this updates hourly.
