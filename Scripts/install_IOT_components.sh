#!/usr/bin/env bash

CONN_STR="$1"

echo 'Recieved connection string '$CONN_STR

sudo apt-get update

#Install docker
echo 'Downloading docker'
curl -fsSL get.docker.com -o get-docker.sh

echo 'Installing docker'
sudo sh get-docker.sh

#install python 2.7 pip
sudo apt-get install python-pip -y

#install IOT Edge runtime
sudo pip install -U azure-iot-edge-runtime-ctl

if [ ! -z $CONN_STR ]; then
	echo 'Attempting to setup device with connection string {'$CONN_STR'}'
	sudo iotedgectl setup --connection-string "$CONN_STR" --auto-cert-gen-force-no-passwords

	echo 'starting up device.'
	sudo iotedgectl start
else
	echo 'No connection string set, exiting.'
fi
