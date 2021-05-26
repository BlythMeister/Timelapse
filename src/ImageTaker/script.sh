#! /bin/bash

while true
do
  DATE=$(date +"%Y-%m-%d")
  TIME=$(date +"%y%m%d_%H%M")
  echo "Taking photo to file $DATE/$TIME"
  mkdir /home/chris/drive/photos/$DATE
  fswebcam --device /dev/video0 --resolution 1280x720 --no-banner --delay 2 --skip 40 --frames 5 /home/chris/drive/photos/$DATE/$TIME.jpg
  echo "Sleeping 1 minutes before next photo..."
  sleep 1m
done
