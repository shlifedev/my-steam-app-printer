#!/bin/sh  
docker build -t steam_appid_gen . 
docker run -it steam_appid_gen